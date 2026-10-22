# Mealsy production handoff — desktop

Updated 22 October 2026. Start here when continuing locally or with a desktop
coding assistant. **Production submission remains blocked.** This handoff records
source/artifact evidence, not Google approval or confirmation of deployed services.

## Get the right source

```sh
git fetch origin
git switch codex/android-runtime-fixes
git pull --ff-only origin codex/android-runtime-fixes
git switch -c release/android-production
```

Commit `227f383` contains the public-policy hosting work. This handoff is added
after it. The iOS branch is `codex/ios-client-support`; its `9d16ab9` merge includes
the same policy links. Do not use `master` assuming all this work is there.
`gh-pages` is a public-site-only snapshot, not application source.

Keep local changes before switching branches. Use the Android branch for the
Android migration, then reconcile shared changes with iOS deliberately.

## 1. Activate and verify the public policies

Open https://github.com/TheCalZone/Mealsy/settings/pages and choose
**Deploy from a branch → gh-pages → /(root) → Save**. Wait for deployment, then
open both URLs in a private browser window without GitHub authentication:

- https://thecalzone.github.io/Mealsy/privacy/
- https://thecalzone.github.io/Mealsy/delete-account/

```sh
python scripts/check-public-policies.py
python scripts/check-public-policies.py --live
```

Use `python3` if that is your local command. App URLs already point here; put the
verified URLs in the corresponding Play Console fields. Cloud network policy
blocked both the settings API and live-site verification, so neither activation
nor public accessibility has been confirmed. If Pages is unavailable on the repo's
plan, use a separate public policy-only repository or Firebase Hosting and update
the app URLs; do not expose the app repository just to publish a policy.

In branch publishing mode, future policy edits must also be copied to `gh-pages`.
See [PUBLIC_POLICY_HOSTING.md](PUBLIC_POLICY_HOSTING.md) for the allowlist builder
and optional GitHub Actions alternative. Do not publish all of `firebase/desktop`
as a substitute: it includes a separate desktop application.

**Done when:** both URLs return the intended pages publicly, navigation works,
Callum@brightbark.co.uk is monitored, and the text matches deployed behaviour.

## 2. Upgrade Android properly, including native 16 KB support

Current source pins SDK 8.0.426, MAUI 8.0.3 and `net8.0-android`, targets API 34,
and uses application ID `com.companyname.recipeapp`. Confirm the final package ID
before creating the Play listing/products; changing it also affects Firebase app
registration, config downloads, signing fingerprints and billing setup.

Install an IDE/toolchain that supports your selected maintained MAUI release.
Check the current target-API requirement in Play Console; do not assume API 35
is still sufficient. Upgrade SDK/global.json, workloads, target frameworks, MAUI,
toolkits, Firebase bindings, AndroidX and SQLite together. Raising only the
manifest target does not fix the runtime or native libraries.

A cloud experiment tried SDK **10.0.402**, MAUI **10.0.120**, JDK 21, Android API 36,
Toolkit 15.0.1, UraniumUI 3.0.0, sqlite-net-pcl 1.11.285 and SQLitePCLRaw 2.1.11.
**It did not build and was not committed/pushed.** The empty production-readiness
branch is not a finished migration. Its first compile failure was
`Custom Handlers/CustomEditorHandler.cs`: ConnectHandler/DisconnectHandler using
AppCompatEditText no longer matched EditorHandler overrides. Resolve against the
actual platform view type in the selected MAUI version rather than removing the
keyboard behaviour. Further compile/runtime issues may appear after that fix.

Restore also warned about AndroidX Concurrent.Futures/Lifecycle version ranges,
vulnerable Newtonsoft.Json 9.0.1 and SQLitePCLRaw.lib.e_sqlite3.android 2.1.11.
Trace the dependency graph and choose supported replacements; do not suppress
warnings or assume that experiment's versions are suitable. Unused legacy
Xamarin.CommunityToolkit.MauiCompat and explicit AndroidX pins are candidates
for review. Old Firebase bindings also need compatibility review.

Update target-specific conditions in the csproj and scripts after choosing the
framework: `build-android-test-apk.sh` currently hardcodes net8.0-android. Review
iOS conditions too, without claiming its upgrade has been validated.

After migration, from the repository root (replace the framework if different):

```sh
dotnet --info
dotnet workload restore RecipeApp/RecipeApp.csproj
dotnet restore RecipeApp.sln
dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj
dotnet build RecipeApp/RecipeApp.csproj -f net10.0-android -c Debug
dotnet clean RecipeApp/RecipeApp.csproj -f net10.0-android -c Release
```

Verify the solution filename locally if your checkout differs. Build a clean
Release candidate with your production signing configuration; the repository's
test APK script uses a development key and is **not** a production signing recipe.
Do not commit upload keys/passwords or pass secrets in shared logs.

```sh
python scripts/check-android-startup.py PATH_TO_APK
python scripts/audit-android-artifact.py PATH_TO_APK --aapt PATH_TO_AAPT --output artifact-audit.json
```

Use current Android build-tools `apksigner verify --verbose --print-certs` and
`zipalign -c -P 16 -v 4` against the APK. The previous .NET 8 artifact had 15 of
16 inspected 64-bit libraries failing ELF and/or ZIP 16 KB checks, including Mono
and SQLite. No newer artifact has resolved that finding yet. Inspect the final
AAB and APKs generated from it with bundletool; passing an APK audit alone is
not proof that Play-generated packages are compatible.

