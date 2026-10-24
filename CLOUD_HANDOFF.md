# Mealsy cloud handoff — 17 October 2026

**Shared shopping UI, 24 October:** The iOS branch now includes Android changes through `5889c23`: simpler shopping options, visible list tabs and Clear checked, grouped list tools and selected-list retention. It also brings in the latest email-verification, household crash and account-name attribution fixes. Version code 5 is reserved for the next build. Shared tests: 448 passed; Android builds. Native iOS verification is still required.

**iOS branch preparation, 23 October:** `codex/ios-client-support` now merges the
Android source through `cb837b4`, retaining Swift widget/share source. Added a MAUI
scene delegate with warm/cold invitation/widget routing and a Mac simulator
build/install/launch helper. Extension versions follow the host build. See
[current Mac instructions](IOS_SETUP.md). Metadata/helper tests pass on Windows;
native iOS/Swift compilation, simulator launch, signing and device acceptance
remain unverified. The first app-only simulator run omits extensions by default.

**Email confirmation, 23 October:** Version code 4 fixes the Account screen's
unconditional confirmation prompt while social is paused. Account loading reloads
the signed-in Auth user and token, uses the current verification flag, and offers
resending only for accounts known to be unverified. Unknown/offline status has retry
copy rather than being treated as unverified. Sending also checks live status first
and skips delivery for already-confirmed users. No verification flags are set locally
or changed by the app; existing server enforcement remains authoritative.

**Shopping attribution, 23 October:** Household shopping tags now resolve the
contributor's account first/last name from `users/{uid}`, including existing items
and approved requests. Missing names fall back to “Household member”; emails are
not used as tag labels. Profile reads are limited to distinct current contributors
and the caller, within the membership-checked transaction. All 146 backend tests pass.
**Household crash, 23 October:** Reproduced with a sample household in an Android
API 34 x64 Release emulator build. `Page.IsBusy=true` in `HouseholdPage.Run`,
called by `OnAppearing`, crashes in MAUI 10 `Page.OnPageBusyChanged` before the
network request. Removed the page busy flag; the existing in-page loading control
and duplicate-action guard remain. Unexpected load failures now enter diagnostics.
Version code increased to 3 for the replacement Android build. Shared tests: 447 passed.

**Android migration, 23 October:** Source now uses .NET/MAUI 10, minimum SDK 24,
target SDK 36 and version code 2. A new upload-signed ARM64 AAB and phone APK pass
static bundle/signature/16 KB native checks; 447 shared tests and the transitive
NuGet advisory check pass. Start with [the current release record](PLAY_RELEASE_2026_10_23.md).
Device, store-flow and iOS acceptance remain open; social remains paused.

**Firebase update, 23 October:** See [current rollout evidence](FIREBASE_ROLLOUT_2026_10_23.md)
for deployed household roles/questions/attribution, admin confirmation and automatic
deletion infrastructure. Social remains paused; staging/device acceptance remains open.

For the remaining production-release work, start with
[PRODUCTION_DESKTOP_HANDOFF.md](PRODUCTION_DESKTOP_HANDOFF.md) (22 October 2026).
It records the latest hosting, Android compatibility, Firebase deletion and Play
Console blockers; the historical notes below do not establish production readiness.

## Localization: initial five-language rollout

Resources/Localization/Strings*.resx contains 262 matching messages in English,
Spanish, French, German and Portuguese. AppLocalization uses ResourceManager and
parent-culture fallback (es-MX → es, fr-CA → fr, pt-BR → pt). App initialization
resolves the phone UI language before creating Shell/forms; unsupported languages
fall back to English. Settings → Language stores a device-level override; restart
applies it without rebuilding a live Shell or abandoning an open editor. Resetting
preferences restores automatic language selection on the next launch.

Only explicit UI literals and selected display-only converters are translated.
Recipe text, ingredient names, account data, category IDs, sort tokens, route names
and serialized quantities are not rewritten. Settings and category pickers retain
canonical values and display translated labels. CurrentCulture stays regional,
separate from CurrentUICulture; dates/number entry retain phone conventions. Existing
comma-decimal parsing and Play's localized currency prices remain authoritative.
Shared buttons can wrap and grow for longer labels. iOS metadata declares the five
languages, but native iOS has not been built or tested here.

Coverage is an initial core rollout, not a claim that every screen is translated:
advanced/admin content, several long descriptions, native widgets, backend errors,
ingredient autocomplete/classification and some dynamic messages remain English.
User recipe content is intentionally never machine-translated. Further languages,
RTL layouts and region-specific ingredient vocabularies need explicit follow-up.
Add UI messages to every resx; use AppLocalization.Format for placeholders and the
Translate markup extension for XAML. Never pass saved identifiers through translation.

