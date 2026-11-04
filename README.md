# Mealsy v6 — compact shopping and floating quick add

Source: codex/android-runtime-fixes at 60f8661. ARM64 Android test APK, version code 6, package com.companyname.recipeapp, minimum API 24 / target API 36, .NET/MAUI 10.

Includes Compact/Comfortable item sizes, floating + item entry and configurable left/right button placement. Change both under Shopping → Options → Item size & + button position, List tools or Settings → Shopping. Existing quantity, suggestions, notes, household permissions and sync remain. Based on the latest desktop v5 rollout source.

451 shared tests and the Android Debug build passed. Release APK clean-published with scripts/build-android-test-apk.sh; signature, ZIP integrity and native alignment verified for this file. No physical-phone or purchase test performed here. No Firebase update required for this UI change. The old .NET 8 startup-token checker does not apply to .NET 10.

Development-signed in the refreshed cloud environment. Its signer differs from the desktop upload/Play signer and may also differ from older cloud APKs. Android can reject an update with a different signer. Do not uninstall an existing version without preserving device-only recipes, lists, plans and drafts. This is a sideload UI test APK, not the Play upload bundle; use the Play test track for store-purchase acceptance.

See SHA256SUMS and artifact-audit.json for this exact file.

Test signer SHA-256: 05a4dc773d00be1736fa00d304be0ef736a771a7d39c55fd71b02febea9277ae
