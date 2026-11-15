# Stonewards Undo Ring Selection

**Confirm ring selections and skips before they are applied in Stonewards.** Cancel an accidental action and keep the same offers without spending a reroll or a skip.

## ✨ Features

- **Ring confirmation** — clicking a ring opens a confirmation dialog before the upgrade is applied.
- **Skip confirmation** — pressing the game's **SKIP** button requires confirmation before the skip is used.
- **Cancel and choose again** — return to the same ring offers without spending a reroll or a skip.
- **Background input lock** — the 75%-darkened backdrop blocks ring cards and **SKIP** while confirmation is open, including additional keyboard or controller submissions.
- **Game-style interface** — a centered dialog with readable ring names, a gold pixel frame, and distinct **CONFIRM** / **CANCEL** buttons.

## 🎮 How to use

1. Open the game's **Upgrade Offers** screen and click a ring.
2. Press **CONFIRM** to select it, or **CANCEL** to return to the same offers.
3. To choose a different ring, press **CANCEL** first. Ring cards and **SKIP** cannot be used behind an open confirmation.
4. To skip an upgrade, press the game's **SKIP** button, then confirm or cancel that action in the same way.

There is no extra hotkey. Only pending manual choices can be cancelled; already applied rings cannot be undone. The game's automatic ring-selection timer is unchanged.

## ⚙️ Configuration

The mod is enabled by default. BepInEx creates `BepInEx/config/com.thereisnouser.stonewards.undoringselection.cfg` with:

```ini
[General]
Enabled = true
```

Set `Enabled = false` to disable confirmation for both ring selection and **SKIP**.

## 📦 Installation

Install [UndoRingSelection on Thunderstore](https://thunderstore.io/c/stonewards/p/thereisnouser/UndoRingSelection/) using **r2modman** or another compatible Thunderstore mod manager. Launch **Stonewards** through the manager.

The package declares **BepInExPack 5** as a dependency.

## 🛠️ Development

### Building

Building requires a .NET SDK, the Stonewards game assemblies, and BepInEx 5 core assemblies from an existing mod-manager profile. Close the game before installing a rebuilt DLL.

From the repository root, run in PowerShell:

```powershell
$gameDir = Read-Host 'Stonewards installation folder'
$profileDir = Join-Path $env:APPDATA 'r2modmanPlus-local\Stonewards\profiles\stonewards-undo-ring-selection'

# Build and install the DLL into the development profile.
powershell.exe -NoProfile -ExecutionPolicy Bypass `
    -File .\build-and-install.ps1 `
    -GameDir $gameDir `
    -ProfileDir $profileDir

# Or build a Thunderstore upload archive without installing the DLL.
powershell.exe -NoProfile -ExecutionPolicy Bypass `
    -File .\package-thunderstore.ps1 `
    -GameDir $gameDir `
    -ProfileDir $profileDir
```

The profile must already contain BepInExPack. The development profile name matches the repository name. The first script installs the DLL under `BepInEx/plugins/StonewardsUndoRingSelection/`; the packaging script writes `dist/UndoRingSelection-<version>.zip`, using `thunderstore/README.md` for the archive's README.

### Implementation

Harmony intercepts the game's ring-card click/submit handlers and manual skip callback before the action is committed. A UI Toolkit modal blocks background pointer input, and the handlers reject additional card/skip submissions while a confirmation is pending. **CONFIRM** calls the game's original selection or skip handler; **CANCEL** discards the pending action.

## 🔗 Links

[Thunderstore](https://thunderstore.io/c/stonewards/p/thereisnouser/UndoRingSelection/) · [Report an issue](https://github.com/thereisnouser/stonewards-undo-ring-selection/issues) · [Changelog](CHANGELOG.md)

---

*Made by thereisnouser*
