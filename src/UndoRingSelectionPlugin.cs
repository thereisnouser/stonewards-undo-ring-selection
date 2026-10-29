using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
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
        public const string PluginVersion = "0.1.2";

        private static UndoRingSelectionPlugin _plugin;
        private static ConfigEntry<bool> _enabled;
        private Harmony _harmony;
        private static MethodInfo _onUpgradeClicked;
        private static MethodInfo _onSkipClicked;
        private static bool _confirmingSkip;
        private static PendingChoice _pending;

        private sealed class PendingChoice
        {
            internal RogueLikeUpgradeMenu Menu;
            internal object[] Args;
            internal bool IsSkip;
            internal VisualElement Panel;
            internal Label Title;
            internal Label Description;
            internal Label Hint;
        }

        private void Awake()
        {
            _plugin = this;
            _enabled = Config.Bind("General", "Enabled", true,
                "Require confirmation before manually selecting a ring or skipping an upgrade choice.");

            _onUpgradeClicked = AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnUpgradeClicked");
            _onSkipClicked = AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnSkipClicked", Type.EmptyTypes);
            if (_onUpgradeClicked == null || _onSkipClicked == null)
            {
                Logger.LogError("One of Stonewards' upgrade/skip callbacks was not found. Plugin disabled for this game version.");
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
                Logger.LogInfo("Undo Ring Selection " + PluginVersion + " loaded. Native callback (" +
                    string.Join(", ", signatures) + "); card clicks intercepted before PerformAction.");
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
            // Only manual card click/submit events are intercepted now.
            // No native card action/tween has been started at this point.
            if (_enabled == null || !_enabled.Value || menu == null || menu.IsUpgradeSelected)
                return true;

            try
            {
                if (_pending != null && !ReferenceEquals(_pending.Menu, menu))
                    Cancel();

                if (_pending == null)
                    _pending = new PendingChoice { Menu = menu };

                _pending.IsSkip = false;
                _pending.Args = args != null ? (object[])args.Clone() : Array.Empty<object>();
                EnsurePanel(_pending);
                _pending.Title.text = "CONFIRM RING SELECTION";
                _pending.Description.text = "RING: " + DescribeSelection(menu, _pending.Args);
                _pending.Hint.text = "CHOOSE ANOTHER RING OR CANCEL THIS PICK";

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

        private static bool InterceptSkip(RogueLikeUpgradeMenu menu)
        {
            // OnSkipClicked sends CmdSkipUsed immediately. Do not allow it through
            // until the player explicitly confirms.
            if (_confirmingSkip || _enabled == null || !_enabled.Value ||
                menu == null || !menu.IsOpen || menu.IsUpgradeSelected)
                return true;

            try
            {
                if (_pending != null && !ReferenceEquals(_pending.Menu, menu))
                    Cancel();

                if (_pending == null)
                    _pending = new PendingChoice { Menu = menu };

                _pending.IsSkip = true;
                _pending.Args = null;
                EnsurePanel(_pending);
                _pending.Title.text = "CONFIRM SKIPPING UPGRADE";
                _pending.Description.text = "SKIP THIS RING SELECTION?";
                _pending.Hint.text = "CANCEL TO KEEP YOUR CURRENT UPGRADE OFFERS";

                _plugin.Logger.LogDebug("Manual skip held for confirmation.");
                return false;
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError("Skip confirmation failed; allowing vanilla skip: " + ex);
                Cancel();
                return true; // Fail open if the game UI changed.
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

        private static readonly Color FrameGold = new Color(0.86f, 0.73f, 0.45f, 1f);
        private static readonly Color FrameHoverGold = new Color(1f, 0.88f, 0.58f, 1f);
        private static readonly Color WindowBackground = new Color(12f / 255f, 18f / 255f, 28f / 255f, 0.995f);
        private static readonly Color HeaderBackground = new Color(92f / 255f, 46f / 255f, 50f / 255f, 1f);

        private static void EnsurePanel(PendingChoice choice)
        {
            if (choice.Panel != null && choice.Panel.parent != null)
                return;

            VisualElement root = choice.Menu.rogueLikeUpgradeScreen;
            if (root == null)
                throw new InvalidOperationException("rogueLikeUpgradeScreen is unavailable");

            // This is the actual Stonewards UXML element used by
            // UpgradePopupController (verified against the game assembly).
            // Anchor to it so the confirmation follows the upgrade offers panel
            // across resolutions and does not dim Inventory or Stats.
            VisualElement offers = root.Q<VisualElement>("RogueLikeUpgradePopup");
            if (offers == null)
                throw new InvalidOperationException("RogueLikeUpgradePopup was not found in the upgrade UI");

            EnsureFrameTextures();

            // The confirmation fills the UPGRADE OFFERS container, not the
            // screen. Ignore pointer hits outside the panel itself; the dialog
            // itself is positioned from the exact 50%/50% center below.
            var anchor = new VisualElement
            {
                name = "stonewards-undo-ring-selection-anchor",
                pickingMode = PickingMode.Ignore
            };
            anchor.style.position = Position.Absolute;
            anchor.style.left = 0;
            anchor.style.right = 0;
            anchor.style.top = 0;
            anchor.style.bottom = 0;
            // Position the popup explicitly; anchor flex layout is not used
            // to determine the modal's center.

            // Strong modal backdrop: suppress visual competition from cards
            // and buttons, while preserving the underlying click handlers.
            var backdrop = new VisualElement
            {
                name = "stonewards-undo-ring-selection-backdrop",
                pickingMode = PickingMode.Ignore
            };
            backdrop.style.position = Position.Absolute;
            backdrop.style.left = 0;
            backdrop.style.right = 0;
            backdrop.style.top = 0;
            backdrop.style.bottom = 0;
            backdrop.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            anchor.Add(backdrop);

            var panel = new VisualElement { name = "stonewards-undo-ring-selection-confirmation" };
            panel.style.position = Position.Absolute;
            // UI Toolkit translates percentages relative to the panel itself,
            // keeping the exact center of the offers window at any resolution.
            panel.style.left = Length.Percent(50);
            panel.style.top = Length.Percent(50);
            panel.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            panel.style.width = 620;
            panel.style.maxWidth = Length.Percent(95);
            panel.style.height = 270;
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
            header.style.height = 57;
            header.style.backgroundColor = HeaderBackground;
            panel.Add(header);

            var divider = new VisualElement { pickingMode = PickingMode.Ignore };
            divider.style.position = Position.Absolute;
            divider.style.left = 11;
            divider.style.right = 11;
            divider.style.top = 69;
            divider.style.height = 3;
            divider.style.backgroundColor = FrameGold;
            panel.Add(divider);

            var title = new Label("CONFIRM RING SELECTION");
            title.name = "stonewards-ring-confirm-title";
            title.pickingMode = PickingMode.Ignore;
            title.style.position = Position.Absolute;
            title.style.left = 22;
            title.style.right = 22;
            title.style.top = 17;
            title.style.height = 49;
            title.style.fontSize = 27;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(1f, 0.90f, 0.67f, 1f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(title);

            var description = new Label("RING SELECTED");
            description.name = "stonewards-ring-confirm-selected";
            description.pickingMode = PickingMode.Ignore;
            description.style.position = Position.Absolute;
            description.style.left = 28;
            description.style.right = 28;
            description.style.top = 90;
            description.style.height = 40;
            description.style.fontSize = 24;
            description.style.unityFontStyleAndWeight = FontStyle.Bold;
            description.style.color = new Color(1f, 1f, 0.95f, 1f);
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(description);

            var hint = new Label("CHOOSE ANOTHER RING OR CANCEL THIS PICK");
            hint.pickingMode = PickingMode.Ignore;
            hint.style.position = Position.Absolute;
            hint.style.left = 25;
            hint.style.right = 25;
            hint.style.top = 137;
            hint.style.height = 29;
            hint.style.fontSize = 18;
            hint.style.unityFontStyleAndWeight = FontStyle.Bold;
            hint.style.color = new Color(0.82f, 0.82f, 0.80f, 1f);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(hint);

            var buttons = new VisualElement();
            buttons.style.position = Position.Absolute;
            buttons.style.left = 14;
            buttons.style.right = 14;
            buttons.style.bottom = 17;
            buttons.style.height = 61;
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

            offers.Add(anchor);
            // Store the anchor so Cancel removes the entire panel and all controls.
            choice.Panel = anchor;
            choice.Title = title;
            choice.Description = description;
            choice.Hint = hint;
        }

        private static Button CreateGameButton(string text, Action onClick,
            Texture2D normal, Texture2D hover)
        {
            var button = new Button(onClick) { text = text };
            button.RemoveFromClassList("unity-button");
            button.style.width = 232;
            button.style.maxWidth = Length.Percent(46);
            button.style.height = 54;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.paddingLeft = 5;
            button.style.paddingRight = 5;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.flexShrink = 1;
            button.style.fontSize = 23;
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
                new Color(0.22f, 0.38f, 0.30f, 1f), FrameGold, "UndoRingSelection_Confirm");
            _confirmHoverFrame = CreateFrameTexture(
                new Color(0.30f, 0.48f, 0.38f, 1f), FrameHoverGold, "UndoRingSelection_ConfirmHover");
            _cancelFrame = CreateFrameTexture(
                new Color(0.36f, 0.25f, 0.30f, 1f), FrameGold, "UndoRingSelection_Cancel");
            _cancelHoverFrame = CreateFrameTexture(
                new Color(0.46f, 0.31f, 0.37f, 1f), FrameHoverGold, "UndoRingSelection_CancelHover");
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

            try
            {
                if (choice.IsSkip)
                {
                    // The patch on OnSkipClicked must allow only this confirmed call.
                    // The original method then handles skip usage, network commands,
                    // and menu closing exactly as in unmodded Stonewards.
                    _confirmingSkip = true;
                    _onSkipClicked.Invoke(menu, null);
                    _plugin.Logger.LogInfo("Confirmed skip through native OnSkipClicked.");
                }
                else
                {
                    // Ring selection also commits only after confirmation.
                    _onUpgradeClicked.Invoke(menu, choice.Args);
                    _plugin.Logger.LogInfo("Confirmed ring through native OnUpgradeClicked.");
                }
                Cancel();
            }
            catch (TargetInvocationException ex)
            {
                _plugin.Logger.LogError("Vanilla upgrade/skip callback threw: " + (ex.InnerException ?? ex));
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError("Could not confirm upgrade/skip choice: " + ex);
            }
            finally
            {
                _confirmingSkip = false;
            }
        }

        private static void Cancel()
        {
            var choice = _pending;
            _pending = null;
            // No need to undo the game's card state: OnClicked was intercepted
            // BEFORE PerformAction, so clickTween/ShowOutline never ran.
            if (choice?.Panel != null)
                choice.Panel.RemoveFromHierarchy();
        }

        private static bool InterceptCard(object card)
        {
            if (_enabled == null || !_enabled.Value || card == null)
                return true;

            try
            {
                var menu = ReadMember(card, "_upgradeMenu") as RogueLikeUpgradeMenu;
                if (menu == null || !menu.IsOpen || menu.IsUpgradeSelected)
                    return true;

                // Honor the native guards for a draft that is still animating,
                // so clicking during a reroll cannot confirm a hidden card.
                if (ReadMember(card, "isShowing") is bool showing && showing)
                    return true;
                if (ReadMember(card, "isHiding") is bool hiding && hiding)
                    return true;

                var boxedChoice = ReadMember(card, "DraftChoice");
                if (!(boxedChoice is RogueLikeUpgradeManager.UpgradeDraftChoice draft) || !draft.IsValid)
                    return true;

                // ClickEvent itself proves that the player clicked this card.
                // Unlike PerformAction, do not gate on its delayed isHovered flag.
                return Intercept(menu, new object[] { draft });
            }
            catch (Exception ex)
            {
                _plugin?.Logger.LogError("Could not intercept native card click: " + ex);
                return true; // Fail open: preserve the vanilla selection path.
            }
        }

        private static string DescribeSelection(RogueLikeUpgradeMenu menu, object[] args)
        {
            if (args == null || args.Length == 0 || args[0] == null)
                return "CURRENT CHOICE";

            var value = args[0];
            string upgradeId = GetUpgradeId(value);
            int chosenIndex = value is int ? (int)value : -1;
            var popup = AccessTools.Field(typeof(RogueLikeUpgradeMenu),
                "upgradePopupController")?.GetValue(menu) as UpgradePopupController;

            // Prefer what the game's own ring card currently displays.
            // The raw upgrade Name/ID is an identifier such as CRIT_DAMAGE, not a title.
            if (popup?.UpgradeButtons != null)
            {
                int index = 0;
                foreach (var button in popup.UpgradeButtons)
                {
                    if (button == null)
                    {
                        index++;
                        continue;
                    }

                    var displayed = button.DraftChoice;
                    bool matches = chosenIndex >= 0 ? index == chosenIndex :
                        !string.IsNullOrEmpty(upgradeId) &&
                        displayed.Upgrade != null &&
                        string.Equals(displayed.Upgrade.ID, upgradeId, StringComparison.Ordinal);

                    if (matches)
                    {
                        string title = FindNativeCardTitle(button);
                        if (!string.IsNullOrWhiteSpace(title))
                            return TruncateTitle(title);
                    }
                    index++;
                }
            }

            // Localization-aware properties/methods vary between game builds.
            object upgrade = ReadMember(value, "Upgrade") ?? ReadMember(value, "upgrade") ?? value;
            foreach (var member in new[] { "LocalizedName", "localizedName", "DisplayName" })
            {
                string name = ReadMember(upgrade, member)?.ToString();
                if (LooksLikeTitle(name))
                    return TruncateTitle(name);
            }
            foreach (var methodName in new[] { "GetLocalizedName", "GetLocalizedTitle" })
            {
                try
                {
                    var method = AccessTools.Method(upgrade.GetType(), methodName, Type.EmptyTypes);
                    string name = method?.Invoke(upgrade, null)?.ToString();
                    if (LooksLikeTitle(name))
                        return TruncateTitle(name);
                }
                catch { /* Keep a human-readable fallback if localization API changed. */ }
            }

            // Last resort: display a readable title, never an uppercase raw ID.
            string raw = upgradeId ?? value.ToString();
            raw = raw.Replace('_', ' ').Replace('-', ' ').Trim();
            return TruncateTitle(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw.ToLowerInvariant()));
        }

        private static string GetUpgradeId(object value)
        {
            if (value == null)
                return null;
            var upgrade = ReadMember(value, "Upgrade") ?? ReadMember(value, "upgrade") ?? value;
            return ReadMember(upgrade, "ID")?.ToString();
        }

        private static string FindNativeCardTitle(object card)
        {
            if (card == null)
                return null;

            // In Stonewards the title is a Label on UpgradePopupController.UpgradeButton.
            // Use reflection because its private field names may change between versions.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = card.GetType().GetFields(flags);
            foreach (var field in fields)
            {
                if (!typeof(Label).IsAssignableFrom(field.FieldType))
                    continue;
                if (field.Name.IndexOf("name", StringComparison.OrdinalIgnoreCase) < 0 &&
                    field.Name.IndexOf("title", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string title = CleanGameText((field.GetValue(card) as Label)?.text);
                if (LooksLikeTitle(title))
                    return title;
            }

            // Fall back to the visible card labels, excluding the rarity/NEW/
            // description labels so the title matches what the player sees.
            foreach (var field in fields)
            {
                if (!typeof(VisualElement).IsAssignableFrom(field.FieldType))
                    continue;
                var element = field.GetValue(card) as VisualElement;
                string title = FindTitleInVisualTree(element, 0);
                if (LooksLikeTitle(title))
                    return title;
            }
            return null;
        }

        private static string FindTitleInVisualTree(VisualElement element, int depth)
        {
            if (element == null || depth > 12)
                return null;
            if (element is Label label)
            {
                string value = CleanGameText(label.text);
                if (LooksLikeTitle(value))
                    return value;
            }
            for (int i = 0; i < element.childCount; i++)
            {
                string value = FindTitleInVisualTree(element[i], depth + 1);
                if (LooksLikeTitle(value))
                    return value;
            }
            return null;
        }

        private static string CleanGameText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return Regex.Replace(value, "<[^>]+>", "").Trim();
        }

        private static bool LooksLikeTitle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            value = CleanGameText(value);
            if (string.IsNullOrWhiteSpace(value) || value.Length > 65 ||
                value == "NEW" || value == "Common" || value == "Uncommon" ||
                value == "Rare" || value == "Epic" || value == "Legendary" ||
                value.Contains(":") || value.Contains(">") || value.Contains("%"))
                return false;
            // Internal identifiers look like CRIT_DAMAGE, whereas real names
            // are normally title-cased and contain spaces.
            if (value.Contains("_") || value == value.ToUpperInvariant())
                return false;
            return true;
        }

        private static string TruncateTitle(string title)
        {
            return title.Length <= 65 ? title : title.Substring(0, 62) + "...";
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

        // Patch the actual UI event handler, not the menu's final choice callback.
        // PerformAction starts clickTween -> ShowOutline(true) before it invokes the
        // menu callback, which is why cancelling used to leave a stuck outline.
        [HarmonyPatch]
        private static class CardClickPatch
        {
            private static MethodBase TargetMethod()
            {
                var type = AccessTools.Inner(typeof(UpgradePopupController), "UpgradeButton");
                return AccessTools.Method(type, "OnClicked", new[] { typeof(ClickEvent) });
            }

            private static bool Prefix(object __instance)
            {
                return InterceptCard(__instance);
            }
        }

        [HarmonyPatch]
        private static class CardSubmitPatch
        {
            private static MethodBase TargetMethod()
            {
                var type = AccessTools.Inner(typeof(UpgradePopupController), "UpgradeButton");
                return AccessTools.Method(type, "OnSubmitted", new[] { typeof(NavigationSubmitEvent) });
            }

            private static bool Prefix(object __instance, NavigationSubmitEvent evt)
            {
                if (InterceptCard(__instance))
                    return true;
                evt?.StopPropagation(); // Matches the native OnSubmitted behavior.
                return false;
            }
        }

        [HarmonyPatch(typeof(RogueLikeUpgradeMenu), "OnSkipClicked")]
        private static class SkipClickedPatch
        {
            private static bool Prefix(RogueLikeUpgradeMenu __instance)
            {
                return InterceptSkip(__instance);
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
                // Opening/rerolling a draft invalidates our pending manual click.
                Cancel();
            }
        }

        [HarmonyPatch(typeof(BaseMenu), "Close")]
        private static class MenuClosedPatch
        {
            private static void Postfix(BaseMenu __instance)
            {
                if (!(__instance is RogueLikeUpgradeMenu))
                    return;
                if (_pending != null && ReferenceEquals(__instance, _pending.Menu))
                    Cancel();
            }
        }
    }
}
