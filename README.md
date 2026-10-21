# Mealsy shopping scroll fix — 38bb44c

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-38bb44c/Mealsy-38bb44c-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, 38bb44c.

Snacks and Essentials switch in place without recreating the shopping creator's
scroll view. Tapping the selected checklist leaves it unchanged. Cupboard and
review steps remember their scroll positions when moving between steps or
returning from an item editor. A shorter checklist naturally clamps scrolling
to the available content. Selected items and searches remain intact.

Includes household-first startup loading: today's shared meals and shopping
summary on Home, shared one-minute cloud-read reuse, manual refresh, and paused
background shopping polling. Existing account sync remains after startup
preloading. No Firebase deployment required; network usage can still incur
normal Firebase charges.

Android 6+. Uses the same test signing key as previous APKs; install as an update.
408 automated tests and Android build pass. Release rebuilt cleanly and checked
for native startup metadata, signature, alignment, archive integrity and download
checksum. Scroll behavior and live household syncing still need phone testing.
