# Mealsy shopping test — 385228f

[Download signed APK](Mealsy-385228f-test.apk).

Source: `codex/android-runtime-fixes`, commit `385228f`.

New small ↕ control beside each shopping item: move up/down within its section or
edit/change section. Your item order saves per list/account on this device and
survives household sync, filters and reopening. Other members keep their layout.

New items without a category use local recognition: `500g minced beef` becomes
`minced beef`, quantity `500 g`, section Meat. Recognizes produce, dairy, bakery,
pantry, drinks, frozen, household, pet food and chilled alternatives. Explicit
non-Other categories and existing saved lists are kept. Leading amounts retain
quantity-field precedence. Social remains paused. Section drag reordering stays.

Android 6 or later. Same test signing certificate as previous cloud APKs. Android
may ask to allow installation from your browser. Different signing certificates
cannot update each other; preserve local data before deciding to uninstall.
Google Play purchases need a Play-installed test build. No Firebase deployment.

314 app tests and Android build passed. Signature, alignment and download checksum
checked. Native interaction still needs phone testing. Try repeated moves, filtered
lists, household sync, ingredient quantity/category recognition and manual category
overrides. The household meal-plan service still needs its documented backend.
