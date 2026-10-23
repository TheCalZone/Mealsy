# Version 3 — household crash fix (23 October 2026)

Reproduced the reported process exit in a Release API 34 x64 emulator with an
isolated sample household: `HouseholdPage.Run` set `Page.IsBusy` during
`OnAppearing`, triggering a `NullReferenceException` in MAUI's
`Page.OnPageBusyChanged` before networking. Removed the page busy flag while
retaining the in-page loader and duplicate-action guard. Unexpected load errors
are now recorded through the existing opt-in diagnostic path.

The same emulator setup renders the household after the fix, expands a member
drawer and refreshes successfully; the app remains running. Sample data was used
only in an ignored test copy, never the production build or backend.

- 447 shared tests passed.
- Version code 3, minimum SDK 24, target SDK 36, ARM64 phone build.
- `artifacts/android/Mealsy-v3-household-fix-upload-signed.aab`
  SHA-256: `636bfc0f0d27a94977283634f07ad79c107798151c483123f204a75f74c064e4`
- `artifacts/android/Mealsy-v3-household-fix-phone-test.apk` generated from that AAB.
- APK v2/v3 signatures and 16 KB ZIP alignment verified; all 171 native ELF
  libraries pass the 16 KB alignment audit. Uses the same upload key as version 2.
- Real-phone confirmation and Play-generated APK acceptance remain outstanding.

# Android bundle review — 23 October 2026

## Rebuilt after the Android/.NET 10 migration

**Use `artifacts/android/Mealsy-v2-api36-min24-upload-signed.aab`, not the old API 34 bundle below.**
The old upload error is fixed in both project settings and the actual bundle:
minimum SDK **24**, target SDK **36**, version code **2**, version name **1.0**.
Package remains `com.companyname.recipeapp`. The upload certificate is unchanged.

- AAB: 29,538,496 bytes (28.2 MiB).
- SHA-256: `5c20b03e42ea75e32271befcfc05ce9efd4b935cb9aa1a277206177b9614359f`.
- ARM64 Android phones; x86/x64 and 32-bit ARM are not in this candidate.
- .NET SDK 10.0.402, Android workload 36.1.2, runtime 10.0.13,
  MAUI 10.0.120, JDK 21.0.12 and Android SDK/build tools 36.
- Firebase Core 4.2.0 / Auth 5.0.1 / Firestore and Crashlytics 4.0.0;
  initialized using the new activity-locator API, including widget quick-add.
- Toolkit 15.0.1, UraniumUI 3.0.0 and its new theme-resource registration.
- sqlite-net-pcl 1.11.285, SQLitePCLRaw 3.0.5, SourceGear SQLite 3.53.4,
  Newtonsoft.Json 13.0.4. Old native SQLite, AndroidX pins and legacy Xamarin
  compatibility packages removed from the resolved app graph.
- Ads/consent SDK dependencies excluded; no AD_ID or Ad Services permissions in
  the generated APK. Social remains paused. Billing and household features retained.
- Keyboard handler, nullable date-picker saves and safe Auth v5 error mapping
  migrated. Existing database filenames/tables and private account data preserved.

### Checks passed

- Release AAB build, bundletool 1.17 validation and jarsigner verification.
- 447 shared tests under .NET 10, including auth error handling and existing
  SQLite/recipe/planner/shopping tests.
- Official NuGet vulnerability scan (including transitive packages): no vulnerable
  packages reported by the current advisory sources.
- **171/171** inspected native 64-bit libraries pass 16 KB ELF alignment.
- Generated universal APK passes native ELF/ZIP inspection and
  `zipalign -c -P 16 4`; APK v2 and v3 signatures verify.
- Manifest non-debuggable, backup disabled, package/config identifiers match.

Phone test file: `artifacts/android/Mealsy-v2-api36-min24-phone-test.apk`.
Generated from this exact AAB by bundletool, signed with the same upload key.
It may require uninstalling a previous development-signed APK because that uses
a different signer. Preserve any device-only recipes/lists before uninstalling.
The old .NET 8 JNI-token checker is not compatible with .NET 10 assembly wrappers;
it is not used to claim startup success here.

Build logs retain existing deprecation/XAML optimization warnings (281 on the
Release publish); no compile errors or NuGet vulnerability warnings. Default
Debug APK signing failed separately with Java exit code 2; the verified Release
AAB and bundletool-generated APK above do not depend on that signing path.

### Still required before production

