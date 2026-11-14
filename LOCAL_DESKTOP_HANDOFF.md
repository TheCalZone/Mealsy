# Mealsy — local desktop continuation handoff

Updated 14 November 2026. Read this first when continuing locally. This supersedes older handoff source/version and cloud-validation summaries; it does not establish current Firebase/Play Console state.

## Current source and test build

- Android application branch: `codex/android-runtime-fixes`; latest functional change `585aacb`, followed by this documentation update. Do not assume `master` contains it.
- iOS branch: `codex/ios-client-support`; merge `ee8218e` contains the same shared changes and preserves its native extension/lifecycle work. This handoff is also being merged there.
- Android package remains `com.companyname.recipeapp`, version name `1.0`, version code **10**, minimum SDK **24**, target SDK **36**, ARM64 phone build.
- Toolchain used: .NET SDK **10.0.402**, MAUI **10.0.120**, JDK **21**, Android SDK/build tools **36**. Use the repository project/global configuration as the source of truth.
- Latest APK: [Mealsy v10 test APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-585aacb/Mealsy-v10-585aacb-test.apk). Includes the UI pass, early shopping loading, duplicate prompts and remembered section order. Artifact branch `android-test-585aacb` is downloads only, not application source.
- Signed with a **cloud development key**, not the production upload key. Certificate SHA-256: `05a4dc773d00be1736fa00d304be0ef736a771a7d39c55fd71b02febea9277ae`. Same cloud key as the recent v8 test APK. A Play/desktop-signed install may reject updating from it. Do not uninstall without preserving device-local lists, plans, drafts and recipes.
- This APK is not a Play AAB. Never replace the existing Play upload key with the cloud development key.

## Preserve desktop work before updating

The user has done Firebase work locally. Inspect local uncommitted changes and local-only commits first; do not reset, clean or overwrite them. Use a separate worktree/checkout if needed. Do not commit credentials or signing material.

```sh
git status --short
git fetch origin +refs/heads/codex/android-runtime-fixes:refs/remotes/origin/codex/android-runtime-fixes +refs/heads/codex/ios-client-support:refs/remotes/origin/codex/ios-client-support
```

If the local Android branch already exists and switching is safe:

```sh
git switch codex/android-runtime-fixes
git merge --ff-only origin/codex/android-runtime-fixes
```

If the branch does not exist, create it with `git switch --no-track -c codex/android-runtime-fixes origin/codex/android-runtime-fixes`. A narrow fetch configuration has previously prevented tracking-branch creation; the explicit refspec above avoids fetching only master. If fast-forward fails, review and merge desktop commits deliberately, preserving Firebase work. Work on a new local continuation branch after reconciling.

## Recent behaviour and implementation

### Calmer UI — `833a9f2`

`DisclosureSection.cs` provides animated secondary-option drawers, respecting reduced motion. Account, Settings, household, recipes, shopping tools and planners use it. Main actions stay visible; sign-in/day saving/shopping review use footers. Recipe ingredient/step and meal actions use compact menus. `PageOptions.cs` filters current available actions and removes completed setup shortcuts. Privacy/deletion, subscription and admin routes remain accessible. Social is still paused. See [UI review and device checks](UI_FLOW_REVIEW.md).

### Early shopping loading — `2408d76`

`App.OnStart` calls `HouseholdWarmup.Start()` after pending deletion cleanup and before local recipe/library loading. Existing resume/sign-in calls use the same service. It now warms the main personal list alongside household status/list/current meal week. `CloudReadCache` coalesces requests and expires reads after one minute. No new background polling or historical-data fetching.

The personal preload only warms the read cache; it does not overwrite device state. Shopping renders saved personal rows before cloud access checks, shows a spinner and loading-specific empty text, then enables editing before awaiting list sync. Household access must still be confirmed before opening shared data. The existing rebase/conflict logic preserves pending edits. Personal preloading can add a read when someone launches without visiting Shopping; do not describe it as zero extra reads. Extra personal lists are device-local.

### Duplicate paste and section order — `585aacb`

`ShoppingListDocument.PasteDuplicates` compares parsed item names using case/whitespace normalization; leading quantities and iOS checkboxes are parsed first. Checks include existing checked rows and repetitions inside the incoming text. Paste offers **Keep both as separate items**, **Skip duplicates**, or **Cancel**. It does not silently sum amounts. Keep both retains independent quantities; skip leaves existing rows/amounts intact and keeps the first new occurrence. One Undo reverses the paste. Account/list context is checked again after clipboard/dialog awaits. This is exact normalized-name detection, not fuzzy synonym matching.

`AppendText` retains its default merge behaviour for legacy/internal callers; the shopping Paste UI supplies the chosen policy. Recipe importing still uses existing quantity combination.

