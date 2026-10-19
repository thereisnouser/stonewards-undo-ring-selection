# Undo Ring Selection (0.1.0 prototype)

A client-side BepInEx 5 mod to **prevent accidental ring picks** when the level-up choice menu opens under the mouse cursor.

## How it works

1. Click a ring: **selection is held**, not committed to the game.
2. Click a different ring to change your mind.
3. Click **CONFIRM** to run the game's original ring-selection handler.
4. Click **CANCEL** to discard the pending choice and return to the ring selection screen.

The mod does **not** remove an upgrade after it has been confirmed. Rolling back already-applied upgrades is unsafe for complex effects and network synchronization.

The mod affects only the `RogueLikeUpgradeMenu.OnUpgradeClicked` ring-selection path. It does not change reroll costs, upgrade lists, or save data.

## Build and install (Windows / PowerShell)

Requirements: Stonewards installed locally, BepInEx 5 installed in an r2modman profile, .NET SDK.

Use a profile named exactly `stonewards-undo-ring-selection`.

```powershell
.\build-and-install.ps1 `
  -GameDir 'F:\Steam\steamapps\common\Stonewards' `
  -ProfileDir 'C:\path\to\r2modmanPlus-local\Stonewards\profiles\stonewards-undo-ring-selection'
```

The command compiles against **your locally installed** game binaries and copies `StonewardsUndoRingSelection.dll` to `BepInEx/plugins/StonewardsUndoRingSelection/` within that profile. No original game files are edited.

Check `BepInEx/LogOutput.log` for `Undo Ring Selection 0.1.0 loaded`.

## Test checklist

- Start a level-up ring draft and click ring A. The game should NOT finalize the pick.
- Click ring B. The confirmation UI should change the pending pick, not spend a reroll.
- Click CANCEL, then pick ring C and click CONFIRM. Ring C should be applied once.
- Test on solo and multiplayer as a **client**, then as host.
- During a pending choice, reroll if allowed. The old pending choice must disappear.
- Try it with any installed ring-affecting mods, especially UpgradeQueue; do not assume compatibility without testing.

## Known limitations

- **Prototype, not yet built or tested inside the game.** This repo currently contains source and a local build script, not a compiled DLL.
- The popup is a simple UI Toolkit confirmation panel rather than an exact copy of the game's pixel-art widgets.
- If the game version changes callback semantics or menu layout, it may require adjustment.
- Game-controlled auto-picks, timer expiry, and multiplayer network timing need validation in the actual game.
- If the mod encounters a UI construction error, it allows the game's original click to proceed rather than trapping the player in the upgrade screen.

## Configuration

`BepInEx/config/com.thereisnouser.stonewards.undoringselection.cfg`:

```ini
[General]
Enabled = true
```

## Credits

Based on observations of the game's API from the public source of [UpgradeQueue](https://github.com/darkharasho/stonewards-upgrade-queue) and the existing Stonewards modding approach. Independent implementation.
