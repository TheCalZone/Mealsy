# Mealsy language support — a7d98b7

[Download the signed Android test APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-a7d98b7/Mealsy-a7d98b7-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, commit a7d98b7.

First localization rollout: English, Spanish, French, German and Portuguese.
The app chooses a supported language from the phone's UI language at startup;
unsupported languages fall back to English. Settings → Language lets you override
that choice. Restart Mealsy to apply a change without interrupting an open editor.

262 messages are translated across all five catalogs, covering core tabs, account
forms, shopping controls, recipe-editing and meal-planning actions. This is not a
complete translation of every screen: advanced/admin content, some descriptions,
backend errors, native widgets and ingredient recognition still use English.
Recipe content, ingredient names and saved identifiers are never machine-translated.
Regional date/number-entry conventions and Google Play's currency prices remain
separate from the app-language preference. Shared buttons wrap and grow for labels.

351 app tests pass, including real satellite resources, catalog/placeholder parity,
regional fallback, preference migration and decimal-comma parsing. Android builds
pass. Release packaging uses the native startup-reference guard for all four ABIs;
translated assemblies, signing, alignment, archive and download are verified.
Physical-device language/layout and keyboard testing is still needed. iOS language
metadata is included in source; this APK does not verify native iOS behaviour.

Android 6+. Same cloud test key as previous APKs; install as an update. No data
clearing or uninstall is required. Preserve local-only data before replacing any
build signed with a different key. Real purchases require a Play test-track install;
complimentary Premium grants can cover testers. No Firebase changes or deployment.
