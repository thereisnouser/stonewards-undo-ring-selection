using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace StonewardsUndoRingSelection
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class UndoRingSelectionPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.thereisnouser.stonewards.undoringselection";
        public const string PluginName = "Undo Ring Selection";
        public const string PluginVersion = "0.1.0";

        private static UndoRingSelectionPlugin _plugin;
        private static ConfigEntry<bool> _enabled;
        private Harmony _harmony;
        private static MethodInfo _onUpgradeClicked;
        private static bool _replaying;
        private static PendingChoice _pending;

        private sealed class PendingChoice
        {
            internal RogueLikeUpgradeMenu Menu;
            internal object[] Args;
            internal VisualElement Panel;
            internal Label Description;
        }

        private void Awake()
        {
            _plugin = this;
            _enabled = Config.Bind("General", "Enabled", true,
                "Require confirmation before choosing a level-up ring. Cancel or choose another ring before confirming.");

            _onUpgradeClicked = AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnUpgradeClicked");
            if (_onUpgradeClicked == null)
            {
                Logger.LogError("RogueLikeUpgradeMenu.OnUpgradeClicked not found. Plugin disabled for this game version.");
                return;
            }

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(UndoRingSelectionPlugin).Assembly);
                var parameters = _onUpgradeClicked.GetParameters();
                var signatures = new List<string>();
                foreach (var parameter in parameters)
                    signatures.Add(parameter.ParameterType.Name + " " + parameter.Name);
                Logger.LogInfo("Undo Ring Selection 0.1.0 loaded. OnUpgradeClicked(" + string.Join(", ", signatures) + ")");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to install ring-choice patches: " + ex);
                _harmony?.UnpatchSelf();
            }
        }

        private void OnDestroy()
        {
            Cancel();
            DisposeFrameTextures();
            _harmony?.UnpatchSelf();
        }

        private static bool Intercept(RogueLikeUpgradeMenu menu, object[] args)
        {
            // The vanilla method runs only when the player has explicitly confirmed.
            if (_replaying || _enabled == null || !_enabled.Value || menu == null || menu.IsUpgradeSelected)
                return true;

            try
            {
                if (_pending != null && !ReferenceEquals(_pending.Menu, menu))
                    Cancel();

                if (_pending == null)
                    _pending = new PendingChoice { Menu = menu };

                _pending.Args = args != null ? (object[])args.Clone() : Array.Empty<object>();
                EnsurePanel(_pending);
                if (_pending.Description != null)
                    _pending.Description.text = "RING: " + DescribeSelection(_pending.Args);

                _plugin.Logger.LogDebug("Ring click held for confirmation; args=" + _pending.Args.Length);
                return false;
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError("Ring confirmation failed; allowing vanilla click: " + ex);
                Cancel();
                return true; // Fail open: never block ring selection if the mod UI is broken.
            }
        }

        // The same gold staircase-corner treatment used by our other Stonewards mods.
        // Built at runtime: no proprietary game images or fonts are redistributed.
        private const int FrameTextureSize = 32;
        private const int CornerSlice = 12;
        private static Texture2D _windowFrame;
        private static Texture2D _confirmFrame;
        private static Texture2D _confirmHoverFrame;
        private static Texture2D _cancelFrame;
        private static Texture2D _cancelHoverFrame;

        private static readonly Color FrameGold = new Color(0.72f, 0.64f, 0.48f, 1f);
        private static readonly Color FrameHoverGold = new Color(0.97f, 0.85f, 0.60f, 1f);
        private static readonly Color WindowBackground = new Color(19f / 255f, 23f / 255f, 32f / 255f, 0.98f);
        private static readonly Color HeaderBackground = new Color(76f / 255f, 46f / 255f, 48f / 255f, 1f);

        private static void EnsurePanel(PendingChoice choice)
        {
            if (choice.Panel != null && choice.Panel.parent != null)
                return;

            VisualElement root = choice.Menu.rogueLikeUpgradeScreen;
            if (root == null)
                throw new InvalidOperationException("rogueLikeUpgradeScreen is unavailable");

            EnsureFrameTextures();

            // This full-width anchor centers the popup while remaining transparent to
            // mouse input outside the actual window. Works at 1080p and 1440p.
            var anchor = new VisualElement
            {
                name = "stonewards-undo-ring-selection-anchor",
                pickingMode = PickingMode.Ignore
            };
            anchor.style.position = Position.Absolute;
            anchor.style.left = 0;
            anchor.style.right = 0;
            anchor.style.bottom = 18;
            anchor.style.alignItems = Align.Center;
            anchor.style.justifyContent = Justify.Center;

            var panel = new VisualElement { name = "stonewards-undo-ring-selection-confirmation" };
            panel.style.position = Position.Relative;
            panel.style.width = 680;
            panel.style.maxWidth = Length.Percent(95);
            panel.style.height = 230;
            panel.style.flexShrink = 1;
            panel.style.backgroundColor = Color.clear;
            ApplyPixelFrame(panel, _windowFrame);
            anchor.Add(panel);

            // Burgundy title strip and fine gold divider, matching the other HUD windows.
            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.style.position = Position.Absolute;
            header.style.left = 12;
            header.style.right = 12;
            header.style.top = 12;
            header.style.height = 53;
            header.style.backgroundColor = HeaderBackground;
            panel.Add(header);

            var divider = new VisualElement { pickingMode = PickingMode.Ignore };
            divider.style.position = Position.Absolute;
            divider.style.left = 11;
            divider.style.right = 11;
            divider.style.top = 65;
            divider.style.height = 2;
            divider.style.backgroundColor = FrameGold;
            panel.Add(divider);

            var title = new Label("CONFIRM RING SELECTION");
            title.name = "stonewards-ring-confirm-title";
            title.pickingMode = PickingMode.Ignore;
            title.style.position = Position.Absolute;
            title.style.left = 22;
            title.style.right = 22;
            title.style.top = 16;
            title.style.height = 44;
            title.style.fontSize = 25;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(1f, 0.88f, 0.66f, 1f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(title);

            var description = new Label("RING SELECTED");
            description.name = "stonewards-ring-confirm-selected";
            description.pickingMode = PickingMode.Ignore;
            description.style.position = Position.Absolute;
            description.style.left = 28;
            description.style.right = 28;
            description.style.top = 81;
            description.style.height = 37;
            description.style.fontSize = 23;
            description.style.unityFontStyleAndWeight = FontStyle.Bold;
            description.style.color = new Color(0.99f, 0.98f, 0.92f, 1f);
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(description);

            var hint = new Label("CHOOSE ANOTHER RING OR CANCEL THIS PICK");
            hint.pickingMode = PickingMode.Ignore;
            hint.style.position = Position.Absolute;
            hint.style.left = 25;
            hint.style.right = 25;
            hint.style.top = 124;
            hint.style.height = 25;
            hint.style.fontSize = 17;
            hint.style.unityFontStyleAndWeight = FontStyle.Bold;
            hint.style.color = new Color(0.76f, 0.77f, 0.78f, 1f);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(hint);

            var buttons = new VisualElement();
            buttons.style.position = Position.Absolute;
            buttons.style.left = 14;
            buttons.style.right = 14;
            buttons.style.bottom = 15;
            buttons.style.height = 53;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.alignItems = Align.Center;
            buttons.style.justifyContent = Justify.Center;
            panel.Add(buttons);

            var confirm = CreateGameButton("CONFIRM", () => Confirm(choice.Menu),
                _confirmFrame, _confirmHoverFrame);
            var cancel = CreateGameButton("CANCEL", Cancel,
                _cancelFrame, _cancelHoverFrame);
            buttons.Add(confirm);
            buttons.Add(cancel);

            root.Add(anchor);
            // Store the anchor so Cancel removes the entire panel and all controls.
            choice.Panel = anchor;
            choice.Description = description;
        }

        private static Button CreateGameButton(string text, Action onClick,
            Texture2D normal, Texture2D hover)
        {
            var button = new Button(onClick) { text = text };
            button.RemoveFromClassList("unity-button");
            button.style.width = 216;
            button.style.maxWidth = Length.Percent(46);
            button.style.height = 49;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.paddingLeft = 5;
            button.style.paddingRight = 5;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.flexShrink = 1;
            button.style.fontSize = 22;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.color = new Color(0.98f, 0.95f, 0.87f, 1f);
            ApplyPixelFrame(button, normal);
            button.RegisterCallback<PointerEnterEvent>(_ =>
            {
                button.style.backgroundImage = new StyleBackground(hover);
                button.style.color = new Color(1f, 0.98f, 0.85f, 1f);
            });
            button.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                button.style.backgroundImage = new StyleBackground(normal);
                button.style.color = new Color(0.98f, 0.95f, 0.87f, 1f);
            });
            return button;
        }

        private static void ApplyPixelFrame(VisualElement element, Texture2D frame)
        {
            // UI Toolkit border radius draws smooth corners, so we use point-filtered
            // 9-slice textures with the same stepped pixels as Session Visibility.
            element.style.backgroundColor = Color.clear;
            element.style.backgroundImage = new StyleBackground(frame);
            element.style.unitySliceLeft = CornerSlice;
            element.style.unitySliceRight = CornerSlice;
            element.style.unitySliceTop = CornerSlice;
            element.style.unitySliceBottom = CornerSlice;
            element.style.borderTopWidth = 0;
            element.style.borderBottomWidth = 0;
            element.style.borderLeftWidth = 0;
            element.style.borderRightWidth = 0;
            element.style.borderTopLeftRadius = 0;
            element.style.borderTopRightRadius = 0;
            element.style.borderBottomLeftRadius = 0;
            element.style.borderBottomRightRadius = 0;
        }

        private static void EnsureFrameTextures()
        {
            if (_windowFrame != null) return;

            _windowFrame = CreateFrameTexture(WindowBackground, FrameGold, "UndoRingSelection_Window");
            _confirmFrame = CreateFrameTexture(
                new Color(0.20f, 0.29f, 0.27f, 1f), FrameGold, "UndoRingSelection_Confirm");
            _confirmHoverFrame = CreateFrameTexture(
                new Color(0.28f, 0.38f, 0.32f, 1f), FrameHoverGold, "UndoRingSelection_ConfirmHover");
            _cancelFrame = CreateFrameTexture(
                new Color(0.28f, 0.23f, 0.27f, 1f), FrameGold, "UndoRingSelection_Cancel");
            _cancelHoverFrame = CreateFrameTexture(
                new Color(0.38f, 0.28f, 0.33f, 1f), FrameHoverGold, "UndoRingSelection_CancelHover");
        }

        private static int StairInset(int distanceFromEdge)
        {
            if (distanceFromEdge < 2) return 8;
            if (distanceFromEdge < 4) return 6;
            if (distanceFromEdge < 6) return 4;
            if (distanceFromEdge < 8) return 2;
            return 0;
        }

        private static bool InsideStaircase(int x, int y)
        {
            if (x < 0 || y < 0 || x >= FrameTextureSize || y >= FrameTextureSize)
                return false;
            int edgeX = Mathf.Min(x, FrameTextureSize - 1 - x);
            int edgeY = Mathf.Min(y, FrameTextureSize - 1 - y);
            return edgeX >= StairInset(edgeY) && edgeY >= StairInset(edgeX);
        }

        private static Texture2D CreateFrameTexture(Color fill, Color edge, string name)
        {
            var texture = new Texture2D(FrameTextureSize, FrameTextureSize, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            var pixels = new Color[FrameTextureSize * FrameTextureSize];
            for (int y = 0; y < FrameTextureSize; ++y)
            {
                for (int x = 0; x < FrameTextureSize; ++x)
                {
                    if (!InsideStaircase(x, y))
                    {
                        pixels[y * FrameTextureSize + x] = Color.clear;
                        continue;
                    }

                    // Two-pixel gold silhouette follows each step, not a smooth curve.
                    bool interior = InsideStaircase(x - 2, y) &&
                                    InsideStaircase(x + 2, y) &&
                                    InsideStaircase(x, y - 2) &&
                                    InsideStaircase(x, y + 2);
                    pixels[y * FrameTextureSize + x] = interior ? fill : edge;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void DisposeFrameTextures()
        {
            var frames = new[] {
                _windowFrame, _confirmFrame, _confirmHoverFrame,
                _cancelFrame, _cancelHoverFrame
            };
            foreach (var frame in frames)
                if (frame != null)
                    UnityEngine.Object.Destroy(frame);

            _windowFrame = null;
            _confirmFrame = null;
            _confirmHoverFrame = null;
            _cancelFrame = null;
            _cancelHoverFrame = null;
        }

        private static void Confirm(RogueLikeUpgradeMenu menu)
        {
            var choice = _pending;
            if (choice == null || !ReferenceEquals(choice.Menu, menu))
                return;

            // OnUpgradeClicked can be private. Replay the *same* event with its original arguments,
            // without manually touching upgrades, rerolls, stats, network commands, or save state.
            _replaying = true;
            try
            {
                _onUpgradeClicked.Invoke(menu, choice.Args);
                _plugin.Logger.LogInfo("Confirmed a ring through vanilla OnUpgradeClicked.");
                Cancel();
            }
            catch (TargetInvocationException ex)
            {
                _plugin.Logger.LogError("Vanilla ring selection threw: " + (ex.InnerException ?? ex));
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError("Could not confirm selected ring: " + ex);
            }
            finally
            {
                _replaying = false;
            }
        }

        private static void Cancel()
        {
            var choice = _pending;
            _pending = null;
            if (choice?.Panel != null)
                choice.Panel.RemoveFromHierarchy();
        }

        private static string DescribeSelection(object[] args)
        {
            if (args.Length == 0)
                return "CURRENT CHOICE";

            object value = args[0];
            if (value == null)
                return "CURRENT CHOICE";

            // Different game versions may pass a draft index, choice struct, or an upgrade object.
            object upgrade = ReadMember(value, "Upgrade") ?? ReadMember(value, "upgrade") ?? value;
            string name = ReadMember(upgrade, "DisplayName")?.ToString()
                ?? ReadMember(upgrade, "Name")?.ToString()
                ?? ReadMember(upgrade, "name")?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                if (value is int index)
                    return "OPTION " + (index + 1);
                return "CURRENT CHOICE";
            }
            return name.Length <= 65 ? name : name.Substring(0, 62) + "...";
        }

        private static object ReadMember(object instance, string name)
        {
            if (instance == null)
                return null;
            var type = instance.GetType();
            try
            {
                return AccessTools.Field(type, name)?.GetValue(instance)
                    ?? AccessTools.Property(type, name)?.GetValue(instance, null);
            }
            catch { return null; }
        }

        [HarmonyPatch(typeof(RogueLikeUpgradeMenu), "OnUpgradeClicked")]
        private static class RingClickedPatch
        {
            private static bool Prefix(RogueLikeUpgradeMenu __instance, object[] __args)
            {
                return Intercept(__instance, __args);
            }
        }

        [HarmonyPatch(typeof(RogueLikeUpgradeMenu), "OnUpgradeDraftGenerated")]
        private static class DraftGeneratedPatch
        {
            private static void Postfix(RogueLikeUpgradeMenu __instance)
            {
                if (_pending != null && ReferenceEquals(_pending.Menu, __instance))
                    Cancel();
            }
        }

        [HarmonyPatch(typeof(UpgradePopupController), "Setup",
            new[] { typeof(List<RogueLikeUpgradeManager.UpgradeDraftChoice>) })]
        private static class CardsSetupPatch
        {
            private static void Postfix()
            {
                // After opening the screen OR rerolling, an old unconfirmed choice must not survive.
                Cancel();
            }
        }

        [HarmonyPatch(typeof(BaseMenu), "Close")]
        private static class MenuClosedPatch
        {
            private static void Postfix(BaseMenu __instance)
            {
                if (__instance is RogueLikeUpgradeMenu && _pending != null &&
                    ReferenceEquals(__instance, _pending.Menu))
                    Cancel();
            }
        }
    }
}
