# Mealsy iOS client branch

## Latest handoff — 21 October 2026

Use **`codex/ios-client-support`** for the iOS handoff. It now merges the Android
source through `38bb44c`; this is the same shared app, not a separate rewrite.
The older preparation branch is no longer the version to build. Android's source
branch stays separate; iOS-specific follow-up work is committed here.

### Changes carried across

- Shopping creator Snacks/Essentials update in place; cupboard and review steps
  restore their positions after navigation or editing.
- Household shopping/current-week meals preload first. Home shows today's meals
  and remaining items, with timestamps and explicit refresh. One-minute read reuse,
  request coalescing, failure backoff and background-poll pausing apply on iOS too.
  Existing account sync is retained. No backend deployment is required for these.
- Ingredient recognition, pasted-list cleanup, suggestions, local recipe finder,
  quantity formatting, themes, languages, recipe/meal-planner improvements and
  account loading indicators all use the current shared source.
- iOS retains handle-only native MAUI item/section dragging. Sections now invoke
  compact-preview/restore callbacks and reject a drag if its account/list changes.
  Android's custom floating preview and continuous edge scrolling are Android-only;
  equivalent iOS animation/autoscroll is not implemented or verified.
- Browser Share to Mealsy and the home-screen quick-add widget remain Android-only.
  iOS uses link paste in the importer; no iOS share extension or WidgetKit target
  is supplied. Social entry points remain hidden on both platforms.

### Mac hookup and deployment entry point

1. Pull this branch on a Mac. Run `python3 scripts/check-ios-config.py` and
   `python3 scripts/ios-preflight.py`. The metadata check currently passes; the
   preflight correctly blocks the retained legacy .NET/MAUI/Firebase baseline.
2. Complete the toolchain/Firebase migration described below on the Mac. Native
   build/link tests must choose the compatible package/workload set; this Linux
   handoff has not upgraded or validated those dependencies.
3. Run `bash scripts/build-ios.sh simulator`. It selects the project's iOS target
   and the Mac's simulator architecture, and builds without signing. It runs both
   checks first and deliberately refuses the unsupported legacy baseline.
4. In Apple Developer/App Store Connect register the exact bundle ID, enable the
   chosen signing team, install its certificate/profile locally and confirm that
   the bundled Firebase iOS app belongs to the intended project. Test native
   Auth, permission-protected household reads/writes and account switching against
   that backend before distribution. Never embed admin credentials in the app.
5. After simulator and physical-device checks, export `IOS_SIGNING_IDENTITY`,
   `IOS_PROVISIONING_PROFILE` and a new positive integer `IOS_BUILD_NUMBER` locally,
   then run `bash scripts/build-ios.sh archive`. The script creates an archive/IPA;
   it does not upload. Validate the archive's privacy manifests, signing, version,
   symbols and entitlements in Xcode Organizer, then upload to TestFlight.
6. Start with internal testers. Exercise two family accounts, offline edits,
   expiry/revocation, day/week rollover, background/resume, scroll restoration,
   compact section drag/drop/cancel, photos, recipe import and invitation links.
   Confirm free access and existing Premium entitlement behaviour. Apple purchase,
   ad, push, widget and browser-share features must remain unavailable until their
   native/server implementations are completed.

**Status:** shared source synchronised and hookup instructions prepared; **not
App Store ready**. A signed iPhone build, native Firebase verification, supported
release migration, StoreKit/server verification and store disclosures remain open.
No Apple or Firebase production configuration was deployed by this change.

Validation on this handoff: all 408 shared tests pass; public iOS metadata checks
and shell syntax checks pass. The native iOS build attempt stops at NETSDK1178
because this Linux installation has no Microsoft.ios.Sdk.net8 workload. The
Mac build script correctly exits with a clear message on Linux. These checks do
not establish native iOS compilation, linking, signing or runtime behaviour.

## Previous assessment — 18 October 2026

The preparation commit has now been integrated into the latest Android branch,
including the newer shopping drag/autocomplete and account loading fixes. Social
and creator features remain paused on iOS too. Shared household shopping and the
new authenticated household meal-plan backend use the same accounts on Android
and iPhone; no second subscription per family member is required.

**The next milestone is a signed iPhone test build of recipes, shopping and shared
household plans, not enabling social, ads or Apple purchases.** This Linux cloud host
has no connected Mac, XcodeBuildMCP or iPhone simulator. No IPA/native iOS success
is claimed. The existing preparation is a development baseline, not a current
App Store release configuration.

