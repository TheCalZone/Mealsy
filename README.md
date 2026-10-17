# Mealsy Android shopping test — 8fc0e88

[Download the signed APK](Mealsy-8fc0e88-test.apk).

Source: `codex/android-runtime-fixes`, commit `8fc0e88`.

Includes long-press-and-drag shopping section headers and reorder-screen rows.
Optional movement buttons remain in the reorder menu. Section order saves per
list/account on this device. Social and creator features remain paused.

Leading amounts are recognized in quick-add, paste, widget additions and recipe
shopping: `500 g beef` becomes name `beef`, quantity `500 g`. A nonempty quantity
field takes priority. Recipe quantities are extracted before portion scaling.

Android 6 or later. Standalone Release APK signed with the same cloud test key as
previous Mealsy cloud downloads. Allow browser installation if Android asks.
A differently signed local build cannot be updated by this APK; preserve its
local data before deciding whether to uninstall it. Google Play purchases require
a Play-installed test build. No Firebase changes were deployed.

291 app tests and the Android build passed. Signature and ZIP alignment verified.
Test long-press gestures on your phone, reordering across sections and persistence
when reopening. Check `500 g beef` with an empty quantity and with an explicit
`750 g`, pasted lists, recipe portion scaling and household shopping sync.