351 app tests pass, including real satellite-resource loading, parity/placeholder
checks for all language catalogs, regional fallback, settings migrations and
preserving decimal-comma input. Android builds verify the resource assemblies.
Check long labels, dialogs, language switching/restart and native keyboard interaction
on physical phones. No Firebase changes or deployment.

## Tester consistency pass

Account/household Premium shortcuts and the feature screen no longer display fixed
reference prices. PlanPage loads the monthly and annual labels from the same Play
base-plan offers used at checkout. Missing store products show Price unavailable,
with purchase buttons disabled; Refresh plans & prices retries. No guessed UK price
or savings claim is displayed. SubscriptionPricing retains historical UK reference
constants for documentation/tests only; this pass does not change Play Console
prices, billing product IDs or paid entitlements. Extra-place counts no longer quote
an assumed total; Play confirms the price before payment. Purchase preparation now
keeps its selected product/offer local so another price lookup cannot overwrite it.

Premium and Account copy correctly distinguish private device-local personal plans,
weekly templates, shopping-trip libraries and drafts from shared household meal
plans. Shared planners and household widgets use consistent Premium wording. The
recipe book has a direct import-link action. Home includes typed meals in today's
menu and uses standard shopping navigation; batch planning is labelled personal.
Household tools use themed buttons and provide sign-in/personal-planner routes.
Plan/features navigation returns to the existing plan screen instead of stacking
another subscription page. Admin patch notes include the pass.

338 app tests pass, including typed dashboard meals and missing recipe handling.
Android Debug build passes without warnings. A standalone tester APK is built via
the clean-publish/startup-reference guard. No Firebase changes or deployments.
Actual checkout prices and purchases need Play test-track installation/configured
products; a sideloaded APK can display Price unavailable. Existing administrator
complimentary Premium grants can cover non-purchasing testers. Confirm household
sync and invitations using two physical devices; these are not cloud-verified here.

## Administrator patch notes

Account → Administrator → Patch notes shows bundled newest-first change cards,
release dates and the installed app version/build. The notes button appears after
the existing administrator status check; account changes still invalidate actions.
This is a local read-only history, with no Firebase write, new endpoint or audit
entry. Content lives in Services/AppPatchNotes.cs; add a dated entry when shipping
future changes. Earlier social work is clearly marked as paused. The administrator
screen now keeps its Administrator title when social features are paused and only
the explicit complimentary Premium shortcut autofocuses the email field.

## Startup crash fix and Mealsy splash title

The af9600c APK reproduced a native Mono SIGABRT before App construction:
`icall.c:6231: !only_unmanaged_callers_only`. Its libxamarin-app.so startup tokens
were stale: Initialize 0x06002274 / RegisterJniNatives 0x06002273, while its actual
bundled Mono.Android.dll methods were 0x06002275 / 0x06002274. This is a Release
incremental packaging mismatch, not a Firebase or keyboard/form failure. No user
data/schema migration is required. The existing keyboard resize code is retained.

Use `scripts/build-android-test-apk.sh` to clean Release before standalone test
publishing with the existing Android development key. It runs the new Python
standard-library `scripts/check-android-startup.py APK` guard, which reads native
ELF startup tokens and the actual (LZ4-compressed) Mono.Android.dll inside the APK.
The guard rejects the original broken APK and passes the rebuilt APK for all four
ABIs. It supports this project's .NET 8 assembly-store layout; newer SDK layouts
must update the check rather than silently skipping it. Supply DOTNET_ROOT/PATH,
ANDROID_HOME/JAVA_HOME and the normal NuGet/cache selectors for your environment.
For production signing and Play bundles, use your production build/signing workflow.

Splash now uses mealsysplash.svg: the existing salad illustration plus a cream
Mealsy wordmark rendered as vector paths, with the same Forest background. Text is
outlined so Android resource generation does not depend on device fonts. Existing
icon and other resources remain intact. Android Debug clean build and 337 app tests
pass; final APK startup consistency is checked against its bundled bytes.

Software API 29 emulator reproduced the original runtime abort. A clean rebuild
passed that point into Firebase initialization and MAUI/Android view creation.
The emulator also suffered Android System UI ANRs under software emulation; do not
claim a complete interactive device pass from this run. Check cold launch and
keyboard/forms on a physical phone with the replacement APK. No uninstall or
app-data clearing is part of this fix.

## Latest additions: meal-planner importing and optional recipe photos

Personal day cards now offer Import recipe from link for each meal slot. Saving the
reviewed recipe returns to that day and adds it to the chosen slot; Save day commits
the plan. Household meal editing offers the same importer, then asks permission to
attach a shared recipe snapshot. Saving the household meal is still explicit.
Cancellation never attaches an unfinished recipe. Existing unrelated new-recipe
drafts block the meal-import flow rather than attaching the wrong draft. Recipe
imports remain Premium and existing account guards apply throughout.

