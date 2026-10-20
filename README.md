# Mealsy direct shopping drag — a4dea5a

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-a4dea5a/Mealsy-a4dea5a-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, a4dea5a.

Shopping names, quantities and notes sit together on the left; the right side
holds the drag handle. On Android, touch the handle and move without waiting for
a long press. A highlighted target indicates before/after placement on release.
Moving near the viewport edge scrolls the list. Order remains within the item's
section and is saved on this device for the chosen list. Checkboxes, tapping and
swiping outside the handle retain their actions.

Also includes the homepage recipe tap/long-press shortcuts and offline Use what
you have recipe finder, replacing the AI creator. Includes earlier widget and
meal-planner shopping suggestions.

Android 6+. Same cloud test key as previous APKs; install as an update. 371 app
tests pass. Android build, release startup metadata, signing, alignment, archive
and download checksum are verified. Physical-device touch/drag, scrolling,
keyboard and native menus still need testing. No Firebase backend changes.
