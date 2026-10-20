# Mealsy typed-meal shopping suggestions — ba533f4

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-ba533f4/Mealsy-ba533f4-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, ba533f4.

Weekly shopping review now works without saved recipes. Typed meal names in slots
without recipe attachments suggest matching saved essentials. Suggestions are
unticked, show source meals and weekly occurrence counts, and use the usual
purchase quantity once per shop. Review amounts before adding. No ingredient
lists or quantities are guessed. Unmatched meals can prefill a new essential.
Includes the compact Android widget and native quick-entry dialog.

Android 6+. Same cloud test signing key as previous test APKs; install as an
update. 362 automated tests and Android build pass. Signature, archive integrity,
alignment, native startup metadata and download checksum verified. Physical
phone testing is still needed. No Firebase backend changes required.