### Release blockers, in order

1. Migrate the app to a supported .NET/MAUI 10 toolchain. .NET/MAUI 8 and the old
   Firebase wrappers/native bindings are the main blockers. Upgrade related UI
   packages together and remove the obsolete Xamarin CommunityToolkit compatibility
   package if its remaining use can be replaced. Keep Android regression tests
   passing throughout; don't merely rename the target framework and declare success.
2. Migrate Firebase Core/Auth/Firestore/Crashlytics together to a compatible current
   binding family. The maintainer's current Plugin.Firebase guidance targets .NET 10
   and uses AdamE iOS bindings. Avoid mixing old Xamarin.Firebase.iOS and AdamE
   bindings for the same native frameworks. Check actual APIs and native linking
   on the Mac before choosing/pinning the final dependency set.
3. Choose a matching Xcode/.NET iOS workload, rather than updating Xcode independently.
   Apple uploads require Xcode 26+ and iOS SDK 26+. The .NET iOS release notes also
   warn that apps built using Xcode 27 must adopt UIScene lifecycle support or crash
   at launch. A current compatible MAUI workload must supply that lifecycle support;
   the retained MAUI 8 AppDelegate is not evidence of compatibility.
4. Build/run first on Simulator, then sign an `ios-arm64` build using your Apple
   development team and exact bundle ID. Verify Firebase startup, sign-in,
   two-account shared shopping, shared dinner editing/conflicts, offline drafts,
   photos, safe areas, iPad layout and invitation links. No signing keys belong in Git.
5. Add Apple transaction verification and StoreKit purchases/restoration before
   advertising iOS Premium purchases. APNs, native share extensions and WidgetKit
   remain separate work. This first iPhone test milestone doesn't depend on them.

On the updated Mac, run `python3 scripts/ios-preflight.py` and
`python3 scripts/check-ios-config.py` from the repository. The preflight is read-only
and reports the migration blocker until the actual target/dependencies are upgraded.
Return its output for the next native build step. The older commands below describe
the legacy development target; they do not meet today's App Store release needs.

Primary references checked on 18 October 2026:

