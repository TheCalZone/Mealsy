# Mealsy Android test build

[Download signed APK](Mealsy-610e0af-test.apk) for Android 6 or later. Signed with the same cloud debug key as previous test builds. Allow installation from your browser when prompted. This is a standalone test build, not a production store release.

Source: TheCalZone/Mealsy branch `codex/android-runtime-fixes`, commit `610e0af`.

Includes all earlier changes plus cleaned recipe and shopping paste from Notes, Markdown and other apps. Removes common checklist symbols, list markers and square brackets while preserving quantities, fractions, accents and normal brackets. Shopping checks retain their collected state. Copy one item per line and use Paste in Mealsy.

137 app tests and the Android build passed; APK signature verified. Real phone clipboard and visual checks are still needed. Firebase server configuration is unchanged.
