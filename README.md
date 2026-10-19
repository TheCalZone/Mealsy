# Mealsy Android test — af9600c

[Download Mealsy-af9600c-test.apk](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-af9600c/Mealsy-af9600c-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, commit af9600c.

- Android forms resize above the keyboard; recipe Preview/Save remain in a fixed footer.
- Import recipes directly into a personal meal slot or attach to a household meal.
- Optional recipe photo checkbox; missing or blocked photos do not prevent importing.
- Review/save the recipe, then save your day or household meal to keep the plan.

Android 6+ standalone test APK. Uses the same cloud test signing key as earlier
cloud builds. If your installed copy has a different signature, preserve/export
any local-only data before replacing it. Google Play purchases require a
Play-installed build. No Firebase deployment or schema changes in this update.

Validation: 337 app tests pass; Android Debug build passes. APK signature and ZIP
alignment verified. Native keyboard behaviour, image decoding and live-site import
need phone testing. The SHA256 file allows verification of this exact download.
