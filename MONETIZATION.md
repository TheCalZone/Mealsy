# Advertising and Mealsy Family

## Product decision

**Free:** personal recipes, shopping lists, meal planning, essentials/snacks, AI
recipe generation with the user's own OpenAI key, and the signed-in food community.
Banner advertisements can appear on Home and Food feed after the advertising
privacy/age flow permits them. No interstitial or rewarded ads are included.

**Mealsy Family:** **£24 per year in the UK**, automatically renewing. One subscription
held by the household owner unlocks household creation, invitations and shared
shopping-list reading/editing, and removes ads for the owner and all current household
members (up to 20 total). Invitees do not each need to buy a plan. Google Play displays
localized prices and the actual renewal terms before confirmation. There is no trial.

A member's separate personal subscription removes ads for that member but does not
pay for another owner's household. To become the paying household owner they can
leave that household and create their own. Shared meal plans/recipe books are not
implemented; the paid family feature is the existing shared shopping-list system.

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
   plan ID **`annual`**, billing period **one year**. Set the UK price to **GBP 24.00**,
   configure other regions/local prices/taxes, activate the base plan, and do not
   configure an introductory trial for this integration. The app selects the normal
   annual base-plan offer and shows Play's formatted price, rather than hardcoding
   an amount into the purchase request.
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
its own service identity. It validates the package, product, annual base plan,
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
