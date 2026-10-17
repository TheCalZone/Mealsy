# Advertising and Mealsy Premium

## Product decision

**Free:** personal recipes, shopping lists, meal planning, essentials/snacks, AI
recipe generation with the user's own OpenAI key, and the signed-in food community.
Banner advertisements can appear on Home and Food feed after the advertising
privacy/age flow permits them. No interstitial or rewarded ads are included.

**Mealsy Premium (including Family):** **£5.99 per month or £49.99 per year in the UK**, automatically renewing. One subscription
unlocks supported recipe-link imports, reusable weekly templates and saved personal shopping trips for solo cooks as well as households. It also unlocks household creation, invitations and shared
shopping-list reading/editing, and removes ads for the subscriber and covered household members. Four people are included; up to 16 extra places are sold separately at £0.99/person/month UK reference pricing. Invitees do not each need to buy a plan. Google Play displays
localized prices and the actual renewal terms before confirmation. There is no trial.

A member's separate personal subscription unlocks personal Premium tools and removes ads for that member but does not
pay for another owner's household. To become the paying household owner they can
leave that household and create their own. Shared meal plans/recipe books are not
implemented; Family sharing is the shared shopping-list system. Templates and saved trips remain private, per account and device. Importing a link is paid; manual entry, ingredient/method paste and editing previously imported recipes remain free.

Expired plans pause sharing and invitations but **do not delete household data**.
Members can still leave, and owners can remove members, revoke invitations or delete
the household without paying. Personal shopping lists and the community stay free.
Cancellation stops renewal but keeps paid access until expiry; Google Play grace
periods retain access. Payment pending, on hold, paused, expired or revoked does not
unlock the plan. The app includes restore and Google Play manage/cancel links.

## What is and isn't configured

Code includes Google Play Billing **8.0.0**, Google Mobile Ads **24.5.0**, UMP consent,
Firebase receipt verification, entitlement checks, subscription notifications and
hourly reconciliation. Android now requires **Android 6/API 23 or later** for these
SDKs. AndroidX dependencies were aligned with those SDKs. The unused Firebase umbrella
package was replaced with the existing Core/Auth/Firestore/Crashlytics modules,
avoiding duplicate legacy Analytics/modern AdMob measurement classes. Unused broad file/media and
secure-settings permissions were removed; legacy gallery read access is limited to
API 32 and earlier.

**Live purchases and ad revenue are not activated by pulling this branch.** No Play
Console subscription, AdMob account, payment settings, privacy policy, live ad unit,
Firebase billing deployment or Android Publisher permissions were created remotely.
The session has no access to those accounts. Debug uses Google's sample banner IDs;
Release live ads are disabled until publisher configuration is supplied. Unknown
consent or unknown signed-in plan status prevents ad requests. Real billing requires
an installation from a Google Play testing/release track and the matching account.

**Store-release prerequisite:** this repository still targets .NET MAUI 8 / Android
API 34. Before submitting a new public release, migrate to a supported MAUI/Android
toolchain and the target API currently required by Google Play, and validate the
store's current SDK/billing policies. Compiling this development build is not proof
of current Play submission eligibility.

## 1. Google Play subscription

1. Register/confirm the Play app package **`com.companyname.recipeapp`**. If your
   real published package differs, update the MAUI application ID, Android manifest,
   Firebase mobile configuration, `firebase/functions/billing.js` package constant
   and the plan's manage-subscription link before building/deploying.
2. Create a subscription with product ID **`mealsy_family`** and auto-renewing base
   plans **`annual`** (one year, UK **GBP 24.00**) and **`monthly`** (one month,
   UK **GBP 3.00**). Configure other regions/local prices/taxes and activate both
   base plans. Do not configure an introductory trial for this integration. The app
   selects the chosen normal base-plan offer and shows Play's formatted price,
   rather than hardcoding an amount into the purchase request. Both have the same
   Family benefits. Existing subscribers manage their subscription in Google Play;
   the app does not initiate a second simultaneous subscription.
