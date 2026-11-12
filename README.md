# Mealsy Android test — version 8

Source: `833a9f2` on `codex/android-runtime-fixes`.
ARM64 Release APK, signed using the cloud development key, not the production upload key.
Includes the UI simplification pass. Android 7.0/API 24 or newer.

Build, signature, archive and native 16 KB alignment checks passed. Shared source tests: 451 passed. Device runtime acceptance remains outstanding; emulator installation was unsuccessful. This is a test build, not a Play release.

Signer SHA-256: `05a4dc773d00be1736fa00d304be0ef736a771a7d39c55fd71b02febea9277ae`.
If an installed Mealsy uses a different signer, Android will reject the update. Uninstalling deletes device-local data; back it up first.
