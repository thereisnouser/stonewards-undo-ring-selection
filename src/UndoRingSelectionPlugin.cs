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

        private static void EnsurePanel(PendingChoice choice)
        {
            if (choice.Panel != null && choice.Panel.parent != null)
                return;

            VisualElement root = choice.Menu.rogueLikeUpgradeScreen;
            if (root == null)
                throw new InvalidOperationException("rogueLikeUpgradeScreen is unavailable");

            var panel = new VisualElement { name = "stonewards-undo-ring-selection-confirmation" };
            panel.style.position = Position.Absolute;
            panel.style.left = Length.Percent(24);
            panel.style.right = Length.Percent(24);
            panel.style.bottom = 12;
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.alignItems = Align.Center;
            panel.style.paddingTop = 7;
            panel.style.paddingBottom = 7;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 0.96f);
            panel.style.borderTopWidth = 2;
            panel.style.borderBottomWidth = 2;
            panel.style.borderLeftWidth = 2;
            panel.style.borderRightWidth = 2;
            panel.style.borderTopColor = new Color(0.72f, 0.63f, 0.40f);
            panel.style.borderBottomColor = new Color(0.72f, 0.63f, 0.40f);
            panel.style.borderLeftColor = new Color(0.72f, 0.63f, 0.40f);
            panel.style.borderRightColor = new Color(0.72f, 0.63f, 0.40f);

            var title = new Label("CHOOSE ANOTHER RING OR CONFIRM");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 15;
            title.style.color = new Color(0.98f, 0.90f, 0.69f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 4;
            panel.Add(title);

            var description = new Label("RING SELECTED");
            description.style.color = Color.white;
            description.style.fontSize = 13;
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            description.style.marginBottom = 6;
            panel.Add(description);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            var confirm = new Button(() => Confirm(choice.Menu)) { text = "CONFIRM" };
            StyleButton(confirm, new Color(0.20f, 0.35f, 0.23f));
            var cancel = new Button(Cancel) { text = "CANCEL" };
            StyleButton(cancel, new Color(0.35f, 0.20f, 0.20f));
            buttonRow.Add(confirm);
            buttonRow.Add(cancel);
            panel.Add(buttonRow);

            root.Add(panel);
            choice.Panel = panel;
            choice.Description = description;
        }

        private static void StyleButton(Button button, Color background)
        {
            button.style.width = 116;
            button.style.height = 31;
            button.style.marginLeft = 5;
            button.style.marginRight = 5;
            button.style.fontSize = 15;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = Color.white;
            button.style.backgroundColor = background;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopColor = new Color(0.78f, 0.70f, 0.48f);
            button.style.borderBottomColor = new Color(0.78f, 0.70f, 0.48f);
            button.style.borderLeftColor = new Color(0.78f, 0.70f, 0.48f);
            button.style.borderRightColor = new Color(0.78f, 0.70f, 0.48f);
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
