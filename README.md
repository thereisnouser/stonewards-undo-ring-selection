# Stonewards Undo Ring Selection

**Prevent accidental ring picks in Stonewards by confirming your choice before it is applied.** Cancel a pending choice and select another ring without spending a reroll.

## ✨ Features

- **Confirm before choosing** — clicking a ring opens a confirmation window instead of immediately locking in the upgrade.
- **Cancel and choose again** — dismiss an accidental choice and pick a different ring with a single click.
- **Game-matched interface** — a larger confirmation window with pixel-stepped corners, a gold frame, and readable ring names.
- **Original upgrade handling** — confirmation passes the selected upgrade to Stonewards' normal selection logic.

## 🎮 How to use

When the **Upgrade Offers** screen appears:

1. Click the ring you want.
2. Review the ring name in the confirmation window.
3. Click **CONFIRM** to apply the selection, or **CANCEL** to return to the same three offers.
4. You can also click another ring while the confirmation window is open to change the pending selection.

There is no additional hotkey. **Cancel** does not cost a reroll.

## ⚙️ Configuration

BepInEx creates `BepInEx/config/com.thereisnouser.stonewards.undoringselection.cfg` with this option:

```ini
[General]
Enabled = true
```

Set `Enabled = false` to restore normal ring-click behavior.

## 📦 Installation

Install **UndoRingSelection** using **r2modman** or another compatible Thunderstore mod manager, then launch **Stonewards** through the manager. The Thunderstore package declares **BepInExPack** as a dependency.

## 🔧 Compatibility

- **Game:** Stonewards **0.2.3** (tested locally).
- **Mod loader:** BepInEx 5.
- **Multiplayer:** modifies the local player's selection UI; multiplayer behavior has not been separately verified.

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

The mod intercepts the ring-card `OnClicked` and `OnSubmitted` handlers before the game's `PerformAction` animation runs. **CONFIRM** calls `RogueLikeUpgradeMenu.OnUpgradeClicked` with the pending draft; **CANCEL** simply discards it. No upgrade rolls are regenerated when cancelling.

## 🔗 Links

[GitHub repository](https://github.com/thereisnouser/stonewards-undo-ring-selection) · [Report an issue](https://github.com/thereisnouser/stonewards-undo-ring-selection/issues) · [Changelog](CHANGELOG.md)

---

*Made by thereisnouser*
