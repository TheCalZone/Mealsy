# Following, discovery and Family testing

The backend update was deployed to `mealsy-b732b` on 17 October 2026. Validation
passed 77 backend tests, eight desktop browser tests and four real Firestore emulator integration tests. The
Android Debug APK built with no warnings/errors and its v1/v2/v3 signatures and
ZIP alignment were verified against the same dedicated test certificate as the
previous phone-test APK. Native screen interactions still need phone testing.

Android now offers **For you**, **Following**, and **Latest** in the food community.
The first visit asks for food interests; saving an empty selection skips interests.
The toolbar's **Food interests & following** screen edits these choices and removes
follows, including follows whose original posts have since been removed.

Follow a cook from any visible post. Follows use the verified author's UID, not a
display-name search, so identical display names do not merge accounts. Each account
can follow up to 200 cooks. A follower sees public posts only; following never
grants access to private recipes, profiles, email addresses or household lists.

For you ranks each chronological batch using chosen interests, followed authors,
and topics from liked posts. Likes are idempotent, and unliking reduces that topic
signal. The topic catalog is inferred from public captions, recipe titles,
descriptions and ingredient names. These are recommendation signals, not verified
dietary or allergy labels. Device/profile dietary restrictions are not read.

Search matches any entered word against public captions, cook display names, recipe
titles, descriptions, ingredients and inferred topics (for example Italian can
match pasta). It supports accents. It is keyword/topic matching, not semantic or
typo-tolerant full-text search. **Load more** continues through older public posts,
including after a batch with no matches. Search always covers the whole public community, regardless of the selected feed.

Reads are bounded to 30 candidate posts per discovery/search request (15 for plain
Latest). Ranking applies within each batch; it is not a global recommendation
index. Cursors advance over hidden and filtered records to prevent repeated pages.
Large communities should move search to a dedicated search index rather than
increasing this scan limit. Existing desktop clients continue to receive Latest;
the new discovery controls are implemented in Android.

Private `feedAccounts/{uid}` documents hold interests, onboarding state, followed
cooks and bounded topic scores. Direct client access remains denied by the existing
rules. All changes go through authenticated `foodFeed` actions. Client-supplied
account IDs or authors cannot change who owns these settings.

## Family test access

Normal Family subscriptions still use Google Play receipt verification. For testing
with a sideloaded APK, a trusted project administrator can grant temporary access:

```sh
node firebase/functions/grant-family.js mealsy-b732b ACCOUNT_EMAIL 30 --firebase-cli
```

This uses the administrator's existing Firebase CLI login (or omit the flag to use
Admin Application Default Credentials). It verifies that the email exists in Auth,
then writes an expiring `familyGrants/{uid}` document. No password or purchase token
is needed; no purchase is created or charged. Grants last 1–365 days and are not
stored in the client-writable profile or subscription-receipt documents.

The owner grant covers their household members. Both billing status and every
household operation check the grant's expiry. Revoke it with:

```sh
node firebase/functions/grant-family.js mealsy-b732b ACCOUNT_EMAIL revoke --firebase-cli
```

After granting, reopen **Account → Family** to refresh plan status. Then open
**Account → My household → Create a household → Invite a family member**. Each
invitee signs in with their own account and joins using the single-use code.

Only a trusted administrator can grant access. Client writes to `familyGrants`
are denied, and subscriber receipt verification/reconciliation never manufactures
or overwrites an administrator grant. Expiry or revocation pauses household access
without deleting the household or its shopping list.

## Usernames, verification and automatic safety checks

Account now stores the chosen public username through the server-owned
`communityProfiles/{uid}` collection. Duplicate names receive distinct server
identifiers starting at 1001; only the chosen name is displayed publicly.
Post/comment authorship uses the authenticated account's saved name, never the
client-supplied name. Existing posts retain their original author snapshot.

Every new post (including recipe shares) requires Firebase's signed
`email_verified` claim. Account provides Send confirmation email and Check email
confirmation; the latter refreshes the ID token. Comments still require sign-in.

