# Android version 5 — 27 October 2026

Source: `1dd9c67` on `codex/android-runtime-fixes`, fast-forwarded from origin.
Includes simpler shopping options and clear-checked controls, the owner shopping
approval switch, and member colour choices. Earlier household crash and email
confirmation fixes remain included. Social and creator features stay paused.

Read `HOUSEHOLD_SHOPPING_SETTINGS_HANDOFF.md`, `FIREBASE_HANDOFF.md`,
`CLOUD_HANDOFF.md` and the existing production/release handoffs before building.
The new settings require household, householdQuestions and processAccountDeletion
to be deployed together; no new rules or indexes are required for this delta.

All three functions deployed successfully to `mealsy-b732b` on 27 October.
Read-only deployment metadata verification passed for required function states,
indexes, TTL and the account-deletion recovery scheduler.

Validation: 449 shared .NET tests and 148 backend tests passed. The Android build
uses .NET 10.0.402, MAUI 10.0.120, minimum SDK 24 and target SDK 36. Release output
is ARM64 and uses the existing Mealsy upload key.

## Signed bundle

`artifacts/android/Mealsy-v5-shopping-settings-upload-signed.aab` — 29,547,330 bytes.

SHA-256: `13fe0d29a7cca4f51b3e81f8289995160640f0f691b74f77e9edbf66ed59c4cb`

Bundletool validation and JAR signature verification passed. The exported manifest
confirms version code 5, minimum SDK 24, target SDK 36, backup disabled and a
non-debuggable build. All 171 native ELF64 libraries pass the 16 KB alignment audit.
No advertising ID permission is present. Generated APK ZIP alignment and Play's
own device acceptance are not established by these AAB checks.

Phone checks from the settings handoff remain outstanding: owner setting saves,
restricted additions in both approval modes, pending requests, recipe/widget
additions, colour refresh on another client, and member-removal/deletion cleanup.
This record does not establish Play approval or physical-phone acceptance.
