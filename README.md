# Stonewards Undo Ring Selection

**Prevent accidental ring picks and skips in Stonewards by confirming your actions before they are applied.** Cancel an unconfirmed ring choice or skip without using a reroll or a skip.

## ✨ Features

- **Confirm before choosing** — clicking a ring opens a confirmation window instead of immediately locking in the upgrade.
- **Cancel and choose again** — dismiss an accidental choice and pick a different ring with a single click.
- **Skip confirmation** — manually pressing **SKIP** also asks for confirmation before using a skip.
- **Centered game-style interface** — a large confirmation window centered on the screen, with pixel-stepped corners, a gold frame, and readable ring names.
- **Original upgrade handling** — confirmation uses Stonewards' existing selection or skip logic.

## 🎮 How to use

When the **Upgrade Offers** screen appears:

1. Click a ring to open the centered confirmation window.
2. Click **CONFIRM** to select that ring, or **CANCEL** to keep the current offers.
3. You can also click another ring before confirming to change the pending choice.
4. Click the game's **SKIP** button to open a separate skip confirmation. **CONFIRM** uses the skip; **CANCEL** returns to the current ring offers without using it.

There is no additional hotkey. Cancelling does not spend a reroll or a skip.

## ⚙️ Configuration

BepInEx creates `BepInEx/config/com.thereisnouser.stonewards.undoringselection.cfg` with this option:

```ini
[General]
Enabled = true
```

Set `Enabled = false` to disable confirmation for both ring clicks and **SKIP**.

## 📦 Installation

Install **UndoRingSelection** using **r2modman** or another compatible Thunderstore mod manager, then launch **Stonewards** through the manager. The Thunderstore package declares **BepInExPack** as a dependency.

## 🔧 Compatibility

- **Game:** Stonewards **0.2.3** (tested locally).
- **Mod loader:** BepInEx 5.
- **Multiplayer:** modifies the local player's selection UI; multiplayer behavior has not been separately verified.
- **Release 0.1.2:** the centered popup and skip-confirmation changes still need in-game verification.

## ⚠️ Known limitations

- **Already confirmed upgrades cannot be undone.** The mod prevents accidental confirmation; it does not remove applied rings.
- Only manual ring selection is intercepted. The game's automatic selection timer is unchanged.
- Compatibility with other mods that alter the upgrade-selection screen has not been separately verified.

## 🛠️ Development

### Building

You need a .NET SDK, a local Stonewards installation, and BepInEx 5 in a mod-manager profile. Close the game before installing the rebuilt DLL.

```powershell
$gameDir = 'C:\Games\Stonewards' # Replace with your Stonewards installation folder.
$profileDir = Join-Path $env:APPDATA 'r2modmanPlus-local\Stonewards\profiles\stonewards-undo-ring-selection'

.\build-and-install.ps1 -GameDir $gameDir -ProfileDir $profileDir
```

The script compiles `StonewardsUndoRingSelection.dll` against your installed game's assemblies and places it in the selected profile's `BepInEx/plugins/StonewardsUndoRingSelection/` folder. The development profile name matches the GitHub repository name.

### Implementation

The mod intercepts the ring-card `OnClicked` and `OnSubmitted` handlers before the game's `PerformAction` animation runs. A Harmony prefix also intercepts `RogueLikeUpgradeMenu.OnSkipClicked` before its network command. **CONFIRM** invokes the appropriate native handler; **CANCEL** discards the pending action without generating new ring offers.

## 🔗 Links

[GitHub repository](https://github.com/thereisnouser/stonewards-undo-ring-selection) · [Report an issue](https://github.com/thereisnouser/stonewards-undo-ring-selection/issues) · [Changelog](CHANGELOG.md)

---

*Made by thereisnouser*
