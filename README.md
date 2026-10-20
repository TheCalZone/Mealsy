# Mealsy browser recipe sharing — 5ec24ad

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-5ec24ad/Mealsy-5ec24ad-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, 5ec24ad.

Open a recipe in your Android browser, choose Share, then Save recipe to Mealsy
(or Mealsy). The importer opens with the shared link filled in. Choose whether
to include a photo, then Import & review. Importing requires Premium and a
supported website; some sites block imports. Sharing never fetches or saves a
recipe automatically. Existing drafts are protected. Screenshot/file sharing
is not recipe importing. Web recipe discovery remains excluded.

Also includes compact section bundles: ingredients temporarily fold away while
a section is dragged and expand again on drop/cancel. Quantities, collected
checks and data are kept. Earlier icon-only dragging, animated gaps and
continuous edge scrolling are included.

Android 6+. Same cloud test signing key as previous APKs; install as an update.
403 automated tests and Android build pass. Clean release packaging, native
startup metadata, share target manifest, signature, alignment, archive and
download checksum verified. Native chooser/cold-start sharing and drag layout
still need phone testing. No Firebase backend changes required.
