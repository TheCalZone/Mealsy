# Mealsy tester consistency update — 65f761d

[Download the signed Android test APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-65f761d/Mealsy-65f761d-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, commit 65f761d.

- Account and household shortcuts no longer quote fixed subscription prices.
- Upgrade cards/buttons use current Google Play prices. If the store is unavailable,
  pricing is explicitly unavailable and purchase buttons stay disabled.
- Shared household meal plans and private device-local plans are described consistently.
- Typed meals show on Home. Recipe book has direct recipe-link importing.
- Clearer household sign-in routes and themed buttons; improved plan/features navigation.
- Administrator patch notes include the update and the previous patch-notes feature.

338 app tests pass; Android Debug build passes without warnings. The Release APK uses
clean packaging and passes the native startup-reference check on all four ABIs.
Signature, alignment, archive and download checks are performed before distribution.
This update has not received a full physical-phone UI or two-account Firebase test.

Android 6+. Same cloud test key as earlier cloud APKs; install over the previous
cloud test build. No uninstall or data clearing is required. If replacing a build
signed with another key, preserve/export local-only data first.

Real purchases require Google Play test-track installation, available store products
and configured prices. Sideloaded builds may show Price unavailable. Use existing
administrator complimentary Premium grants to let invited testers try paid features
without buying. This update does not alter Play Console prices or Firebase deployment.
