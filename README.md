# Mealsy offline autocomplete test — d594928

[Download signed APK](Mealsy-d594928-test.apk).

Source: `codex/android-runtime-fixes`, commit `d594928`.

Shopping quick-add and widget quick-add now suggest ingredients as you type.
Try `minced` for beef/lamb/pork/garlic/ginger, or `500g min` and choose minced beef.
Explicit quantity fields take priority. Choosing a suggestion fills fields only;
tap Add when ready. Suggestions use common ingredients, account-owned local recipe
and checklist names, current shopping items and local frequency history. No AI or
Firebase requests. Previously added drag handles and aisle recognition remain.

Android 6 or later, signed with the same cloud test key as previous downloads.
Allow installation from your browser if prompted. A differently signed local app
cannot update with this APK; preserve local data before deciding to uninstall it.
Google Play purchase testing requires a Play-installed build. Social remains paused;
no Firebase deployment or backend changes were made.

321 app tests and Android build passed. APK signature, ZIP alignment and download
checksum verified. Phone UI still needs testing: typing speed/keyboard, selecting
suggestions with and without a quantity, widget entry, frequent item ranking and
account switching without another account's personal ingredient suggestions.
