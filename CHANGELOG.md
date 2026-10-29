# Changelog

## Unreleased — Confirmation dialog styling

- Center the confirmation on the actual `RogueLikeUpgradePopup` panel instead of the full screen.
- Restrict the 45% backdrop dimming to the upgrade offers panel; leave Inventory and Stats readable.
- Darkened the upgrade screen behind the confirmation dialog without blocking clicks outside the dialog.
- Adjusted the centered dialog to 620×270 for a narrower, slightly taller layout and strengthened its gold frame, burgundy header, and text contrast.
- Enlarged the **CONFIRM** and **CANCEL** buttons and enhanced their hover colors.
- No changes to ring selection, skip behavior, or upgrade costs.

## 0.1.2 — Centered confirmation and skip protection

- Center the confirmation window on the upgrade-selection screen.
- Require confirmation for manually clicking **SKIP** before the game consumes a skip.
- Keep the previous ring-confirmation flow and allow **CANCEL** to leave the current offers available.

## 0.1.1 — Thunderstore icon quality

- Restored the original 256×256 icon artwork without downscaling or palette reduction.
- No gameplay changes.

## 0.1.0 — Initial release

- Added a confirmation window before a manually clicked ring is applied.
- Added **CONFIRM** and **CANCEL** controls, allowing a different ring to be chosen without a reroll.
- Matched the game's panel style with a gold frame, stepped corners, bold text, and native ring names.
- Fixed stuck selection outlines and extra clicks after cancelling by intercepting card input before the game's selection animation.
- Added an `Enabled` configuration option.