No connected device was available. Test cold launch, sign-in/out, existing SQLite
data upgrade, recipe editing/import, shopping drag/widget, shared roles/plans,
themes, notifications and diagnostics opt-in on an ordinary and 16 KB device.
No native startup or iOS success is claimed. Play-generated split APK acceptance,
pre-launch reports, licence-test purchases/trial/restore/cancellation and a full
disposable-account deletion lifecycle remain unverified.

Complete Play Console Data safety, audience/content rating, app access/reviewer
credentials and applicable closed-testing requirements. This AAB removes the
identified build/manifest/native-library blockers, not these release acceptance gates.
No Play upload/release or real-account deletion was performed by this migration.

### Repeatable build

`global.json` now selects .NET 10.0.402. Install its `maui-android` workload and use
JDK 21 / Android SDK 36. Local isolated tools are in ignored `.firebase/dotnet10`,
`.firebase/jdk21` and `.firebase/android-sdk`; they are not included in Git.
Use `scripts/build-android-bundle.ps1` with SDK paths, the existing upload key,
its password **file**, and alias. It explicitly targets ARM64 to avoid Windows
host-RID inference by `dotnet publish`. Back up the signing files as described below.

## Historical API 34 candidate — superseded

Created locally: `artifacts/android/Mealsy-release-candidate-api34-upload-signed.aab`
(56,749,734 bytes, approximately 54.1 MiB). This file is ignored, not in Git.

- SHA-256: `ed6c42b2e6aff7f9a47628188bad2831065f38717fc063cbd97703d6204c0147`
- Package: `com.companyname.recipeapp`, retained to match the user's Play product.
- Actual manifest: versionCode 1, versionName 0.1.0.1, minSdk 23, targetSdk 34.
  The manifest versionName overrides csproj's 1.0; consolidate before first upload
  and use increasing version codes for later uploads.
- Release trimming and AOT enabled, debuggable false, backup disabled.
- .NET SDK 8.0.201 / MAUI 8.0.3, Android SDK 34, JDK 17.0.14.
- Release publish passed with the existing OnBackPressed deprecation warning.
- Bundletool 1.15.1 validation passed; jarsigner verification passed.
  Self-signed upload-certificate and missing timestamp notices are expected for
  this Android signing key; structural validity is not Play eligibility.
- Artifact audit JSON: `artifacts/android/Mealsy-release-bundle-audit.json`.
- Advisory snapshot: `artifacts/android/Mealsy-dependency-advisories.json`.

## Upload key

The user confirmed no bundles have been uploaded yet. A distinct new RSA-3072
upload key was created without replacing the old Cal keystore or test signer.
The final candidate is signed only with `mealsy-upload`.

Ignored local files (never commit or publish the private key/password):

- `.firebase/signing/mealsy-upload-2026-10-23.p12` — private upload keystore.
- `.firebase/signing/mealsy-upload-2026-10-23.password` — generated password.
- `.firebase/signing/mealsy-upload-certificate.pem` — public certificate.

Certificate SHA-256:
`45:6D:8F:A7:51:ED:7A:53:79:01:45:EE:1A:39:7E:C1:2E:68:C6:23:0D:D6:7D:F2:04:01:1B:F4:FD:23:0A:3C`.

Back up the keystore and password securely off this machine before relying on
the key. Enrol Play App Signing during the first release. Google's app signing
certificate will differ from this upload certificate; register the correct Play
app-signing fingerprint for device APIs/app links when available. No registration
or Play upload was performed here. The public PEM is not a private signing key.

## Why this bundle must not be submitted as production

See [the updated blockers](PLAY_POLICY_AUDIT.md): target 34 vs required 36,
unsupported MAUI 8, 173 native 16 KB ELF failures, two dependency advisories,
pending release/device/billing/deletion acceptance and Console declarations.
An AAB cannot be installed directly on a phone; use generated APKs or a Play test
track after fixing upload eligibility. Existing phone APKs remain separate.

After the toolchain/dependency migration, rebuild from the reviewed release
source with this upload key, rerun bundletool/signature/native checks, generate
device APKs and test on normal/16 KB devices. This audit only checks ELF inside
the bundle, not ZIP alignment of Play-generated APKs or runtime behaviour.

Reusable bundle inspection (export the manifest with bundletool first):

```sh
python scripts/audit-android-bundle.py FILE.aab --manifest manifest.xml --required-target-sdk 36 --output audit.json
```
