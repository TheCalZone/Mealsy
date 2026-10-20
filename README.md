# Mealsy continuous drag scrolling — 95a7379

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-95a7379/Mealsy-95a7379-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, 95a7379.

Hold a shopping item or section near the top or bottom edge to scroll steadily,
without needing to wiggle your finger. Speed increases gently closer to the
edge. The floating preview and drop gap update as rows scroll into view, including
after the original row has left the recycled viewport. Moving to the middle,
release, cancellation, focus loss and account/list changes stop scrolling.
Dragging remains restricted to move icons. Item moves remain within a section.

Android 6+. Same cloud test signing key as earlier APKs; install as an update.
391 automated tests and Android build pass. Clean release packaging, native
startup metadata, signing, alignment, archive integrity and download checksum
verified. Sustained touch/scroll behaviour still needs physical-phone testing.
No Firebase backend changes. Web recipe suggestions remain excluded.
