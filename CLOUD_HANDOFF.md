# Mealsy cloud handoff — 17 October 2026

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