3. After the target/toolchain release update, use Visual Studio Archive/Publish to
   produce a correctly signed Android App Bundle (`.aab`) for your Play app (the
   repository's current development Release configuration produces an APK). Upload
   the signed bundle to an internal test track. Configure license
   testers and opt-in testers. Purchase from the test-track installation, using the
   Firebase account that will own the household. Play controls the actual charge;
   Firebase does not set the price. Store commissions/taxes affect your proceeds.
4. Enable the **Google Play Android Developer API** in the linked Google Cloud
   project. Grant the runtime service account used by your Firebase Functions
   appropriate Play Console app permissions to view subscriptions and manage orders
   (including purchase acknowledgement). Check the deployed function's service
   account in Cloud Console; do not assume it matches your desktop login.
5. Deploy reviewed rules and the complete function codebase from the repository root:

   ```sh
   npm ci --prefix firebase/functions
   npm test --prefix firebase/functions
   npx --prefix firebase/functions firebase login
   npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only "functions:households,firestore:rules"
   ```

   This includes `household`, `foodFeed`, `billing`, `playSubscriptionUpdates` and
   `reconcileSubscriptions`. Existing household creation/sharing becomes gated after
   deployment; existing households are preserved and their owners need a verified
   subscription. Coordinate the Android rollout with this backend change.
6. Configure Play Console **real-time developer notifications** to the Pub/Sub topic
   **`projects/mealsy-b732b/topics/mealsy-play-billing`**. Grant
   **`google-play-developer-notifications@system.gserviceaccount.com`** permission to
   publish to that topic. The deployed function subscribes to it; test notification
   delivery from Play Console. Configure the topic only for the intended Play app.
   Blaze billing / enabled Pub/Sub and Scheduler services are required.
7. Test restore, payment pending, renewal, cancellation, refund/revocation and owner
   expiry with license-test accounts. Confirm an invited member is ad-free without
   making a second purchase. Confirm free users cannot call the household API to
   bypass the plan screen.

## Verification and entitlement storage

The app supplies the purchase token to the authenticated `billing` callable. The
server fetches the purchase from the Android Publisher subscriptions v2 API using
its own service identity. It validates the package, product, supported annual/monthly base plan,
subscription state, expiry and the SHA-256 obfuscated Firebase account identifier.
The app sets that identifier in Google Play's billing flow. A token cannot be rebound
to another Mealsy account. Active unacknowledged subscriptions are acknowledged by
the backend; there is no client-only premium toggle.

`billingPurchases/{sha256(token)}` holds the private token/binding and verified state;
`billingEntitlements/{uid}` holds the currently selected entitlement. Both are denied
to all client Firestore reads/writes by the existing default-deny rules. Family
operations read the owner's entitlement within their Firestore transaction. Profile
fields and device preferences do not grant server access.

Real-time notifications refetch state from Google rather than trusting event-provided
expiry. Hourly reconciliation provides a fallback. Refund/hold changes can therefore
lag until a notification, a status/restore check, or reconciliation succeeds; expiry
is checked on every shared request. Monitoring failed notifications/reconciliation
is part of operating the service. Do not remove these deployed functions when
selectively deploying the backend.

Restore must use the same Firebase account used to subscribe. If the app was closed
or the network failed during checkout, Play keeps the purchase; restore submits it
again for verification/acknowledgement. Signing out does not transfer the subscription.
A short (one-hour, bounded by paid expiry) cache can suppress ads offline, but it
never authorizes a shared Firebase operation. Unknown signed-in billing status hides
ads until verification is possible, rather than risking ads for a paid customer.

## 2. AdMob and consent

1. Register the Android app in AdMob; configure your payment account and a banner ad
   unit. Publish the app's privacy policy and the required `app-ads.txt` file on the
   publisher website/domain listed in the store. Complete the store's ads/data-safety
   disclosures for the Google SDKs and your actual audience/data use.
2. Replace the sample AdMob **application ID** in
   `RecipeApp/Platforms/Android/AndroidManifest.xml` with your own app ID.
3. In `RecipeApp/Services/MonetizationConfig.cs`, supply `LiveBannerId`, your actual
   `PrivacyPolicyUrl`, and set `LiveAdsConfigured` to true only after setup/testing.
   Release checks reject the sample app ID and an empty privacy/ad-unit configuration.
   Debug always uses Google's official test banner ID. Never click live test ads.
4. Configure AdMob **Privacy & messaging** for the regions/audience you serve and test
   UMP consent acceptance, rejection and privacy-options forms on actual devices.
   The first banner requests an age group (under 13, 13–15, 16+); age groups are kept
   on the device, not published. Under-age and child-directed flags are set as
   appropriate. Requests use non-personalized ads (`npa=1`) and are made only after
   UMP permits requests; declining required consent can mean no ad is displayed.
5. **Account → Mealsy Family → Ad privacy choices** allows age-group changes and
   consent review. Banners are destroyed on privacy changes, paid upgrade and screen
   departure. No ads are requested on shopping/editing/checkout screens. Paid plan
   suppression is rechecked when a banner's page is reopened.

There is no production ad unit or publisher policy text bundled in this source.
Replace the configuration with your real details; do not publish invented policies.

## Checks

- `dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj`
- `npm test --prefix firebase/functions` includes fake-Play verification/state,
  acknowledgement, replay prevention, owner-paid member coverage, expiry and free
  personal-list access tests. It does not charge Google Play accounts.
- Auth/Functions protocol checks use local emulators (including unsigned billing
  rejection). Real Firestore/rules integration remains blocked in the cloud by the
  emulator JAR download policy; see `firebase/FEED.md` for local commands.
- Billing purchase flows, AdMob/UMP forms, restore after an app restart, and plan/ad
  changes on two devices require internal-track/device testing. No real purchases
  or live ad impressions were generated in the cloud.

The complete current feature inventory is in [FEATURES.md](FEATURES.md).

## Monthly option and upgrade features

The upgrade screen now offers £5.99/month and £49.99/year UK options. The annual option
is £4.17/month equivalent billed annually; twelve monthly payments cost £71.88 at those
UK prices. Google Play always supplies the actual local price before purchase.
A separate **What's included** page lists every Family benefit and distinguishes
free personal tools, optional OpenAI costs and personal/local meal planning.

For the monthly option, activate `monthly` in Play Console, then deploy the whole
function codebase so billing, RTDN and scheduled reconciliation all support it:

```sh
npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only functions:households
```

No Play product creation, live Firebase deployment or real purchase was performed
in the cloud workspace. The existing target-SDK/production publishing requirements
above still apply. Test both base plans, cancellation, restore and invited-member
ad removal with Google Play license-test accounts on the internal track.

## Premium tools and updated pricing

The public paid plan is named **Mealsy Premium**, for solo cooks and households. Existing Google Play product `mealsy_family` and base-plan IDs `monthly`/`annual` are retained; update the store display name and prices rather than creating duplicate products. Selected UK prices are £5.99/month, £49.99/year, and £0.99/month per extra member. Annual saves £21.89 against twelve monthly payments (about 30.5%).

In Play Console, update the actual base-plan prices and all extra-place tiers. App reference prices do not change Google Play charges or existing subscriber prices. Follow Play's existing-subscriber notice/consent rules for any price migration. This change performs no live store update or Firebase deployment. Prices from the store remain authoritative at checkout.

Supported website imports are guarded when Import & review is pressed, including Android shared links. A declined upgrade keeps the link; saved recipes and drafts remain editable. Weekly templates are reached from Meals or Account; saved shopping trips from Shopping's menu or Account. Both allow up to 100 named entries per account on the device, stored atomically with separate hashed account paths. Saving and reusing require a server-verified active plan; all covered household members get the same Premium tools. When offline, the existing verified ad-free entitlement cache permits a grace of up to one hour, bounded by plan expiry. No client purchase flag grants access. After expiry the saved libraries are retained and can be read/deleted; saving/reusing requires renewed access. They are not cloud backups or shared household libraries.

Shopping snapshots are saved explicitly, rather than silently recording every purchase. Reuse keeps item quantities/notes, resets collected state, creates new item IDs and merges into the personal list without replacing existing items. Weekly templates replace the current week's meals only after confirmation, preserve its week date, and reject missing recipe references. Neither feature deletes existing recipes. Basic manual recipes, paste, lists, cooking view and weekly planning remain free.

Phone checks: verify free/paid/expired accounts, an invited paid-household member, account switching while forms are open, offline expiry, template confirmation/cancel and saved-trip quantities. On a Play-installed test build, check the new localized prices, annual renewal period, extra-place tier totals and restore using existing product IDs.
