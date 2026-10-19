# Mealsy Android startup fix — ce116fe

[Download the test APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-ce116fe/Mealsy-ce116fe-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, commit ce116fe.

Fixes stale native startup references in the previous af9600c APK. Clean Release
packaging and a new bundled-assembly consistency check prevent that mismatch.
Splash artwork now includes the Mealsy title. All previous keyboard, meal-plan
import and optional recipe photo features are retained.

337 app tests pass. Android builds pass. Startup metadata check rejects the old
APK and passes this APK on all four ABIs. Signature, alignment and archive verified.
Software API 29 emulator reproduced the old native crash; a clean rebuild passes
runtime initialization. Android System UI freezes prevented complete interactive
verification in that emulator. Cold launch and normal usage need phone testing.

Android 6+. Same cloud test signing key as previous cloud APKs; install as an update.
No uninstall or data clearing is required. If your installed app has a different
signing key, preserve/export local-only data before replacing it. Play billing needs
a Play-installed build. No Firebase deployment or schema changes in this update.