Import screen includes a checked-by-default Save recipe photo, if available option,
and a busy indicator. Unchecking it skips photo extraction/download. Recipe JSON-LD
image strings, arrays and ImageObject URLs are supported, including relative URLs.
Photos use public HTTPS only, bounded streaming/timeouts/redirects and existing
FeedPhoto JPEG compression/metadata stripping. Private network addresses are blocked
at socket connection. Failed/missing photos do not prevent recipe import. Photo data
uses the existing recipe_image field; no backend schema or deployment is required.

337 app tests pass, including new photo metadata/URL tests. Android Debug build passes
without warnings. Phone checks: keyboard resize and scrolling; import with/without a
photo; unsupported image; cancelling/back navigation; importing for a personal meal
and a household meal; saving the day/household meal after attachment. Native keyboard,
image decoding and live-site imports still require device testing.

## Latest fix: keyboard-safe Android forms

Application uses Android Resize soft-input mode, and MainActivity declares/applies
AdjustResize, so the viewport shrinks above the keyboard rather than being covered.
Scrollable forms can reach their bottom actions while typing. RecipeEditorView now
keeps Preview/Save/status in a fixed footer outside its scrollable fields; the footer
is disabled with the form while busy. Top Options actions remain available too.
No recipe data or backend changes. Verify portrait/landscape, large fonts, multiline
method/private notes, ingredient fields, shopping edit Save and household forms on
a phone with its actual keyboard. Native keyboard interaction cannot be confirmed
by the platform-independent tests alone.

## Latest cooking feature: step ingredient checklists

RecipeReadView uses local StepIngredients matching to show suggested ingredients
inside method cards. Whole words/plurals/accents/preparation modifiers are handled;
longer compound names win over contained names (chicken stock vs chicken) and
ambiguous short references (two oils) are not guessed. Vague "remaining ingredients"
instructions require manual selection. Each step has independent temporary ticks,
separate from overall prep and shopping checks. Choose ingredients adds/removes
suggestions without changing the saved recipe. Repeated uses can be checked again.

Displayed amounts are recipe totals, not inferred allocations; follow step text
for half/divided quantities. All rendered totals follow serving adjustments.
No backend changes or AI calls. Tests cover matching, ambiguity, duplicate section
ingredients, repeated uses and preserving source quantities. Verify cooking flow
and manual selections on a phone.

## Latest UI adjustment: one Options control in page toolbars

Application PageAppearing groups each page's existing toolbar actions under a
single `⚙ Options` control. Android uses a popup anchored to that control; other
platforms (or a missing native anchor) use the standard Options action sheet.
Actions retain their existing Clicked handlers, live labels, command bindings and
CanExecute states. Shopping Paste/Clear remain bound to the current page model;
recipe favourite labels refresh when opening the menu. Back navigation is unchanged.
The helper also covers recipe editor Save/Preview and household planner actions.
No new social entry points or backend changes. Verify menu placement/selection and
startup/status-bar colours on a phone with the updated APK.

## Latest integration — 18 October 2026

The current branch incorporates iOS preparation commit `bfdd79e` with the newest
shopping drag/autocomplete and loading changes preserved. Social/creator features
remain deliberately paused; the server now enforces that pause too, including
publishing, moderation-heavy calls and social push delivery. The friend/privacy
backend was not implemented or re-enabled.

Shared household meal planning now has an authenticated `householdMeals` service,
idempotent versioned edits, conflict handling, safe recipe snapshots, membership/
owner-Premium checks and recursive household cleanup. See `FIREBASE_HANDOFF.md`.
The website release uses the paused community flag and shopping/invitation pages.

Current local validation: 321 app tests, 112 backend tests, seven Firestore emulator
tests, 14 browser tests, the iOS metadata checker and Android Debug build passed.
Functions and the paused Hosting release were deployed successfully on 18 October.
Nine live read-only checks passed, including the new household-meals endpoint,
the paused website flag and invitation/share landing pages.
No native iOS build or IPA is verified: the Mac is not connected to this session.
`IOS_SETUP.md` documents the .NET/MAUI/Firebase migration and matching Xcode workload
needed before a current iPhone release. Run `python3 scripts/ios-preflight.py` on
the Mac to collect its toolchain state without installing or changing anything.

## Latest update: Android startup and status-bar colours

