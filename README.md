# Mealsy v4 Android test APK

Source: codex/android-runtime-fixes at 4d7f8ab. Version name 1.0, version code 4. Android ARM64, minimum API 24, target API 36, .NET/MAUI 10.

Includes the latest household crash fix, account-name shopping attribution and current email-verification display, together with multiple shopping destinations and household shopping questions.

Built in the cloud from the latest source using scripts/build-android-test-apk.sh. Development-signed sideload APK; this is not the desktop upload-signed bundle or a Play-generated APK. Signature, ZIP integrity and native alignment audited; no physical-phone or purchase test performed for this artifact. The old .NET 8 JNI-token checker is not compatible with this .NET 10 build.

A desktop/Play-signed installed build may reject this APK because its signer differs. Do not uninstall an existing build without first preserving device-only recipes, lists, plans and drafts. Prefer the Play test track to update a Play-installed build and test purchases.

See artifact-audit.json and SHA256SUMS for this exact file's validation metadata.
