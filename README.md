# Mealsy handle-only shopping dragging — c5aebd8

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-c5aebd8/Mealsy-c5aebd8-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, c5aebd8.

Shopping sections now move only from the dedicated move icon, both in the main
list and on Reorder sections. Names, counts and card backgrounds scroll normally.
Item dragging remains handle-only. Floating previews, sliding neighbours,
outlined Drop here gaps and light haptics are retained.

Includes the existing offline saved-recipe ingredient finder. Web recipe
suggestions have not been added. No Firebase backend changes are required.

Android 6+. Same cloud test signing key as earlier APKs; install as an update.
378 automated tests and Android build pass. Clean release packaging, native
startup metadata, signing, alignment, archive integrity and download checksum
verified. Physical phone touch and animation testing remains necessary.