Section order is now a stable account/device preference (`ShoppingListStore.StateKey + ".preferredSectionOrder"`) reused across personal and household lists. The first available old per-list order seeds it; later legacy list orders do not override it. Missing custom sections retain their positions. Reset explicitly stores an empty default so legacy values cannot reappear. Item order remains per-list. Other household members/devices are unaffected; no backend order field was added.

## Validation and limits

- Latest Android ARM64 Release build: passed, zero errors; existing compiler/XAML warnings remain (282 warnings in the latest run).
- **456 shared tests passed**, including five new duplicate/order regression tests. Cache coalescing, account separation and in-flight edit/rebase tests also pass.
- Test APK signature, ZIP integrity, target/min SDK and native ELF/ZIP 16 KB alignment audit passed. Audit JSON and SHA256SUMS are on the artifact branch.
- No new physical-phone UI acceptance is claimed. The software emulator previously failed during installation because its package service became unavailable. The user's earlier APK feedback is not validation of these latest changes.
- iOS has shared source only: no macOS/Xcode native build or runtime verification was possible here. Android success does not establish iOS readiness.
- These recent UI/shopping changes require **no Firebase deployment**. No live account creation/deletion, purchases, advertising campaigns or store submission was performed in this work.

## Recommended next work on desktop

1. Reconcile local code/Firebase changes with the latest branch without discarding either. Run `dotnet --info`, `dotnet workload list` and `dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj`.
2. Use a physical Android phone and dedicated test accounts. Cold launch with fast/slow/no network; open Shopping immediately and after preloading; check household default, personal selection, loading feedback, offline edits, account switch and membership revocation.
3. Paste checkbox lists containing repeated names, different quantities, checked items and new rows. Exercise all three dialog choices, Undo/Redo and household restricted-member requests. Change accounts/list while an operation is pending and confirm nothing is saved into the wrong context.
4. Reorder sections, switch between personal/household lists, restart, clear/repopulate, hide checked items and reintroduce an empty custom section. Check account isolation, old-order migration and restore defaults. Verify other household members' layouts do not change.
5. Follow `UI_FLOW_REVIEW.md` for small screens, themes, large text, reduced motion, keyboard footers and navigation. Include signed-in owner/adult/restricted and administrator paths without giving external testers the real admin account.
6. Inspect actual Firebase/Play/AdMob configuration locally. Use `PLAY_RELEASE_2026_10_27.md`, `FIREBASE_ROLLOUT_2026_10_23.md`, `FIREBASE_HANDOFF.md` and the focused household handoffs as context, not instructions to blindly redeploy an already-live backend. Verify deletion/privacy links, email confirmation and permissions against dedicated test data.
7. Check current Play Console version codes before producing another production bundle. Choose a code greater than the highest already uploaded, retain the exact package and existing upload key, and verify subscriptions/trial/restore and current ad configuration. `scripts/build-android-bundle.ps1` takes local SDK/JDK paths, keystore path, alias and a password-file path; keep those files private. Historical release artifacts are not the latest source build.
8. UK-first launch with an external two-week testing company is the user's intention. Confirm the company's scope and any account-specific Play testing requirements; passing two weeks alone is not production approval. Prepare dedicated reviewer/test accounts with the required non-admin entitlements. Current ad budget, platform choice and campaign details are not agreed. No paid campaign or spend is authorised; prepare proposals and obtain explicit approval before publishing/spending.
9. For iOS, use `codex/ios-client-support` on a Mac and follow the newest sections of `IOS_SETUP.md` and its simulator/helper instructions. Preserve native extensions and lifecycle routing. Reconcile old contradictory checklist entries against current source and verify Apple purchases, signing, Firebase, widgets/share extensions and any remaining platform gaps rather than claiming deploy readiness.

## Local Android build pointers

For an ARM64 development-signed Release APK (SDK/JDK paths must resolve on this desktop):

```sh
dotnet restore RecipeApp/RecipeApp.csproj -p:TargetFramework=net10.0-android -r android-arm64
dotnet clean RecipeApp/RecipeApp.csproj -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm64
dotnet publish RecipeApp/RecipeApp.csproj -f net10.0-android -r android-arm64 -c Release -p:AndroidPackageFormats=apk -p:AndroidKeyStore=false -p:RunAOTCompilation=false
```

Use this machine's installed MAUI/.NET 10 workload and JDK 21. On Unix, `scripts/build-android-test-apk.sh` wraps clean/publish; restore the explicit Android ARM64 target first if assets were last restored for x64/iOS. Development-signed output is normally `RecipeApp/bin/Release/net10.0-android/android-arm64/publish/com.companyname.recipeapp-Signed.apk`. For Play, use the existing upload keystore via `scripts/build-android-bundle.ps1`, not the development-signing command above. Never put passwords in chat, Git or command-line literals.

Suggested first instruction for the desktop assistant: **Read LOCAL_DESKTOP_HANDOFF.md, inspect local edits/commits and the latest remote Android branch, preserve my local Firebase work, then run the phone acceptance checks and prepare the next production-signed release for review. Do not publish or spend on advertising without my approval.**
