# Mealsy Android Play policy readiness audit — 22 October 2026

## Rechecked locally — 23 October 2026

**A Release AAB was built, signed and validated, but remains blocked for Play submission.**
See [current bundle and signing record](PLAY_RELEASE_2026_10_23.md). This section
supersedes the historical source-only/undeployed findings below.

- Current Play phone/tablet submissions require **API 36** from 31 August 2026;
  the actual bundle targets **34**. [Official target requirements](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en).
- MAUI 8 is unsupported. Migrate the runtime, SDK and dependencies together to a
  supported release, then build/test for the required target.
  [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui).
- The Release bundle contains 332 inspected 64-bit native libraries; **173 fail
  16 KB ELF alignment**, including runtime/SQLite/AOT libraries. Google's current
  page states a 1 February 2027 update-enforcement date; this remains a device
  compatibility defect to fix before release, independently of the API blocker.
  [Official page-size guidance](https://developer.android.com/guide/practices/page-sizes).
- Direct NuGet advisory-feed comparison of 228 resolved packages identifies
  **Newtonsoft.Json 9.0.1** and **SQLitePCLRaw.lib.e_sqlite3.android 2.1.8** as
  affected by published high-severity advisories. Update their dependency chains
  and recheck; ordinary `dotnet list package --vulnerable` could not evaluate
  this legacy project, so it did not produce a clean scan.
- Policy/deletion URLs are live with current text. Firebase handoffs are deployed,
  indexes READY, TTL ACTIVE and recovery enabled; end-to-end disposable-account
  deletion and two-phone tests remain outstanding.
- Bundle manifest is non-debuggable with backup disabled and no legacy AD_ID,
  but includes ACCESS_ADSERVICES_AD_ID/ATTRIBUTION/TOPICS from bundled SDKs.
  Review or remove unused ads dependencies during migration and verify actual
  traffic/consent/Data safety; disabled ads alone does not prove no collection.
- Play Console setup has not been inspected: products/trial, licence testing,
  reviewer credentials, Data safety, audience/content rating and pre-launch checks
  remain acceptance items. Applicable new personal accounts need 12 continuously
  opted-in closed testers for 14 days before applying for production access.
  [Official testing requirements](https://support.google.com/googleplay/android-developer/answer/14151465?hl=en).

No bundle was uploaded and no store release was created.

**Decision: suitable for private testing, blocked for Play production submission.**
This is a repository/artifact audit, not Google approval or a legal certification.
Current Google policy pages could not be retrieved: outbound requests to
support.google.com returned proxy CONNECT 403. Verify the live requirements in
Play Console at submission; API 35 below is a known historical floor, not a
claim that it is the latest 2026 requirement.

## Findings and changes

| Area | Evidence and status | Required before production |
|---|---|---|
| Target SDK | Manifest explicitly targets API 34; global.json/MAUI use .NET 8. API 34 is below the API 35 upload floor introduced in 2025. | Migrate to a supported MAUI/.NET/Android toolchain and meet the then-current target requirement. Do not merely relabel the manifest. |
| 16 KB page support | Prior APK 4b2bdff: 16 64-bit native libraries inspected, 15 fail ELF and/or ZIP 16 KB checks. Runtime, compression, crypto and SQLite binaries are affected. | Upgrade/rebuild native dependencies; check final AAB and test on a 16 KB emulator/device. `audit-android-artifact.py` reports final APK evidence separately. |
| Privacy notice | Public policy/deletion pages now have a dedicated gh-pages snapshot and optional Pages workflow. App URLs point to the planned GitHub Pages paths; policy/contact and both root/project-path navigation are checked locally. | Enable GitHub Pages and verify both URLs anonymously before entering them in Play Console. See PUBLIC_POLICY_HOSTING.md. This environment cannot enable Pages or verify the hostname (network-policy 403). Confirm disclosures against deployed services. |
| Account deletion | New authenticated callable, durable job, recent-auth check, explicit DELETE/owned-space consent, cleanup worker/recovery schedule and rules/callable guards supplied. App offers password review, owned-space summary and request receipt. External email fallback page supplied. | Deploy functions, rules AND indexes, enable TTL, exercise staged full deletion/retry and mailbox verification. App code alone cannot delete production accounts before server deployment. See ACCOUNT_DELETION.md. |
| Optional diagnostics | App/default platform collection disabled when no choice is saved; explicit existing choices preserved. Custom reporter sanitizes data; native SDK still has its own metadata. | Verify runtime opt-in/out on physical devices and declare crash/device data appropriately in Data safety. Already uploaded reports/provider retention are not erased by a toggle. |
| Private cache protection | Android allowBackup/fullBackupContent disabled; account-specific local SQL, drafts, preferences and libraries are cleared after accepted deletion. | Verify backup/device-transfer behaviour and offline copies on other devices. Local SQLite/preferences are not encrypted storage; no claim of encryption at rest. |
| Advertising ID and ads | AD_ID excluded from merged manifest in this source. Release ad requests remain disabled via LiveAdsConfigured=false and empty IDs/policy URL. Ads/UMP packages still bundled. | Inspect final manifest/SDK behaviour. Before enabling ads, configure real AdMob IDs, policy, applicable consent/age handling, AD_ID declaration if needed, app-ads.txt and Data safety. Presence of an SDK is not proof of zero collection. |
| Payments and trial terms | Play Billing v8, local store price, selected offer token, conditional seven-day offer, renewal/cancellation text and server verification. Extra places independent. | Configure products/offers; Play-installed licence testing, acknowledgement, pending purchases, restoration, cancellation/renewal/RTDN and account ownership. Sideloaded APK cannot establish live checkout. |
| Social/UGC | Frontend feature flag and callable socialHandler are disabled in source, including activity delivery. Existing account-owned legacy content cleanup included. | Confirm deployed functions/rules match. Re-enabling social requires another UGC/moderation/report/block policy review. |
| Permissions | Internet/network, optional notifications and bounded legacy photo read permission. MediaPicker is used; no broad modern photo/video read permission intentionally added. | Inspect merged APK inventory, justify each permission, test scoped photo picker and notification denial. Keep optional permissions contextual. |
| Data safety | Auth/profile/recipes/photos/household data, optional dietary data, purchase tokens/status and optional crash reports appear in source. Hosting frontend config is still a placeholder. | Complete the declaration from final release + deployed services, not this table alone. See PLAY_DATA_SAFETY.md. |
| Listing/audience/access | No Console records or reviewer access inspected. Hidden social features should not be advertised. | Accurate screenshots, supported-site caveat, content rating, target audience, ads declaration, reviewer credentials and access to gated functions. Do not target children without a separate Families review. |
| Signing/release | Test APK uses existing cloud development signer, version code 1 and legacy build chain. | Create/store production upload key securely, configure Play App Signing, use increasing version codes, upload AAB and review Play pre-launch report. Do not upload the development-signed test APK as production. |

## Scope and limitations

Reviewed Android manifest/build configuration, runtime ads/diagnostic controls,
account/auth UI, billing/trial selector, data/cache storage, public hosting source,
backend access guards, rules, deletion lifecycle and existing social pause. Tests
exercise local data isolation and backend models; no live account was deleted.
No claim is made about actual Firebase IAM, deployed rules, endpoint versions,
Google Play account setup, billing/AdMob behaviour or a native device session.

The new deletion marker adds a bounded server read to authenticated callable
checks and rules lookups for personal-data requests. Recovery runs every five
minutes and reads at most five ready jobs per batch; deletion work is paginated
and leased. These add normal Firestore/Functions/Scheduler usage. Confirm billing,
budget alerts and queue monitoring; no guarantee of zero cloud charges.

## Primary policy references to verify in browser/Console

- Developer Program Policies: https://play.google.com/about/developer-content-policy/
- User Data: https://support.google.com/googleplay/android-developer/answer/10144311
- Account deletion: https://support.google.com/googleplay/android-developer/answer/13327111
- Data safety: https://support.google.com/googleplay/android-developer/answer/10787469
- Target API: https://support.google.com/googleplay/android-developer/answer/11926878
- 16 KB pages: https://developer.android.com/guide/practices/page-sizes
- Subscriptions: https://support.google.com/googleplay/android-developer/answer/9900533

Run the artifact probe with `python3 scripts/audit-android-artifact.py APK --aapt
/path/to/aapt --output report.json`. It reports evidence, not a compliance badge.


## Validation record

Android Debug build passed (one existing OnBackPressed deprecation warning).
422 shared tests and 121 backend model tests passed. The added Firestore emulator
integration test was NOT run: the emulator JAR download returned HTTP 403 Domain
forbidden. No live Firebase account, subscription or deployment was modified.
The test APK artifact includes a separate machine-readable manifest/native probe
report. Native device deletion and final Play pre-launch review remain open.
