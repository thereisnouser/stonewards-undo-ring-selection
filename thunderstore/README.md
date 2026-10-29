# Stonewards Undo Ring Selection

**Prevent accidental ring picks and skips by confirming your actions before they are applied.** Cancel an unconfirmed selection without spending a reroll or a skip.

## ✨ Features

- **Selection confirmation** — clicking a ring opens a confirmation window before the upgrade is applied.
- **Cancel and retry** — return to the same ring offers and choose another ring with a single click.
- **Skip confirmation** — confirm a manual **SKIP** before it consumes a skip.
- **Centered game-style UI** — a compact dialog centered within **Upgrade Offers**, with a strongly dimmed backdrop, bold labels, readable ring names, and a gold pixel-art frame.

## 🎮 How to use

On the **Upgrade Offers** screen, click a ring to open the centered confirmation window. Press **CONFIRM** to select it or **CANCEL** to keep the same offers. You can also click another ring before confirming.

Press the game's **SKIP** button to open a skip confirmation. Press **CONFIRM** to skip or **CANCEL** to return to the offers. There is **no extra hotkey**. Cancelling spends neither a reroll nor a skip.

## ⚙️ Configuration

Enabled by default. Set `Enabled = false` in `BepInEx/config/com.thereisnouser.stonewards.undoringselection.cfg` to disable confirmation for ring choices and **SKIP**.

## 📦 Installation

Install **UndoRingSelection** using **r2modman** or another compatible Thunderstore mod manager. Launch **Stonewards** through the mod manager. **BepInExPack** is declared as a dependency.

## 🔧 Compatibility

- **Game:** Stonewards **0.2.3** (tested locally).
- **Mod loader:** BepInEx 5.
- **Multiplayer:** modifies the local player's selection UI; multiplayer behavior has not been separately verified.
- **Release 0.2.0:** the updated confirmation layout has been visually checked in-game; multiplayer compatibility has not been separately verified.

## ⚠️ Known limitations

- Confirmed upgrades cannot be undone; only pending manual selections can be cancelled.
- The game's automatic ring-selection timer is unchanged.
- Other mods that alter the upgrade-selection screen have not been separately compatibility-tested.

## 🔗 Links

[GitHub](https://github.com/thereisnouser/stonewards-undo-ring-selection) · [Report an issue](https://github.com/thereisnouser/stonewards-undo-ring-selection/issues) · [Changelog](https://github.com/thereisnouser/stonewards-undo-ring-selection/blob/main/CHANGELOG.md)

---

*Made by thereisnouser*
