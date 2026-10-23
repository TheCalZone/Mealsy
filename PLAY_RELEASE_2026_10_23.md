# Android bundle review — 23 October 2026

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
