# Mealsy iOS extensions

**23 October update:** The branch now includes the shared .NET/MAUI 10 migration
and modern Firebase bindings. Host/extension source build number is 2; helpers
derive defaults from the host project and propagate explicit archive overrides.
For the first app-only run use `bash scripts/run-ios-simulator.sh`; opt into
extensions with `IOS_INCLUDE_EXTENSIONS=1` after installing XcodeGen. The native
Swift/MAUI build and device acceptance below remain outstanding on the Mac.

Native source for an iOS 17+ medium WidgetKit widget and a browser Share extension.
Generated using XcodeGen; no Firebase SDK/dependency belongs in either extension.
The containing MAUI app owns authentication, recipe imports and shopping edits.

## Behaviour

- The widget's + opens the app's quick-add screen, checklist opens the selected
  list, sliders opens list selection. All widget instances use one private,
  account-bound selection on this iPhone. No list contents or credentials are
  exported to WidgetKit. No editable widget text field or background cloud polling.
- Quick add reuses the app's parser, suggestions, offline personal saves, online
  household access checks and existing sync/conflict handling. It opens Mealsy;
  it is not an Android-style text dialog over the launcher.
- Safari/browser Share → Share to Mealsy accepts one HTTPS URL. Save writes one
  atomic link file to the App Group inbox (maximum 20 pending links). It shows a
  Done confirmation; it does not use unsupported tricks to launch the host app.
- Open Mealsy to review one pending link. Review opens the existing importer;
  Later keeps the file, Discard removes it. Settings → Review shared recipe links
  processes more without relaunching. No import/network/save occurs before the
  user's existing Import & review action; Premium/site validation and photo
  selection remain in that screen. Switching accounts does not auto-import links.
- Files/photos, screenshots and plain-text-only shares are not accepted. This
  version handles provider URL attachments (including Safari). Other browser
  formats must use copy/paste until tested/added.

## Apple/Firebase hookup

1. Complete the MAUI/Firebase toolchain migration and Mac prerequisites in
   ../../IOS_SETUP.md. These extensions do not resolve that release blocker.
2. Register three explicit App IDs in the SAME Apple team:
   `com.companyname.recipeapp`, `com.companyname.recipeapp.share`, and
   `com.companyname.recipeapp.widget`.
3. Register App Group `group.com.companyname.recipeapp`. Enable it on the main
   app and Share extension and regenerate their provisioning profiles. The widget
   needs no group: it only contains static links. Enable the main App ID in Firebase
   as already described; the extensions do not need separate Firebase apps.
4. If changing bundle IDs, update the MAUI ApplicationId/Firebase config,
   project.yml, host/share entitlements, and the group constants in
   ExtensionNavigation.cs and ShareViewController.swift together. Run the checker.
5. Install XcodeGen (`brew install xcodegen`), select the matching Xcode/workload,
   then run `bash scripts/build-ios.sh simulator` from the repository root. It
   builds both appex products and passes their directory into MAUI bundling.
   `IOS_INCLUDE_EXTENSIONS=0` deliberately builds a client without them and hides
   their Settings/widget support entry points.
6. For signed builds install profiles for ALL three IDs. In addition to the main
   script's identity/profile/build-number variables, export `IOS_TEAM_ID`,
   `IOS_SHARE_PROFILE`, `IOS_WIDGET_PROFILE`. Set `IOS_APP_VERSION` if changing the
   marketing version. Run `bash scripts/build-ios.sh archive`. No provisioning
   profiles, signing private keys or Apple credentials should be committed.
   Build numbers/marketing versions are passed to host and extensions together.
7. Check the resulting .app/PlugIns contains both appex bundles. Validate all
   signatures, embedded profiles, nested bundle IDs and entitlements in the final
   archive; validate with Xcode Organizer before TestFlight. If the MAUI SDK's
   AdditionalAppExtensions behaviour changes during migration, verify its current
   embedding/signing guidance rather than ignoring validation errors.

## Required native checks (not performed in Linux)

Run the metadata checker: `python3 scripts/check-ios-extensions.py`.
Build both Swift targets and the MAUI iOS target on the Mac; no native compilation
or runtime success is claimed by this source-only Linux handoff.

On simulator AND physical iPhone:

- Add/remove the widget; exercise all three links with app cold, warm, signed out,
  wrong account, personal offline, active/expired/revoked household, no saved list.
  Confirm explicit re-selection after account switching; basic widget stays usable
  without account/list information visible on the Home Screen.
- Share from Safari and another URL-provider browser; cancel, save, Done, later,
  discard, review, repeated shares, full inbox, cold/resumed app and existing recipe
  import draft. Confirm no unexpected network access, duplicate recipe save or
  overwritten draft; import unsupported sites/free accounts through normal UI.
- Check shared storage works with distribution profiles, not only simulator.
  Verify main/share group entitlement equality, correct team, three matching
  version/build values, minimum OS, extension-safe APIs and privacy manifests.
- Test VoiceOver, large text and light/dark modes. No Apple transaction, ad,
  notification or social features are enabled by this implementation.

Reference: .NET iOS `AdditionalAppExtensions` build-item documentation:
https://github.com/dotnet/macios/blob/main/docs/building-apps/build-items.md#additionalappextensions

Linux handoff validation: all 408 shared .NET tests and the Android Debug build
pass, as do both iOS metadata checkers, shell syntax and MSBuild platform-condition
evaluation. Those checks do not compile Swift or iOS-only C# or validate native
extension activation. No signed iPhone build or backend deployment was performed.
