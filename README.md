# Mealsy animated shopping dragging — 16ad114

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-16ad114/Mealsy-16ad114-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, 16ad114.

Shopping items lift into a floating preview that follows your finger. Neighbours
slide aside, with an outlined Drop here gap showing insertion on release.
The same preview works for section headers and compact cards on Reorder sections.
Android starts dragging as you move, without a long-press delay. Light haptics
mark pickup and destination changes. Cancellation restores the original layout;
release commits via existing device-local ordering. Item moves stay within their
section. Includes earlier left-aligned quantities, local leftover recipe finder,
homepage recipe shortcuts, widget and meal-planner improvements.

Android 6+. Same cloud test signing key as earlier APKs; install as an update.
378 automated tests pass and Android build passes. Clean release packaging,
native startup metadata, signature, archive integrity, alignment and download
checksum verified. Physical-phone animation/dragging, scrolling and cancellation
still need testing. No Firebase backend changes.