- [Apple upload requirements](https://developer.apple.com/news/upcoming-requirements/)
- [MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui)
- [.NET iOS/Xcode release compatibility and UIScene notes](https://github.com/dotnet/macios/releases)
- [Xcode workload matching](https://learn.microsoft.com/en-gb/dotnet/ios/troubleshooting/xcode-requirement)
- [Firebase wrapper maintainer guidance](https://github.com/TobiasBuchholz/Plugin.Firebase)

The following section records what the original preparation branch supplied.

Branch: **codex/ios-client-support**, based on the Android/client work through
`909d8df`. Backend source, Firestore rules, Functions, production accounts and
commercial flags are unchanged. Do not merge this branch assuming App Store
purchases have been implemented.

## What this branch prepares

- macOS automatically includes the existing `net8.0-ios` target. Windows/Linux
  retain Android as the default; `EnableIosTarget=true` explicitly includes iOS.
- The application display name is Mealsy. Each platform gets one app icon, and
  obsolete iOS Debug/Release LLVM/debug overrides have been removed so the
  workload's build defaults apply.
- The iOS Firebase config is bundled from `Platforms/iOS/GoogleService-Info.plist`.
  Its bundle ID matches `com.companyname.recipeapp` and project `mealsy-b732b`.
  A missing file causes a clear build error; startup SDK errors use the same
  recovery path as Android rather than preventing local-tool startup.
- `mealsy://join?code=…` and `mealsy://welcome` are registered and routed through
  the AppDelegate to Get started, including app startup. Invitation validation,
  retained codes and confirmation use the shared code. This is a custom scheme,
  not verified universal links or deferred installation attribution. No iOS
  share extension is included; paste website links in the in-app import screen.
- Pages using the shared theme helper enable iOS safe areas. Existing iOS photo
  resizing and permission text remain in place. The optional banner is a renderable
  empty ContentView on iOS; no iOS ad request or unsupported native banner is made.
- Google Play checkout, restore, management and ad-consent controls are omitted
  on iOS. Premium status can be refreshed using the existing verified account API.
  Existing covered household members and administrator grants use that status.
  Basic recipes, shopping, planner, social activity and administration use the
  shared client and current backend. These paths still require device checks.
- Android widget menus are hidden on iOS and direct widget-help navigation gives
  a shopping shortcut. The Activity inbox remains available; iOS background phone
  alerts and promotional-credit purchases are explicitly unavailable.
- An app privacy manifest declares the app-specific Preferences and file-timestamp
  required-reason APIs. This is not a complete privacy disclosure or an audit of
  every transitive SDK; review the final merged archive before distribution.

## Mac prerequisites and build

You need a Mac, Xcode and its command-line tools, a compatible .NET/MAUI iOS
workload, and Apple signing/provisioning for physical devices. Apple Silicon
simulators use `iossimulator-arm64`; Intel simulators use `iossimulator-x64`.
Do not commit certificates, private keys or provisioning profiles.

This repository still pins **.NET 8 / MAUI 8 and old Firebase iOS bindings**.
The chosen workload must support your installed Xcode. A successful Android build
or metadata check does not establish iOS compile/link compatibility. Before an
App Store release, migrate to supported .NET/MAUI and native Firebase versions
that support Apple's then-required Xcode/iOS SDK, and test that migration on Mac.
This branch does not claim that the .NET 8 baseline meets current store submission
requirements.

On a compatible Mac, from the repository root:

```sh
git switch codex/ios-client-support
python3 scripts/check-ios-config.py
dotnet workload restore RecipeApp/RecipeApp.csproj -p:TargetFrameworks=net8.0-ios
dotnet build RecipeApp/RecipeApp.csproj -f net8.0-ios -p:TargetFrameworks=net8.0-ios -c Debug -r iossimulator-arm64 -p:EnableCodeSigning=false
```

The `TargetFrameworks` override limits Mac restoration/building to iOS instead of
also requiring Android tooling. For a device, use `ios-arm64`, select your Apple
development team and a provisioning profile for the exact bundle ID in your IDE
or signing properties, then install/debug normally. Do not change bundle ID
capitalization without registering the matching Apple and Firebase apps.

The public-config checker prints no API keys. A compatible Mac must also verify
that Firebase native libraries link and that the SDK can use the bundled plist.
If the Firebase app or project changes, download its actual public iOS config and
update endpoint/project settings together; don't put administrator credentials
in app resources.

## What still needs platform/backend work

1. **App Store purchases:** implement StoreKit subscription/extra-place purchase,
   restore, management and verified transaction delivery. The current billing
   backend verifies Google Play receipts only. Supporting Apple purchases requires
   server verification, Apple notification handling and entitlement mapping, not
   just new buttons. Do not trust a client Premium flag or redirect users to Google
   Play as an iOS purchase flow. App Store review eligibility needs assessment once
   Apple IAP is implemented; cross-platform account access alone is not an IAP
   implementation.
2. **Phone alerts:** register the iOS Firebase app/APNs credentials and signing push
   capability, implement APNs/FCM token registration, opt-in and notification routing,
   and test sign-out/account switching. The inbox works independently. No APNs
   entitlement is enabled here because a working provider/configuration is absent.
3. **Ads:** integrate current iOS Google Mobile Ads and UMP SDKs, iOS app/ad-unit
   identifiers, applicable consent/ATT handling, and privacy manifests before
   enabling ads. Do not use Android IDs or request tracking permission without an
   implemented need. This branch serves no iOS ads.
4. **Widgets/sharing:** WidgetKit and an iOS share extension are separate native
   targets with app-group/signing requirements. Neither is implemented here.
5. **Distribution:** create the App Store Connect app, configure TestFlight testers,
   privacy/support/account-deletion disclosures and a supported release toolchain.
   The existing public invitation landing page currently advertises Android only;
   add your real TestFlight/App Store URL there once one exists. There is no guessed
   App Store ID or pretend iOS download link in this branch.

## Validation and phone checklist

The cloud environment is Linux. Native iOS build was attempted and blocked by
`NETSDK1178` (Microsoft.ios.Sdk.net8 unavailable for this installation). No iPhone
simulator, IPA archive, code signing, TestFlight or native iOS runtime was verified.
213 shared .NET tests and the public iOS configuration checker passed. Android
regression build is checked independently; no backend deployment is required.

On Mac/iPhone, verify startup, native Firebase sign-in/registration/reset and
verification, two-account household editing and revoked access, template/trip
storage, email keyboards, safe areas, iPad rotation, photo selection/re-encoding,
recipe-link paste, social publication/moderation, Activity refresh, diagnostics
opt-out/delivery, and invitation links with the app cold and already running.
Check that free/expired accounts see no inert purchase buttons or Android widget
instructions and that existing Premium grants unlock the same local tools. Verify
that ads remain absent and no push permission prompt appears until implemented.
