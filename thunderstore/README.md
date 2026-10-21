# Stonewards Undo Ring Selection

**Prevent accidental ring picks by confirming your choice before it is applied.** Cancel a pending choice and pick another ring without spending a reroll.

## ✨ Features

- **Selection confirmation** — clicking a ring opens a confirmation window before the upgrade is applied.
- **Cancel and retry** — return to the same ring offers and choose another ring with a single click.
- **Matching game UI** — bold labels, readable ring names, and a gold pixel-art frame.

## 🎮 How to use

On the **Upgrade Offers** screen, click a ring to open the confirmation window. Press **CONFIRM** to select it or **CANCEL** to return to the three offers. You can also click another ring before confirming to change your choice.

There is **no extra hotkey**. Cancelling does not use a reroll.

## ⚙️ Configuration

Enabled by default. Set `Enabled = false` in `BepInEx/config/com.thereisnouser.stonewards.undoringselection.cfg` to disable the confirmation step.

## 📦 Installation

Install **UndoRingSelection** using **r2modman** or another compatible Thunderstore mod manager. Launch **Stonewards** through the mod manager. **BepInExPack** is declared as a dependency.

## 🔧 Compatibility

- **Game:** Stonewards **0.2.3** (tested locally).
- **Mod loader:** BepInEx 5.
- **Multiplayer:** modifies the local player's selection UI; multiplayer behavior has not been separately verified.

## ⚠️ Known limitations

- Confirmed upgrades cannot be undone; only pending manual selections can be cancelled.
- The game's automatic ring-selection timer is unchanged.
- Other mods that alter the upgrade-selection screen have not been separately compatibility-tested.

## 🔗 Links

[GitHub](https://github.com/thereisnouser/stonewards-undo-ring-selection) · [Report an issue](https://github.com/thereisnouser/stonewards-undo-ring-selection/issues) · [Changelog](https://github.com/thereisnouser/stonewards-undo-ring-selection/blob/main/CHANGELOG.md)

---

*Made by thereisnouser*