New usernames, posts, recipe snapshots, images and comments are screened before
publication using Vertex AI Gemini 2.5 Flash-Lite. Unsafe decisions block the
write. Errors, timeouts and malformed responses fail closed and retain the draft.
No manual prepublication approval is required. Automated moderation can make
mistakes and does not guarantee that every offensive submission is detected.
Previously published posts are not retrospectively screened by this deployment.

`setup-moderation.js` enables Vertex AI and grants the runtime service account
Vertex AI User; `check-moderation.js` checks benign/threatening synthetic examples
without publishing posts. Google processes submitted public content for these
checks, and model calls incur project usage charges. Private recipes are not sent
unless the user chooses to share their public snapshot. Existing successful-write
cooldowns and function instance limits apply; dedicated abuse quotas/App Check
should be considered before broad public rollout.

## Promoted posts infrastructure

Only visible approved recipes qualify. `requestPromotion` records an intent and
returns `checkoutEnabled:false`; it never charges or activates placement.
Activation requires a separate trusted `promotionAdmin` claim and a matching,
independently verified server-only payment receipt. Ordinary users cannot write
receipts, campaigns or activation flags. No promotion administrators were granted.

Active unexpired campaigns with valid non-revoked receipts are labelled Promoted.
At most one promoted candidate is moved to the top of each For you/search batch;
visibility and moderation still apply. Receipt revocation/expiry removes promotion
on the next load. This is bounded page ranking, not an advertising auction.
Payment checkout, store receipt ingestion, refunds integration, pricing and a
campaign management interface remain to be implemented before selling placement.
Never manufacture a verified receipt from a user's claimed payment.

## iOS and mixed-device households

The backend uses Firebase account identities and household membership, not device
platform. An invited iPhone member can use the same owner's Family entitlement and
shared list as Android members. Buying a new subscription on iOS is not implemented:
current purchase verification targets Google Play, while existing entitlements are
usable across household members.

The project includes an opt-in `EnableIosTarget=true` target, Firebase plist bundle
configuration, startup fix, photo permission text and iOS photo preparation.
These changes have NOT been compiled or tested on a Mac/iPhone. The default target
remains Android. A compatible Mac/Xcode environment, Apple signing/provisioning and
TestFlight/App Store distribution are required before an iPhone can install the
native app. The Android APK cannot be installed on iOS.

## Shopping item wire-format compatibility fix

The callable shopping backend accepts both camelCase item fields from
System.Net.Http.Json (`id`, `name`, `inBasket`, etc.) and the existing PascalCase
stored item format. All inputs are normalized before validation and three-way
merge; conflicting duplicate spellings are rejected. This fixes the Invalid
shopping item error without an APK update, including pending offline additions.
Regression tests cover Bread add/retry/edit/delete, invalid payloads, conflicting
edits, and a real emulator household member adding Bread for another member.

## In-app administration

The native Account page checks `administration/status` and shows Administrator
only when Firebase Auth's current server-side custom claims include `admin:true`.
The administration callable re-reads Auth on every request and rejects disabled
accounts and removed admin roles, regardless of stale client ID-token claims.
A client-writable profile field never grants administrator access.

The section supports paginated public post/report review, hiding/restoring approved
posts, resolving reports, pausing active campaigns, account lookup by exact email,
temporary Family grants/revocation (0–365 days), and paginated administrator history.
Restoring legacy posts without automatic safety approval is blocked. Automated
moderation remains the publishing gate; there is no manual approval queue.
Promotion pausing does not issue refunds. Family grant revocation does not cancel
paid entitlements. A grant benefits the account's household only when it is owner.
Private recipe/list contents are not returned by the administration endpoint.

Each mutation requires a reason and writes an `adminAudit` entry with actor,
target, timestamp and before/after flags in the same Firestore transaction as the
change. Direct client reads/writes to these documents remain denied, including
clients carrying an admin claim. Access is through the protected callable only.

To mark an account from a trusted project administrator's signed-in CLI:

```sh
node firebase/functions/set-admin.js mealsy-b732b ACCOUNT_EMAIL grant
node firebase/functions/set-admin.js mealsy-b732b ACCOUNT_EMAIL revoke
```

This preserves unrelated Firebase custom claims. Ordinary app users and admins
cannot assign administrator roles in the app. Reopen Account after a role change.
The current implementation does not include account deletion, subscription billing
changes, private-list inspection, or payment activation.
