# Mealsy Android shopping test — 89cca75

[Download the signed test APK](Mealsy-89cca75-test.apk).

Source: `codex/android-runtime-fixes`, commit `89cca75`.

Includes reorderable shopping sections (Shopping → Reorder sections), saved per
list/account on your phone. Social and creator features are paused and hidden.
Recipes, essentials, shopping and household tools remain available.

Standalone Release APK for Android 6 or later, signed with the same cloud test
certificate as the previous Mealsy cloud downloads. Android may ask you to allow
installation from your browser. If your existing app uses a different signing
certificate, preserve its local data before deciding whether to uninstall it.
Google Play purchase testing requires an app installed through Play testing.

274 app tests and the Android build passed. APK signing and alignment were checked.
Native functionality still needs testing on your phone. No Firebase changes were
deployed. Shared household meal plans still need the backend in FIREBASE_HANDOFF.md.

Try quick-add, recipe import, cupboard/essentials checks, shared shopping and changing
section order. Confirm the order survives closing/reopening and switching lists;
checking off or syncing items should preserve it. Household layout order is personal
to each device, so another family member can use their preferred store route.
