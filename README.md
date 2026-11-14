# Mealsy Android v10 test APK — 14 November 2026

Source: `585aacb` on `codex/android-runtime-fixes`. ARM64 Release, min SDK 24, target SDK 36, version code 10.
Includes the UI review, early shopping preload/progress, duplicate paste choices and account/device section-order remembrance.

Development-signed test build, not a Play bundle or production upload key. Signature SHA-256: `05a4dc773d00be1736fa00d304be0ef736a771a7d39c55fd71b02febea9277ae`.
The latest Android build and 456 shared tests passed. Signature/archive and native 16 KB alignment checks passed. Physical-phone runtime acceptance remains outstanding.

If the installed app has a different signer, Android will reject updating. Uninstalling deletes device-local data; back it up first.

Read `LOCAL_DESKTOP_HANDOFF.md` on the Android/iOS source branch to continue development. This artifact branch is downloads only.