**Done when:** clean builds/tests pass, no unresolved vulnerable/incompatible
dependencies remain, final target meets Console requirements, all shipped native
libraries and package alignment pass, and a 16 KB emulator/device launches and
exercises SQLite, Firebase and core flows. Also test a normal-page-size phone.

## 3. Deploy and exercise automatic account deletion

Read [ACCOUNT_DELETION.md](ACCOUNT_DELETION.md) and
[FIREBASE_HANDOFF.md](FIREBASE_HANDOFF.md). Implementation exists but has **not**
been deployed or tested against live Firebase by the cloud work.

Use Node 22, the repo's locked dependencies and the Firebase CLI. From repo root:

```sh
npm ci --prefix firebase/functions
npm --prefix firebase/functions test
```

From `firebase/functions`, run the existing Firestore integration suite with a
demo project (never point this command at production):

```sh
npx firebase emulators:exec --config ../../firebase.json --project demo-mealsy --only firestore "npm run test:emulator"
```

The integration test uses real emulator transactions/rules but does not replace
full deployed function/Auth/Scheduler testing. Its cloud run was blocked by an
HTTP 403 downloading the emulator JAR. Model tests passed; integration is pending.

Sign in locally with `firebase login`, confirm the selected project, and test on
a separate staging project with disposable accounts/households. Check app Firebase
configuration points at staging too. Review database region, functions event
region, IAM, billing, scheduled recovery and Pub/Sub billing configuration.

Deployment from repo root, **staging first**, replacing STAGING_PROJECT_ID:

```sh
firebase deploy --project STAGING_PROJECT_ID --only functions:households,firestore:rules,firestore:indexes
```

Deploy the whole updated codebase, not just the three deletion functions: callable
and billing guards prevent recreation of deleted data. Wait for indexes to be
READY and enable/verify the `accountDeletions.deleteAfter` collection-group TTL
policy. Verify comment/report indexes and the status/retryAfter queue index.
Only after staging acceptance, repeat the reviewed deployment against the intended
production project (existing repository project ID is `mealsy-b732b`).

Test owner/member/no-household cases, owned-space consent, recent/stale login,
duplicate/lost-response requests, failure/retry, account switching, startup local
cleanup, concurrent writes and purchase notifications. Confirm final Auth,
Firestore and memberships disappear; jobs reach completed after the later sweep;
TTL removes completed markers after the configured retention. Check other users'
private data survives. Monitor queue age/retries and verify email fallback handling.
Inventory any datasets added locally that repository cleanup does not yet cover.
Account deletion does **not** cancel/refund Play subscriptions; keep that warning.

**Done when:** deployed staging end-to-end tests and production configuration
checks pass, monitoring exists, policy retention is accurate, and the installed
app receives a request receipt and safely clears account-local data.

## 4. Store, billing and device acceptance

- Configure Play App Signing and a securely backed-up upload key. Increase version
  codes; current source version code is 1. Generate a signed **AAB**, not the cloud
  development-signed APK. Keep the final package ID consistent throughout.
- Configure subscription products/base plans/prices and the seven-day offer in
  Console. Confirm actual store eligibility/prices drive UI; never promise every
  account a trial. Test through a Play-installed internal release with licence
  testers: purchase, pending state, acknowledgement, restore, renewal/cancel,
  expiry and RTDN/server entitlement updates. Deletion does not cancel a trial.
- Ads are currently disabled (`LiveAdsConfigured=false`). Keep this truthful in
  declarations. If enabling them, finish real IDs, consent/age handling, app-ads.txt,
  manifest/AD_ID inspection and Data safety before release. Bundled SDKs still need
  review. Social features remain paused; do not advertise or re-enable them here.
- Complete privacy/deletion URLs, Data safety, audience, content rating, ads,
  reviewer credentials and accurate screenshots in Console. Use
  [PLAY_DATA_SAFETY.md](PLAY_DATA_SAFETY.md) and
  [PLAY_POLICY_AUDIT.md](PLAY_POLICY_AUDIT.md) as evidence prompts, not a declaration
  to copy without checking your deployed services and final binary.
- Smoke-test cold launch, sign-in/loading, household list/meal-plan sync on two
  devices, offline/reconnect, recipe importing, ingredient quantities, drag/scroll,
  widget entry, share-to-Mealsy, keyboard behaviour, themes/localisation, optional
  permission denial and diagnostic opt-in/out. Test upgrade over an existing
  install. Review Play's pre-launch report and complete whatever testing track
  requirements your developer account currently shows before production access.

## Evidence to leave for the next reviewer

Record commit, toolchain/package versions, test results, dependency review, final
AAB/APK hashes, signer fingerprint (not private keys), native audit, 16 KB/device
results, public URL checks, Firebase deployment/index/TTL verification and Play
internal billing results. Previous baseline: **422 shared tests, 121 backend model
tests and Android Debug build passed**; no native device, emulator integration,
public hosting, live billing or production deletion success was established.

Do not mark production-ready until these outstanding checks are evidenced. The
iOS branch still needs its own Mac/Xcode build, signing, device and store review;
Android success does not establish iOS readiness.
