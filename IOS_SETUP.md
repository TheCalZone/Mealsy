# Mealsy iOS setup — 23 October 2026

Use `codex/ios-client-support`. It includes shared work through `cb837b4`:
.NET SDK 10.0.402, MAUI 10.0.120, modern Firebase/UI/SQLite packages and the
household/backend handoffs. iOS widget and browser-share extension source is retained.

This is Mac build preparation, not a verified native build. This Windows session
cannot run Xcode, compile Swift/iOS, sign an IPA or launch an Apple simulator.
No Mac is connected; the first native build may expose further binding/link errors.

## First simulator run on your Mac

1. Install Xcode and its iOS Simulator runtime. Open Xcode once to complete setup
   and accept its licence. Select that full Xcode installation with xcode-select.
2. Install .NET SDK 10.0.402 for your Mac architecture, then from this repo run
   `dotnet workload install maui-ios`. Match the iOS workload to Xcode using the
   [official release notes](https://github.com/dotnet/macios/releases).
   Updating Xcode alone does not update .NET's iOS workload. Use `dotnet workload update`
   when required for the selected compatible combination; do not disable Xcode validation.
3. Open this repo in Codex on the Mac, then run:

```sh
git switch codex/ios-client-support
git pull --ff-only
python3 scripts/ios-preflight.py
bash scripts/run-ios-simulator.sh
```

The helper builds an unsigned simulator app, selects an available iPhone simulator
(preferring a booted one), boots it if necessary, opens Simulator, installs Mealsy
and launches it. Apple Silicon uses `iossimulator-arm64`; Intel uses
`iossimulator-x64`. No distribution certificate or Apple upload is needed.
Set `IOS_SIMULATOR_UDID` to choose a specific available simulator. A failed build
stops the helper before installation or app interaction.

The first run defaults to `IOS_INCLUDE_EXTENSIONS=0`: app-only, with widget/share
entry points hidden. This isolates core app issues from XcodeGen/native extensions
and App Groups. Include them once the app works:

```sh
brew install xcodegen
IOS_INCLUDE_EXTENSIONS=1 bash scripts/run-ios-simulator.sh
```

See [extension setup](native/ios/README.md). To build without launching, run
`IOS_INCLUDE_EXTENSIONS=0 bash scripts/build-ios.sh simulator`.

## Lifecycle and links

A registered `SceneDelegate` and single-window scene manifest now support the
Xcode 27 lifecycle. MAUI retains window creation and lifecycle/auth forwarding.
Warm and cold-start URLs use the existing invitation/widget handlers; navigation
waits for local/account startup. The AppDelegate URL callback remains a fallback.
See [Microsoft's migration guidance](https://devblogs.microsoft.com/dotnet/migrate-dotnet-maui-apps-uikit-scene-lifecycle/).
Test cold/warm `mealsy://welcome`, an actual invitation link and widget URLs on the Mac.
Metadata validation does not establish native callback behaviour.

## iPhone and TestFlight

Host bundle: `com.companyname.recipeapp`, with its matching Firebase configuration
for `mealsy-b732b`. Android and iOS share accounts, household lists and plans;
an invited iPhone member does not need a second owner subscription. Social remains paused.

For extensions register `.share` and `.widget` App IDs in the same Apple team,
plus `group.com.companyname.recipeapp` for host/share. Keep host/extension build
and marketing versions equal; helpers derive defaults from the MAUI project.
Certificates/provisioning and passwords stay local and must not be committed.

After native acceptance, set `IOS_SIGNING_IDENTITY`, `IOS_PROVISIONING_PROFILE`
and a new positive `IOS_BUILD_NUMBER` locally, then run `bash scripts/build-ios.sh archive`.
Extensions additionally need `IOS_TEAM_ID`, `IOS_SHARE_PROFILE` and `IOS_WIDGET_PROFILE`.
This creates an archive/IPA without uploading. Review signing, entitlements,
privacy manifests and symbols before TestFlight.

## Acceptance still required

- Simulator/device compilation and launch, background/resume, warm/cold links.
- Native Auth, email verification, account switching and diagnostic opt-in.
- Existing SQLite data upgrade, recipes/photos/import, themes, shopping and offline recovery.
- Two-account household lists/plans/roles/requests, invitations and expired access.
- Full disposable-account deletion lifecycle without losing another member's private data.
- Widget/share Swift compilation, embedding, App Group storage and interaction.

StoreKit purchase/restore and native APNs/background alerts remain unavailable.
iOS can use existing verified account/household access. Do not advertise checkout,
iOS push or verified extensions before implementation/acceptance. App Store privacy
forms and transitive SDK privacy manifests still require final review.
