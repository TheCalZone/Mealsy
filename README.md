# Mealsy compact widget test — c0560ac

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-c0560ac/Mealsy-c0560ac-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, c0560ac.

Compact Add, Choose list and Open list icons replace large widget buttons.
Add opens a native entry dialog over the launcher, rather than the main Mealsy
screens. Standard Android widgets cannot embed an editable text field.
Recognises quantities such as 500g minced beef, retains offline personal saves,
and checks household access before adding. Widget additions refresh an already
open shopping list while retaining item-edit fields.

Android 6+. Signed with the same cloud test key as previous test APKs; install as
an update. Resize or remove and re-add an existing widget for the smaller layout.
352 automated tests pass and Android build passes. Signing, archive, alignment
and native startup-reference checks are verified. Physical phone widget/keyboard
and launcher behaviour still need testing. No Firebase backend changes required.