Launch splash now uses the default Forest green (#214D35), with the existing food
logo sized square to preserve its proportions. Native colorPrimary/colorPrimaryDark
and MAUI fallback palette no longer use the template purple. Once the app starts,
ThemeService applies the current ThemePrimary to Android's status bar with white
system icons, and reapplies it on theme changes/resume. The native OS splash is
statically Forest-branded; saved palette selection applies after startup. No backend
changes. Verify cold launch, resume and each colour theme on a phone.

## Latest update: offline ingredient autocomplete

Shopping quick-add and WidgetQuickAddPage show up to five ingredient chips after
at least two typed characters. Built-in names include minced beef/lamb/pork/garlic/
ginger; account-owned local recipe ingredients, snacks and essentials supplement
these. Current-list names and a bounded per-account/device name-frequency history
help rank suggestions. Own/guest sources are separated and stale async loads are
discarded after account changes/unloading. No AI, Firebase reads or new backend.

Selecting a chip fills the name, extracts a typed leading amount if the quantity
field is blank, retains a nonempty explicit quantity and suggests an aisle while
respecting another manually selected category. It does not submit the item.
History is local names/counts only (up to 200), not notes or quantities. Personal
recipe/checklist vocabulary is capped at 1,000 names. Social remains paused.

## Latest adjustment: item handles replace movement menus

Shopping item rows now have a long-press drag handle instead of the ↕ action menu.
Only handles initiate item drags; the full item row accepts drops above/below its
midpoint. Drops reorder visible items within their existing section, preserving
hidden/collected item positions, other sections, IDs and item details. External,
cross-section and stale-account/list drags are rejected. Sync waits during item
drags; cancellation doesn't change saved order. Swipe-to-edit remains available.
Native gestures still need phone verification; this change adds no backend fields.

## Latest shopping update: item moves and category recognition

The item-row ↕ icon opens available up/down actions within the visible section;
editing/changing section is available there and by swipe. Local per-list/account
`<storage key>.itemOrder` GUID preferences preserve placement through filter/sync
and new items append. Hidden items and other sections retain their positions.
Copy/share follows item order. No new Firebase fields or backend changes.

ShoppingCategories supplies local whole-word and phrase rules for new uncategorized
items after quantity extraction. Explicit non-Other/custom categories are retained;
saved lists are not migrated. Recipe shopping normalizes source categories before
merging so provenance/cupboard checks remain associated. Household list ordering is
personal to each device, consistent with section ordering. Social remains paused.

## Latest update: drag sections and recognize shopping amounts

Long-press section headers on the shopping screen, or rows in Reorder sections,
to drag above/below another section. The reorder screen keeps optional buttons
under its menu for accessibility. Only drags started in the current list are
accepted; section preferences remain per-list/account on this device. Sync waits
while a section is being dragged. Native gesture interaction needs phone testing.

ShoppingItemInput extracts a recognized leading amount (`500 g beef` → `beef`,
`500 g`). Nonempty explicit quantity always wins. Quick-add, widget additions,
paste, editing, reusable items and recipe shopping use this rule. Recipe shopping
extracts before portion scaling so combined quantities remain correct, without
rewriting the original recipes. Existing notes/checkmarks remain intact. Social
features remain paused; no backend changes are required.

## Latest shopping change: reorderable sections

Shopping has a Reorder sections toolbar/tools action with accessible up/down
controls, immediate persistence and restore defaults. Category order is a local
per-list/account preference (`<list storage key>.sectionOrder`); household members
can each choose their own store route. It is separate from item JSON and Firebase
sync, so it does not require backend changes or change shopping items/checkmarks.
Copy/share exports follow this order. Tests cover ordering, aliases, malformed
preferences, new/missing sections, boundaries and exports. Android build and
274 app tests passed; phone interaction verification remains.

## Current priority: social features paused

The user has paused all social/creator work. Android hides the entry points and
blocks their navigation, incoming content links and social notifications. The web
release also hides the community studio and creator links. Keep the retained code
disabled unless the user explicitly resumes it. Personal/household shopping and
meal planning remain available. See [SOCIAL_PAUSE.md](SOCIAL_PAUSE.md). No Firebase
backend or production deployment changes were made for this pause.

Continue from branch `codex/android-runtime-fixes` in `TheCalZone/Mealsy`.

## Latest frontend work: friends, privacy and shared meal plans

The Android branch now includes friends/search/requests, profiles, privacy-aware posting,
friend conversations and shared household meal planning. **The new backend services have
not been implemented or deployed by this change.** Follow [FIREBASE_HANDOFF.md](FIREBASE_HANDOFF.md)
for exact `social` and `householdMeals` contracts, legacy feed privacy migration,
authorization, media restrictions, concurrency and rollout requirements. Do not advertise
these features as live until those services and real-device integration checks pass.

## Latest content sharing work

Published creator plans and social posts can be shared as identifiers-only HTTPS
links. Recipients can save selected creator recipe copies and shop directly,
with repeated recipe portions combined. Android handles custom links and verified
HTTPS link intents; sign-in keeps the destination. `/share/` is the new mobile
landing page; `/` remains the desktop studio. `foodFeed.getPost` is added in source
for direct authenticated legacy post reads. **No changes have been deployed.**
See [CONTENT_SHARING.md](CONTENT_SHARING.md) and the updated Firebase handoff for
Hosting, App Signing certificate association and privacy requirements.

## Current state

The Android application and backend include business/creator workspaces, moderated recipe publishing, collections and weekly plans, team roles, analytics, campaign drafts and prepaid impression-accounting infrastructure. The latest fixes add full-screen shopping, public-name comments and replies, private activity notifications with opt-in Android push, four included household members with extra-place billing support, household batch portions, Android recipe-link sharing/import and navigation/keyboard fixes.

The backend is deployed to `mealsy-b732b`. Paid promotion activation was confirmed **disabled**. Do not enable `commercialConfig/campaigns.enabled` until the store products, payment permissions/refund notifications and device verification have passed end-to-end tests. See [business/social setup](BUSINESS_AND_SOCIAL_SETUP.md), [pricing estimate](FAMILY_COST_ESTIMATE.md), [diagnostics](DIAGNOSTICS.md) and [cupboard ingredients](CUPBOARD_INGREDIENTS.md).

## Checks

Use Node 22 for the backend. From the repository root:

```sh
npm ci --prefix firebase/functions
node --test firebase/functions/test/*.test.js
dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj
```

The repository pins .NET SDK 8.0.400 with feature-band roll-forward. Install a matching .NET 8 SDK for cloud tests. Native Android builds additionally need the MAUI Android workload, JDK 17 and Android SDK; see [README](README.md). iOS signing/building needs a compatible Mac/Xcode toolchain. A generic Linux cloud environment can run the platform-independent tests without building the native application.

Run the database integration suite only against a demo emulator, never production:

```sh
npx --prefix firebase/functions firebase emulators:exec --project demo-mealsy --only firestore "node --test firebase/emulator.test.js"
```

The normal emulator config uses port 8080. Choose a free port through an alternate Firebase config when necessary. Firebase CLI 14.22 is pinned in backend dev dependencies; its emulator requires a compatible Java runtime. The separate `firebase/live.test.js` suite performs read-only authentication checks against the deployed project; it does not test authenticated business flows or store purchases.

Pricing/Premium update validation: 201 .NET tests passed and the Android Debug build succeeded with zero warnings/errors. Earlier local handoff verification reported 103 backend tests, six Firestore emulator tests and seven live authentication checks passing, plus APK signature/alignment checks. Those backend/live checks were not rerun for this client-only update. APKs and signing material remain local artifacts and are not committed.

## Remaining external/platform work

- Configure and test `mealsy_extra_places` subscriptions at the intended £0.99/month per extra place, including tier changes through the store.
- Configure and test `mealsy_promotion_credit_5`, Publisher API permissions, purchase restoration, refund/voided notifications and Play Integrity App Check before enabling live promotions.
- Test opt-in phone-alert delivery on a real Android phone with separate accounts. The messaging APIs and deployed runtime permission have been configured.
- iOS store purchases, APNs push and an iOS share extension are not implemented. The shared-household backend is platform-neutral.
- Measure production traffic and billing before treating the Family cost estimate as a proven margin.

## Credentials

Never commit administrator service-account JSON, OAuth tokens, private signing keys, or signed build artifacts. Use environment-owned credentials/secrets for authorized deployments. The old `RecipeApp/credentials.json` and `RecipeApp/Cal.keystore` have been removed from tracking and remain local. They were present in earlier history: removing the files from the current branch does not revoke those credentials or rewrite history. Rotate the service-account key through the project administrator, and review the signing-key exposure with the app publisher before a production release.

## Pricing update

UK reference pricing is now £5.99/month or £49.99/year, with £0.99/month for each extra household place. The existing product and base-plan IDs are unchanged. These code/documentation changes do not update Play Console prices or existing subscribers. Store-provided local prices remain authoritative.

## Solo Premium tools

Public plan name is Mealsy Premium, including Family sharing. Product/base-plan IDs stay unchanged. Supported website imports now require paid access; manual recipes/paste and existing recipes stay free. New private device-local libraries provide reusable weekly templates and saved personal shopping trips (100 entries each per account). These use existing server-verified paid status and the short cached entitlement grace; no backend deployment is needed. Libraries remain retained/readable/deletable after expiry. Paid promotions remain disabled. Do not commit APKs or signing material.

## Invitations and onboarding update

`ONBOARDING_AND_INVITES.md` covers the new optional Get started guide, shared Android download/household-code landing page, native `mealsy://join` and `mealsy://welcome` links, and the complimentary-Premium admin shortcut. The admin UI uses the existing audited `setFamilyGrant` API; no live grant or server deployment was performed. Deploy Hosting to activate `/join/` and distribute an updated app. Google Play listing/test eligibility remains required; fresh installs may need code paste and native link handling needs phone verification. Validation: 213 .NET tests, 103 backend tests and 10 browser tests passed. No signed artifacts or signing material are committed.

## Account refresh/loading fix

See `ACCOUNT_LOADING_FIX.md`. Inline Account-tab sign-in now refreshes the owning
page directly instead of relying on navigating to the same Shell route to trigger
OnAppearing. Native progress indicators cover authentication, account hydration
and main loading screens; slower shared actions show a working caption. Account
load generations block obsolete responses after account/page changes. No backend
deployment is needed. This change is on the Android branch; iOS can merge the
shared fix separately. Automated checks now include 217 shared app tests.


## 20 October 2026 — Compact Android shopping widget

The Add icon now launches `WidgetQuickAddActivity`, a non-exported native dialog over the launcher, rather than the MAUI app navigation. Standard Android RemoteViews widgets cannot embed EditText. Choose-list and open-list icons intentionally open Mealsy. Widget sizing is now 3×2 cells (launcher dependent), with 48dp icon targets. Existing widget instances may need resizing or removing and adding again.

The dialog initialises Firebase independently for cold-process entry, checks the selected account and household entitlement, saves personal entries offline, and attempts the existing shopping sync. Failed sync leaves a local copy for retry from the list. Widget writes refresh an already-visible shopping page while retaining any item-edit fields. No Firebase backend/schema changes. Native widget wording is currently English.

Device checks: add from launcher with Mealsy closed, keyboard/IME Done, cancel/back, rotate while entering, offline personal save, household permission failure, signed-out/account-switched widget, multiple independently configured widgets, and return to an already-open shopping list. Verify sizing and focus on the target phone launcher.


## 20 October 2026 — Typed meals to weekly shopping

Personal weekly planner passes typed meal names (only slots with no recipe IDs, including legacy IDs) into the shopping creator. Review is no longer blocked for recipe-free weeks. The cupboard stage shows matching saved essentials as unticked suggestions, source meal names and occurrence counts. A selected essential uses its usual purchase once per shop; no servings/quantities or implicit ingredients are inferred. Existing checklist selection and final quantity review apply. Unmatched names can prefill a new essential editor; saving does not select it automatically. Whole-phrase, case-insensitive matching handles simple plurals and avoids substring matches such as eggs/eggplant. Recipe-free notes are not scanned. No backend changes. New copy is English fallback. Check a typed-only week, mixed recipe/typed week, repeat eggs across days, existing checklist selections, editing/adding essentials, account switching and final shopping quantities on device.


## 20 October 2026 — Homepage recipe card actions

Shared recipe cards are now tappable, retaining View & cook. Homepage cards also expose an Options button and native Android long-press action sheet (View, Edit, Shop ingredients, destructive Delete). Homepage actions reload the recipe and check the captured account/ownership before acting. Delete confirms, performs cloud deletion before local removal, then refreshes the dashboard. Native long-press detection preserves normal scrolling and cancels the pending tap on release. No backend changes. iOS retains tap and visible Options; native long press is Android-specific. Device checks still needed for scrolling, long-press/release, TalkBack Options access, edit/back refresh, account switching and delete/cancel/cloud failure. Existing shopping/planner behaviour is unchanged.


## 20 October 2026 — Local leftover recipe finder replaces AI

Removed ChatPage, ChatViewModel, OpenAIApiService, their navigation route and the OpenAI NuGet dependency. Home and the recipe book now open LeftoverRecipesPage. Plan copy and README describe the local free feature instead of AI billing. Existing recipes are preserved. Historical API keys should still be revoked by their owner if previously exposed; no new key is requested or used.

LeftoverRecipes searches only saved ingredient names for the active account (or device recipes signed out). It handles casual filler words, simple plurals, common minced/ground meat wording, quantities, and up to 10 comma/and/newline-separated ingredients within 1,000 characters. Results rank matched ingredient count and show matching recipe ingredient names plus any requested ingredients absent from that recipe. No external requests, substitutions, quantities or recipes are generated. Open/cook and shopping review use the existing screens and account checks. New text is English fallback; matching aliases are initially English. No Firebase backend changes. Phone checks: natural wording, multi-ingredient ranking, empty library, account switch, recipe open/back/edit/delete refresh, shopping review, keyboard and large text.


## 20 October 2026 — Left-aligned shopping amounts and direct handle dragging

Shopping item rows now have checkbox, a single left-aligned name/quantity/note stack, and the right-hand handle. Android handle touches use native down/move/up events rather than MAUI's long-press drag activation. Movement past the normal touch slop starts selection; visible target rows highlight and shift slightly to indicate before/after insertion. Release commits through existing ShoppingItemOrder/DropItem persistence. Cancellation or an invalid target restores visuals without changing order. The handle alone captures scrolling; swiping/tapping the item and checkboxes keep their existing behaviour. Moving near the RecyclerView edge scrolls it. Account, selected-list, editing and sync checks remain enforced; moves remain within the section and device-local order. Other platforms retain their existing MAUI drag recognisers.

Phone checks still required: drag immediately from handle, reorder in both directions, move near edges, cancel/outside drop, filtered lists/collected visibility, tall wrapped rows, switch accounts/lists, normal scrolling outside handle, swipe edit/delete and checkbox taps. Android touch/motion cannot be verified by the service unit suite. No backend changes.


## 20 October 2026 — Animated item and section drag previews

Android shopping item handles, section headers and cards on Reorder sections now share ShoppingDirectDrag/ShoppingDragPreview. A temporary bitmap snapshot follows the finger above the UI; neighbours animate aside over 160ms and a full-size highlighted Drop here gap indicates placement. Pickup/destination haptics are included. Section headers preview the header position; the dedicated Reorder sections screen provides compact section cards. The underlying items/section order changes only on release, through existing persistence methods. Preview geometry is shared/tested against actual item ordering for up/down moves, before/after insertion, unequal row heights and no-op boundaries. Account/list changes, lost handles, cancellation and page departure restore native alpha/translations and remove/dispose the overlay bitmap. Other platforms retain existing gesture behaviour. Native wording remains English fallback. No Firebase changes.

Physical Android checks remain: floating preview alignment across status/navigation bars, scrolling/recycled rows, edge dragging, variable row heights, drop gaps on dark/light themes, cancellation and backgrounding, item moves within sections, section moves via both headers and the section-order page, alternative buttons and reduced-animation system settings.


## 20 October 2026 — Handle-only section dragging

Shopping section headers now separate the item count from a dedicated 48dp move icon. ShoppingSectionDrag registers the whole row as a preview/drop target but attaches gesture initiation only to the icon, on Android and the existing other-platform fallback. The same separation applies to compact section cards in Reorder sections; optional up/down buttons retain their own touch targets. Item dragging was already handle-only. Check on phone that swiping section names/counts and card backgrounds scrolls without starting a drag, icons still show animated previews, and optional section buttons work. No backend changes.


## 20 October 2026 — Continuous drag edge scrolling

Android ShoppingDirectDrag now runs a 32ms UI timer while dragging. Holding the pointer in a 64dp top/bottom edge zone scrolls continuously at 45–180dp/sec, with fractional-pixel accumulation and elapsed-time clamping. Moving to the middle/outside the viewport, reaching a scroll boundary, release, cancellation, focus loss or account/list changes stops scrolling. The shared implementation covers item handles, main section handles and Reorder sections cards.

After pickup, an Android WindowCallbackWrapper captures the drag's pointer sequence so the original cell can leave the recycled viewport without losing the drag. The original view receives a cancellation; the window callback is restored when the drag ends. The preview reconstructs the offscreen source's logical slot from current model order and restores recycled cell alpha/translations so unrelated rows do not inherit preview effects. Existing section restrictions, ordering persistence and icon-only initiation remain. New tests cover speed/direction/stop boundaries, short viewports and offscreen-source positioning.

Phone verification still required: hold stationary at top/bottom edge for several seconds, drag past the original cell offscreen in both directions, release on newly revealed rows, reach each end, move back to middle, cancel/background, switch accounts/lists, and confirm ordinary scrolling and touch input work after every drag. No backend changes.


## 20 October 2026 — Compact bundles while dragging sections

Main shopping-list section dragging now temporarily folds ingredient rows away so stationary ingredients do not obstruct the animated header cards and drop gap. Native drag lifecycle callbacks toggle ShoppingListViewModel.ShowSectionIngredients/IngredientRowHeight; SwipeView rows become invisible with zero requested height and groups rebuild for measurement. Group contents/counts and the underlying document stay intact. The window-level pointer capture introduced for edge scrolling keeps the gesture alive as rows and headers re-layout. Cancel/drop/focus loss/list changes restore normal ingredient rows; a completed drop then persists the section order through the existing method. Ingredient-only drags and the compact Reorder sections page do not trigger this presentation mode. Recycled/disposed native rows are ignored safely. No backend changes.

Phone checks: pick up a section icon and confirm ingredients fold away, drag through several compact headers and edge-scroll, drop and verify ingredients expand under their original heading in its new position, cancel/background and confirm restoration. Repeat with filtered lists, Hide collected, long sections, checked items, a household list and an ingredient-only drag. Native layout/touch behaviour still needs device verification.


## 20 October 2026 — Android Save recipe to Mealsy share target

A dedicated exported ShareRecipeActivity now owns ACTION_SEND text/plain and text/html filters with the label Save recipe to Mealsy. MainActivity no longer advertises a duplicate generic share target. The no-display receiver accepts shared text/HTML or a text/URI ClipData fallback, extracts a bounded HTTPS URL without fetching it, forwards only that link to MainActivity, then finishes. SharedRecipeLink prefers existing supported recipe hosts among multiple URLs and handles title wrappers, trailing punctuation and HTML entities. Unsupported HTTPS websites can still open the importer and receive its existing manual-entry guidance; host/redirect checks remain in WebsiteRecipe.

MainActivity waits for initial account/Shell readiness, queues up to five incoming shares instead of dropping a second share during navigation, and catches failures rather than letting an async-void exception crash the app. Shared community links still respect the social pause. RecipeWebsitePage explains the browser workflow. Premium checks, draft protection, optional photos and explicit Import & review are unchanged. No automatic network request/save occurs on sharing; no Firebase backend changes. iOS sharing extensions are not included.

Phone checks: browser Share menu target label/icon, cold/warm launch, Chrome/Firefox/Samsung Internet where available, title plus URL, formatted share, two quick shares, unsupported website feedback, non-Premium account, existing recipe draft, optional photo and back to browser. Native share chooser/navigation still need device testing. Web recipe discovery remains excluded.


## 21 October 2026 — Selective household preload and low-cost read reuse

App startup warms household status, the household shopping list and only the current Monday-start week before running the existing AccountSession.SyncSafely catalog/profile sync. Resume/Home/sign-in can trigger the same warmup, coalesced per account with a one-minute retry/attempt cooldown. No startup history queries, separate photo downloads, list patches or membership writes are added. This preserves existing full account sync behaviour; it does not eliminate its existing reads. A callable request can perform several Firestore reads, so three startup requests do not mean three billable document reads.

CloudReadCache caches successful household status, personal/household list reads, meal-week reads and household-meals capability results in memory for 60 seconds. Keys include the account, scope, household and week. It coalesces concurrent requests, clones serialized results, checks invalidation before caching AND returning, and rejects older in-flight results after writes. Patch acknowledgements prime the list cache. Week edits invalidate that week; membership mutations clear read caches/snapshots. Permission/precondition/auth failures clear cached household visibility. No mutation or billing/admin/social result is cached. Explicit list Refresh/conflict reload and meal Refresh/latest-version actions bypass read reuse. Widget local additions retain a forced fresh status check before their backend-authorised patch.

Warmup stores per-account last-view snapshots (one household list and the current meal week) in Preferences. Rendering requires a still-fresh, matching household status with active Family access; snapshots older than a day are excluded and prior-week meals are not shown. This is a saved view, not a grant of membership or Premium. Background loading never writes to ShoppingList.v2/household caches or advances .synced: pending local edits are overlaid for the Home count, with conflicts retaining the device view. Snapshot writes are serialized; account and membership revision checks prevent older sessions from repopulating a changed household. Local sign-out clears the memory read cache; persisted data remains isolated by account and inaccessible to the saved-view UI without renewed membership.

Home's household card shows today's meal names, remaining shared shopping count, last-checked timestamps, direct list/plan links and explicit Refresh. The shared-week page can render a saved current week while refreshing. Shopping still checks for pending local edits every five seconds while visible, but clean network reads are reused for 60 seconds; pending patches are not delayed by read caching. Polling is disabled while App.IsForeground is false. Failed shopping sync attempts back off for one minute unless the user manually refreshes. Other household members' changes can therefore take about a minute to appear in an open list; Refresh fetches immediately.

No Firebase deployment or schema change is needed. Validate on two phones: cold/warm launch and resume, cached Home view after status verification, dirty/offline list edits during preload, current-week boundary, manual refresh, sign-out/account switching, household leave/removal/expiry, failed network then retry, meal edit/delete/latest version and widget add. Unit tests cover coalescing, expiry/checked timestamps, force refresh, account/scope/week keys, mutation races, failure recovery, result cloning and JSON patch-ack reuse. Actual native/UI/Firebase billing behaviour still needs device/project verification.


## iOS synchronisation — 21 October 2026

`codex/ios-client-support` merges Android source through `38bb44c`, including
shared shopping scroll fixes and household-first caching. See IOS_SETUP.md for
feature parity limits and the exact Mac/TestFlight hookup checklist. The new
`scripts/build-ios.sh` runs configuration/readiness checks before a simulator or
signed archive build; it refuses the current legacy toolchain. No native iOS
build, deployment, StoreKit, ads, APNs, WidgetKit or share extension is claimed.
Native MAUI section dragging now uses the compact-preview lifecycle and checks
the account/list context. Android custom preview/autoscroll stays Android-only.


## iOS WidgetKit and Share source — 21 October 2026

The iOS branch now contains `native/ios` Swift targets, XcodeGen configuration,
MAUI AdditionalAppExtensions embedding, opt-in `IOS_EXTENSIONS` capabilities,
App Group link inbox review, and widget deep-link routing to existing quick add.
The Mac build helper builds/bundles both targets by default after its toolchain
checks pass. See native/ios/README.md for exact Apple App IDs/group/profiles.
No Firebase credentials/data are exported to extensions; sharing makes no recipe
request until explicit import. Widget opens the main app; it does not accept
inline text. No backend deployment required. Native build/signing/phone tests
and the existing supported-toolchain migration remain mandatory.
