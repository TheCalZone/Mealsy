# Mealsy drag-handle test — 9a69dd5

[Download signed APK](Mealsy-9a69dd5-test.apk).

Source: `codex/android-runtime-fixes`, commit `9a69dd5`.

Long-press the small handle next to a shopping item and drag above/below another
item within its section. No movement popup. Swipe an item to edit or change its
section. Item order saves per list/account on your device; household members keep
their own layouts. Search/collected filters and sync preserve ordering.

Includes earlier quantity extraction, expanded aisle recognition, draggable section
headers and paused social features. No Firebase deployment or backend changes.

Android 6 or later. Signed with the same cloud test certificate as prior APKs.
Allow installation from your browser if asked. Preserve local data if a different
signing certificate prevents updating a locally built app. Google Play purchase
testing requires a Play-installed build.

315 app tests and Android build passed. Signature/alignment/download verified.
Native touch gestures still need phone testing. Test dragging in both directions,
filtered lists, cancelled drags, list/account changes, swipe editing and household
sync. Cross-section item drops are rejected; change section by editing instead.
