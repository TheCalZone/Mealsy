# Mealsy iOS client branch

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
