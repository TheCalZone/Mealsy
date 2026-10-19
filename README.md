# Mealsy startup theme and Options menu test — 52ef87a

[Download signed APK](Mealsy-52ef87a-test.apk).

Source: `codex/android-runtime-fixes`, commit `52ef87a`.

Includes Forest-green startup background with the existing food logo, correctly
proportioned. Native MAUI purple defaults are replaced with Mealsy green. Once
opened, Android's status bar follows the current app theme with white system icons.
The OS launch splash uses the default Forest brand colour before saved settings load.

Page toolbar actions are grouped under one `⚙ Options` control. Android displays
an anchored popup; fallback platforms use an Options sheet. Existing page actions,
command enabled states and changing labels are retained. Back navigation is kept.

The newer remote integration (52be1c5), including shared household meal-plan backend
support, social pause enforcement and iOS preparation, is preserved. This APK is
Android-only. No backend changes or deployments were made for these UI fixes.

Android 6 or later. Same cloud test signing key as previous APKs. Allow browser
installation if asked. Differently signed local builds cannot update each other;
preserve local data before deciding to uninstall. Play purchases require a
Play-installed test build.

321 app tests and Android build passed. APK signing, alignment and download verified.
Check cold launch, app resume, every colour theme and the Options menu on a phone.
Exercise Home/Account options, shopping Paste/Clear/Sync, recipe Favourite/Copy and
editor Preview/Save. Native appearance and menu placement need device verification.
