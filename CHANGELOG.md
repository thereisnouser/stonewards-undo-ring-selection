# Changelog

## 0.2.0 — Confirmation dialog visual overhaul

- Anchored the confirmation dialog to the game's real `RogueLikeUpgradePopup` container, centered at 50%/50%.
- Increased backdrop dimming to 75% behind the dialog inside **Upgrade Offers**.
- Refined dialog proportions to 620×270 with a darker background, brighter gold pixel frame, and a richer burgundy header.
- Improved text readability and made **CONFIRM** / **CANCEL** buttons larger and more distinct.
- Preserved the existing ring-selection and skip confirmation logic, including cancellation without consuming a reroll or skip.

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

