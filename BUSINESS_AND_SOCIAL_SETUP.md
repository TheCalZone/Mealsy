# Business and social update

## Included

- Account → Mealsy Business & creators: independent creators or organisations, public profile/logo/website, team invitations and separate editor/campaign-manager roles. An administrator verifies the business identity; publication still requires a confirmed email and automatic safety approval.
- Public recipe collections, optional commercial offer links, and editable weekly meal-plan imports into each user's personal plan. Saving a business recipe creates a private copy with attribution to its public post, without modifying the creator's original.
- Batch import previews up to 20 JSON recipes or a saved private recipe. Publication is sequential, moderated, and stops further submissions when the page is left. Stable content IDs prevent identical retries from creating duplicate posts. The initial workspace supports 100 published copies, 50 collections and 50 campaigns per business.
- Aggregate 14-day analytics, excluding the team's own activity and deduplicating by signed-in account/target/day. These figures are estimates and are independent of the monetary impression ledger.
- Campaign drafts, placement previews, no-end-date option, daily/total limits, pause/resume and prepaid credit infrastructure. The approved rate is £1 per 1,000 qualifying impressions, or 0.1p each. Money is accounted in integer micro-pounds.
- Comments use the server-owned public username, support replies, and create private activity notifications for post/reply authors. Following a cook creates an activity notification. Deleted/hidden posts remain inaccessible through notifications.
- Feed → Activity → Phone alerts enables Android push. Alerts contain generic text, not comment contents or names. Sign-out clears the device's recipient binding locally, even if deregistration cannot reach the network. Enable alerts separately after signing into another account. Delivery is best effort; the Activity inbox remains available if a device is offline or notifications are disabled.
- Go shopping hides the shell navigation, tabs, header and extra controls. Exit shopping or Back restores them; Tools reveals quick add/search. Widget opening now explicitly returns to the shopping root rather than retaining an ingredient-entry screen, and existing widgets refresh when the app opens.
- Weekly meal planner → Plan a household batch spreads recipe portions over consecutive empty meal slots, defaults the people count from household membership, marks leftovers and totals one batch in shopping. It never replaces an existing meal or note. It plans quantities; it does not determine safe storage durations.
- Android's Share menu can send recipe links into Mealsy for review. Home → Import recipe link also accepts links. Supported structured-recipe sources: BBC Food, Good Food, Jamie Oliver, Allrecipes, Tesco, Delicious and RecipeTin Eats. Each redirect must remain HTTPS on a supported host. Pages are bounded, parsed on the device, and saved privately only after review. Paywalls, blocked pages, incomplete markup and other websites need manual entry. An existing new-recipe draft must be finished or discarded first.

## Store setup still required

The signed APK supports the Android billing flows, but paid purchases must be tested through a store-installed test release, not assumed to work in a sideloaded APK. Existing Family products remain `mealsy_family`, base plans `monthly` (£3 UK) and `annual` (£24 UK).

Create subscription product **`mealsy_extra_places`**, with monthly renewing base plans **`places-1` … `places-16`**. Each tier grants that many extra places, priced at **£0.50 × tier/month** in the UK. Store-confirmed local pricing is authoritative. The backend verifies the product, tier, account binding, state and expiry; arbitrary client fields cannot grant places. Changes to an existing extra-place subscription use the store's management flow; automatic tier replacement is not implemented. Expiry blocks new joins above the allowance but does not remove existing members or their data. Extra places do not independently grant Family access.

Create one-time consumable product **`mealsy_promotion_credit_5`**, a **£5 promotion-credit balance** with the intended UK price £5. Purchases are bound to both the purchaser's account and business using obfuscated identifiers. The server independently checks the store receipt, credits exactly once, and consumes it. Restore unfinished purchases from the business wallet. Refunded/voided receipts remove their credit, potentially leaving a negative balance that stops spending; refund money is handled through the store. Ensure the existing billing Pub/Sub configuration includes one-time and voided purchase notifications, not only subscription changes.

The backend runtime service account needs the appropriate Android Publisher permissions for this app. The existing subscription verification uses the same store API. No store product was created, priced or activated remotely by this change.

## Activate promotions after a real store test

Promotions remain **off by default**: the server-owned document `commercialConfig/campaigns` must contain `{enabled: true}` to allow credit verification/checkout and live campaign activation. Do not enable it until products, store API permissions, refund notifications and device verification have passed end-to-end testing. Client administrators cannot fabricate wallet credits or store receipts.

The Android client uses the Play Integrity App Check provider and sends its token for impression preparation and confirmation. Register the production signing SHA-256 certificate and Android app in App Check, configure Play Integrity, and test the release distributed through the store. A sideloaded test build may not satisfy the chosen integrity settings. No debug bypass is included in the impression billing endpoint. See [official setup](https://firebase.google.com/docs/app-check/android/play-integrity-provider).

The client requires at least 50% of the post visible for one continuous second in a focused foreground window. The server requires a verified app token, a short-lived account-bound ticket and elapsed time, and excludes team members. Billing is limited to one impression per viewer/campaign/UTC day and enforced atomically against the wallet, daily limit, total limit, current post visibility and campaign state. Attestation and these limits reduce abuse; they do not independently prove a human viewed the ad. Monitor invalid traffic before scaling. Paid placement is ranked within the retrieved chronological feed page; this first implementation is not a global ad-auction or full search index.

## Retention and platforms

TTL settings in `firebase/firestore.indexes.json` expire engagement deduplication and impression deduplication records after 90 days, invitation records after 48 hours, impression tickets after one day and activity inbox entries after 90 days. TTL deletion is asynchronous and billed. Aggregate metrics, campaign spend and purchase records are retained. Existing invitation/visibility permissions still check expiry immediately, independently of TTL.

The household/social/business backend uses authenticated, platform-neutral endpoints. An iPhone user running a configured iOS build can use an Android owner's shared household; every member does not need to purchase a subscription. iOS store purchases, APNs push delivery and an iOS share extension are **not implemented by this update**. iOS signing/building still requires a compatible Mac/Xcode toolchain and provisioning. Windows testing does not establish an iOS build or device result.

The new Android phone-alert delivery must be checked on a real phone with two signed-in accounts. Emulator and unit tests establish server permissions and accounting but cannot prove push delivery, store payments or device integrity.
