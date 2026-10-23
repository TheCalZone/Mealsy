# Firebase handoff: friends, privacy, household meals and content sharing

Reviewer creation and optional manual email confirmation: [REVIEWER_ACCOUNT_HANDOFF.md](REVIEWER_ACCOUNT_HANDOFF.md). Creation uses existing sign-up/grant services; manual confirmation needs the administration backend update.

Latest household roles/request rollout: [HOUSEHOLD_ROLES_HANDOFF.md](HOUSEHOLD_ROLES_HANDOFF.md). Backend deployment and staging checks are required before relying on restricted access.

For current release blockers and the staged account-deletion deployment sequence,
start with [PRODUCTION_DESKTOP_HANDOFF.md](PRODUCTION_DESKTOP_HANDOFF.md) and
[ACCOUNT_DELETION.md](ACCOUNT_DELETION.md). Social features remain paused.

## Applied scope — 18 October 2026

The user reconfirmed that social/creator work stays paused for cost and scope control.
The friends/privacy API below remains a future contract, not an enabled service.

Implemented `householdMeals` with version-1 capability discovery, server-derived
actors, current membership/owner entitlement checks, strict date and safe-snapshot
validation, per-entry versions, idempotent writes/deletes and tombstones. Shared
weeks are bounded documents at `households/{id}/mealWeeks/{weekStart}`. There is no
collection scan or continuous planner polling. A week permits 100 visible meals,
500 retained entry IDs and a 750 KB map; oversized writes fail without partial saves.
Personal device plans and recipe books are not migrated or published.

The household-deletion trigger recursively removes its remaining shared weeks.
Access stops as soon as the household/membership is removed, independently of
asynchronous cleanup. Existing default-deny rules still prohibit direct SDK access.

`firebase/functions/product-features.js` now enforces the pause on deployed public
feed, business, promotion and social-inbox callables, before database/moderation
work. Authentication is still checked first. The social push trigger returns before
querying devices or sending. Subscription verification, household shopping, shared
meals, administrator support and required payment/refund reconciliation remain
available. Stored posts, profiles and receipts are retained; no paid campaigns
are enabled. The browser release flag also remains false.

The content-sharing public read delta stays in source behind the server pause.
The invitation/share landing pages are the paused, shopping-focused versions.
Functions and Hosting were deployed to `mealsy-b732b` on 18 October 2026. Nine
read-only production checks passed. This is not a claim of an authenticated
two-phone or iOS integration test; those device checks remain outstanding.
Store price configuration, Apple purchases/APNs and physical-device verification
remain external work. See `IOS_SETUP.md` for the current iOS assessment.

**Social work is currently paused.** The client hides and blocks social/creator
features; keep these contracts for later rather than implementing/enabling them
unless the user resumes that work. Household shopping and meal planning remain
in scope. See [SOCIAL_PAUSE.md](SOCIAL_PAUSE.md).

**Client branch:** `codex/android-runtime-fixes`. Friends and household meals implement
client DTOs and API contracts only. The later content-sharing change also adds a
`foodFeed.getPost` handler and static Hosting files **in source**. Nothing has been
deployed and no production resources or account data were changed. The new `social`
and `householdMeals` functions below do not exist in the previous deployment. New social/planner screens show a setup/connection error
until they are implemented. Existing personal plans remain local and unchanged.

Private posts never fall back to the existing public endpoint. Ordinary public
posting remains on the existing `foodFeed.publish` path with explicit public
consent. Never interpret a new client's friends/Only me selection as a cosmetic
label on a public document.

## Current client entry points

- Food feed: Friends feed; Friends & requests; tap an author's profile.
- Friends: accepted friends, incoming requests, sent requests, blocked accounts,
  people search, self profile, public bio/photo/discoverability editing.
- Profile: request/cancel/accept/decline/remove, block/unblock and report. Following
  and friendship remain distinct; a follow is not automatically an accepted friend.
- Conversation: audience badge, likes, comments/replies, comment deletion, reporting,
  recipe cooking/private copy, author deletion and audience changes.
- Composer: Everyone in Mealsy, Friends only, Only me; the audience is saved with the
  draft. New and old drafts default conservatively to Friends only if no valid
  audience is recorded. Public posts can still publish using the old public service.
- Household plan: Home “What's for dinner?”, Account, My household and Meals toolbar.
  Shared weeks show all seven days, breakfast/lunch/dinner, typed meals, portions,
  notes and optional recipe snapshots. Any current entitled member can add, edit
  or remove a meal. Personal planning remains separate. Failed saves keep the form;
  copy-draft and explicit review-latest actions support conflict recovery.

## Deployment architecture

Add authenticated callable functions in **us-central1**, same project/codebase:

- `social`: `https://us-central1-mealsy-b732b.cloudfunctions.net/social`
- `householdMeals`: `https://us-central1-mealsy-b732b.cloudfunctions.net/householdMeals`

Use the existing callable wrapper/error-correlation conventions, verified Firebase
Auth tokens and current disabled-user checks. Keep functions bounded; add App Check
only after providers/tokens are available to every supported client. Do not trust
client IDs as actors, client Premium booleans, timestamps, role names or usernames.
Do not enable paid business promotions as part of this work.

The client posts the usual `{data:{action,...}}` callable envelope and consumes
`{result:...}` or Firebase callable error objects. All DTO/request properties are
**camelCase**; dates are plain ISO calendar dates, not UTC instants. Return empty
arrays, not null, and null for the final cursor. The new cursor is an opaque string,
not the legacy `{createdAt,id}` feed cursor. Return stable bounded pages (20 is a
reasonable default); the client must not scan entire user or post collections.

### Capability gates (deploy last)

`social {action:"capabilities"}` → `{version:1, privacyEnforcedVersion:1}`.
`householdMeals {action:"capabilities"}` → `{version:1}`.

Do not advertise `privacyEnforcedVersion:1` until visibility is checked on every
read, mutation, pagination path, notification, search, profile and legacy route.
The client refuses private publishing without this explicit capability. Connection
failure or an old version keeps the draft, never retries it as public.

## Social actions and response contracts

Common user fields: `userId` is the target, never the authenticated actor. Friend
mutations return `{}`. Requests and repeated desired-state mutations are idempotent.

| Action | Request fields | Result |
|---|---|---|
| `searchPeople` | `query` (2–60 chars), `kind:"search"`, `cursor` | `{people:[Person],cursor}` |
| `friendList` | `kind:"friends"|"incoming"|"outgoing"|"blocked"`, `cursor`, optional unused `query` | `{people:[Person],cursor}` for the caller only |
| `viewProfile` | `userId` | `Profile` |
| `updateProfile` | `bio`, `avatar`, `discoverable` | `{}`; actor's profile only |
| `requestFriend`, `cancelFriend`, `acceptFriend`, `declineFriend`, `removeFriend`, `block`, `unblock` | `userId` | `{}` |
| `reportProfile` | `userId`, `reason` (1–500 chars) | `{}`; private moderator report |
| `friendsFeed` | `cursor` | `{posts:[Post],cursor}` |
| `profilePosts` | `userId`, `cursor` | `{posts:[Post],cursor}` visible to caller |
| `getPost` | `postId` | `{post:Post}` |
| `publish` | `id`, `caption`, `recipe|null`, `photo`, `audience` | `{id}`; new private pipeline |
| `setAudience` | `postId`, `audience` | `{}`; author only |
| `like` | `postId`, `liked` boolean | `{}`; desired state |
| `comments` | `postId`, `cursor` | `{comments:[Comment],cursor}` |
| `comment` | `postId`, `id`, `text`, `parentCommentId|null` | `{}`; stable ID makes retries safe |
| `deleteComment` | `postId`, `commentId` | `{}`; author/moderator rules |
| `reportPost` | `postId`, `reason` | `{}` |
| `deletePost` | `postId` | `{}`; author/moderator rules |

`Person = {userId, username, bio:"", avatar:""}`. Do not return account email,
real-name profile fields, phone numbers, device tokens or private recipe IDs.

`Profile = {userId, username, bio, avatar, relationship, friendCount, discoverable}`.
Relationship from the **caller's** view is `none|incoming|outgoing|friends|blocked`.
`blocked` means the caller blocked this account. If the subject blocked the caller,
return generic NOT_FOUND; don't expose who blocked whom. Permit the caller's own
blocked list and a minimal blocked profile for unblocking, with no biography/photo,
posts or friend details. Profile search excludes blocks in either direction. A
non-discoverable profile is excluded from new searches but may still be opened by
its author, existing friends or through an authorized visible post; don't describe
search discoverability as content privacy. Do not expose another user's friend list.

`Post` uses `FoodPost` fields from `Models/FoodPost.cs`: `id`, `authorId`,
`authorName`, `createdAt` (server milliseconds), `caption`, `photo`, `recipe`,
`likeCount`, `commentCount`, `liked`, **`audience`** (`public|friends|onlyMe`) and
**`privacyManaged:true`** for protected posts. Existing public feed records may
remain `privacyManaged:false` / `audience:"public"`. Protected records without
valid privacy metadata are refused by the client. `friendsFeed` includes only
accepted mutual friends' readable posts, plus caller-owned posts if desired;
exclude unrelated public posts and follow-only accounts. Decide/order caller-owned
posts consistently and document this choice in tests.

`Comment = {id,text,authorId,authorName,createdAt,parentCommentId?,replyToName?}`.
Use the server-owned community username, never the submitted display name.
Comments/replies inherit the current parent post's access. Do not accept replies
to comments on another post. Deleted-parent replies may remain with a removed
parent placeholder; never disclose deleted text or hidden conversations.

### Friendship state machine and abuse controls

Store each unordered pair once, or write both account indexes atomically. Suggested
states are pending (requester/recipient), accepted and removed; blocks are separate,
private caller-owned documents. Only the recipient can accept/decline; only the
requester can cancel; either accepted friend can remove. Reject self requests and
requests to missing/disabled users or blocked pairs. Concurrent opposite requests
must produce one predictable state (don't silently imply consent). Unblocking does
not restore friendship. Block removes pending/accepted edges in the same transaction
and prevents future requests, comments, likes, profile discovery and notifications
in both directions. Apply the block policy to legacy public interactions too.

Add per-account request/search/report/comment rate limits and bounded friend counts,
idempotency keys and paginated indexes. Don't allow rapid request/cancel cycles to
spam recipients. Moderate profile text/photos and all submitted social content
using the existing fail-closed safety pipeline. Do not make Only me an exemption
from server validation, and disclose provider processing in the privacy policy.

### Privacy storage: avoid the legacy leak

**Do not put friends/Only me documents into `feedPosts` while old functions or
web clients still read it as public.** Prefer a separate server-only `socialPosts`
collection for protected records. Keep existing public publishing in `feedPosts`
until migration is proven. A new API can adapt authorized legacy public records
for profiles/conversations, but must preserve the correct routing/source identity.

Use globally unambiguous opaque post IDs or a server mapping for legacy and new
records. For an ID reused after a lost publish response, check both namespaces and
content/audience digest: return the original success only for the exact same actor,
content and audience; otherwise reject. Never create a public duplicate when a
private publish's response was lost. Existing public `foodFeed.publish` must gain
this cross-namespace safeguard before private posting is activated.

A post is readable only when not hidden/deleted, no relevant block exists, and:
`public`: any signed-in eligible viewer; `friends`: author or accepted mutual friend;
`onlyMe`: author only. Privileged moderation must be deliberate, logged and outside
normal viewer APIs. Follow edges and household membership do not grant friend-post
access. Re-evaluate on every request, including recipe/photo retrieval, comments,
likes, search, profile lists, notifications and reply previews. Hidden/deleted posts
remain inaccessible. Private bodies must not appear in logs, analytics, ad auctions,
previews, reports to non-moderators or public search indexes. Exclude all nonpublic
posts from promotions. Friendship removal and privacy changes invalidate access
on the next request; content already delivered/saved/screenshotted cannot be recalled.

For a legacy public post changed to friends/Only me, atomically tombstone/remove its
public projection and create/update the protected representation, or use a proven
server visibility filter across every legacy route before retaining it in place.
Counters/likes/comments must remain consistent. Never leave the public copy readable.
If you merge new public records into the legacy feed, set `privacyManaged:true` so
the client uses the new conversation routes, and ensure old app/web clients cannot
bypass permissions. Old public posts remain public unless the author explicitly
changes them; never infer private audiences for old records by default.

## Household meal-plan API

Every operation checks **current** membership and active owner Premium entitlement
(including existing administrator grants), using the same logic as shared shopping.
The caller's own Premium subscription does not buy access to another owner's paused
household. Do not trust a client-provided household ID without membership validation.

| Action | Request | Result |
|---|---|---|
| `readWeek` | `householdId`, `weekStart` | `Week` |
| `upsertMeal` | `householdId`, `weekStart`, `meal`, `expectedVersion` | `{}` or updated version |
| `deleteMeal` | `householdId`, `weekStart`, `mealId`, `expectedVersion` | `{}` |

`Week = {householdId, weekStart:"yyyy-MM-dd", meals:[Meal], revision}`.
`Meal = {id,date,slot,title,servings,note,recipe:null|RecipeSnapshot,version,updatedBy}`.
`id` is a lowercase 32-character GUID. `date` is a valid calendar date in the seven
consecutive days beginning `weekStart`. Slot is `breakfast|lunch|dinner`. Multiple
entries per slot are allowed. Title is 1–160 chars, servings integer 1–1000, note
0–500 chars. Use the existing public-safe `FeedRecipe` snapshot schema/limits for
recipe attachments; copy only ingredients/method/title/description/times/servings,
never owner IDs, SQLite IDs, source URL, private notes or shopping preferences.
`updatedBy` is the server-derived actor, not a client choice. Recipe quantities
remain at the snapshot's original servings; the client scales for the planned
meal's portions when opening the cooking view.

Suggested path: `households/{id}/mealWeeks/{weekStart}/meals/{mealId}` with a small
week revision header. Read/create/update/delete in bounded transactions. New
entries require expectedVersion 0; edits/deletes require the current entry version.
Increment entry version and week revision atomically. Independent meal edits can
merge; competing edits to the same meal return ABORTED/FAILED_PRECONDITION with a
controlled conflict message. A retry of an identical successful write/delete must
be idempotent. Keep deletion tombstones/version history long enough to prevent a
stale editor from resurrecting a meal. Review-latest lets a user explicitly rebase
its draft version, but another intervening edit must still reject the save.

Membership removal must end access immediately; expiry pauses writes/reads according
to your existing shared-data retention policy without deleting plans. Household
deletion must include shared weeks and snapshots in its cleanup. Do not migrate,
replace or publish anyone's existing device-local personal plan. There is no
background polling/real-time subscription in this client: refresh or reopen to
fetch the current household plan. The Home shortcut opens the shared week rather
than copying private meal-plan rows. Push “dinner changed” can be added later using
the existing opt-in notification system, with no meal contents on lock screens.

## Firestore, indexes and activation sequence

Keep direct client SDK reads/writes denied for friendship, protected posts, profile
updates, private reports, counters and household weeks. All access is through the
verified callables; Admin SDK transactions bypass rules, so rules alone are not
sufficient. Determine the actual database edition before configuring indexes. Add
bounded server indexes for recipient+request time, account friendship lists,
author+createdAt profile pages, accepted-friend feed lookup and meal date/slot.
Avoid unbounded `in` arrays, loading every user, and scanning an entire community.
Use stable tie-breaking IDs/cursors and advance through filtered/hidden records.
If media moves to Storage, private media needs current authenticated authorization
or short-lived access, not permanent public download-token URLs.

1. Implement the new functions plus privacy/block checks on all existing routes;
   keep capability flags off initially. Do not relax default-deny rules.
2. Add unit and real Firestore emulator tests: spoofed actor IDs, accept/cancel races,
   block/unblock, removed friends, privacy changes, mixed legacy/new data, unsigned
   access, disabled accounts, moderator boundaries, idempotent retries, hidden posts,
   private search/notification leakage, invitation/membership revocation, entitlement
   expiry, invalid dates, recipe-snapshot privacy and concurrent household edits.
3. Deploy server/index/rule changes to an isolated test project first. Exercise old
   Android/web clients as well as this frontend. Expose version 1 only after access
   control is complete, then deploy the full reviewed codebase to `mealsy-b732b`.
4. Test two friends plus a stranger and two household members on phones. Publish
   Friends/Only me, open conversation links as stranger, unfriend/block, change
   visibility, edit the same dinner concurrently, expire the owner entitlement and
   remove a member. Verify drafts and personal plans remain intact on failure.

This frontend does not implement direct messages, end-to-end encryption, automatic
friend discovery from contacts, shared recipe-book permissions, or a new billing
product. Do not advertise these. Friends/social interactions use existing signed-in
accounts; household planning is covered by the owner's existing Premium plan.

## Activity navigation contract

Extend existing notification items additively with `privacyManaged: true` for posts
served through `social` and `targetUserId` for friend request/acceptance notifications.
For friend events omit `postId`; the client opens the target profile. Existing public
post notifications keep the legacy route. Do not include protected post captions,
photos, recipes or comment bodies in notification messages or push payloads. Recheck
visibility on notification reads and on opening the destination; suppress stale
notifications after blocking, unfriend, privacy changes or deletion. Authorize marking
notifications read against the recipient UID. Friend notifications must be generated
server-side and deduplicated from successful relationship transitions.

## Content sharing and creator meal plans (17 October 2026)

See [CONTENT_SHARING.md](CONTENT_SHARING.md) for release and Android device checks.

Client URLs use `https://mealsy-b732b.web.app/share/?kind=KIND&id=ID`:
`post` addresses the legacy public feed, `social` addresses the privacy-aware
service, and `plan` addresses a published creator collection/weekly plan.
The same target opens through `mealsy://content?kind=KIND&id=ID`. Links contain
identifiers only; neither signed URLs nor caption/photo/recipe snapshots are
embedded. They do not confer access. The static page never reads Firestore or
fetches post content. Social network previews use generic Mealsy text/artwork.

**Implemented backend delta, still requiring deployment:** `foodFeed.getPost`
accepts `{ action: "getPost", postId }` and returns `{ post: FoodPost }`. It
requires auth, reads visibility and the viewer's like in one transaction, rejects
missing/hidden/deleted posts and explicitly rejects nonpublic or privacy-managed
records. It returns only the existing public DTO fields plus explicit
`audience: "public"`, `privacyManaged: false` and `liked`. It is never used as a
fallback for a failed `social.getPost`. Include this action in operational logs.
When implementing the broader social backend above, apply its block/visibility
policy here and to all legacy reads too; the legacy implementation has no new
friend/block database and cannot enforce a not-yet-implemented relationship model.

Creator shares reuse **existing** `business.collection` reads and existing
`saveCollection` publishing; they require signed-in users, a published collection
and verified creator, and omit removed/hidden/unapproved recipes. There is no new
public anonymous collection API or client direct database read. Current server
visibility rules remain authoritative. Consider transactionally consistent reads
when extending this service for new audience support. Do not put household or
friends-only plans into the public business collection namespace.

Recipients can save selected recipe copies and shop directly, or import selected
meals into their personal week. Repeated recipes combine their selected portions
before cupboard review. Existing private recipe edits and shopping items are
preserved. Weekly template saving remains governed by its existing Premium flow;
opening a creator's share and shopping its recipes do not add a new paywall.

Deploy the static `/share/` directory using existing Hosting; keep the desktop
studio at `/` and invitations at `/join/`. Then configure Digital Asset Links
with the **Play App Signing** SHA-256 certificate for both HTTPS hosts. No actual
certificate was guessed or committed. Without it, use the explicit Open Mealsy
button on the landing page. Update store links if the package/listing changes.
There is no Firebase Dynamic Links dependency or automatic deferred install
attribution. After first installation users return to the share page or reopen
its link. Friend/private shares require the new social service before release.

## Google Play seven-day trial hookup — 22 October 2026

Client source supports `mealsy_family`, base plans `monthly` and `annual`, offer
ID `seven-day-trial` under each plan. Configure/activate each desired offer in
Play Console: new-customer acquisition eligibility controlled by Google Play
(not developer-determined), one free seven-day phase (P7D or P1W), then the base plan's normal
recurring P1M/P1Y phase. No intermediate introductory discount phases are supported
by this first client selector. The offer ID is unique within each base plan.

The client only selects this configured offer when returned by Google Play with
that exact phase structure, positive recurring price and matching currency. It
falls back to the base offer if no eligible trial is returned. It does not infer
trial eligibility from Mealsy sign-in or subscription status, persist a local
trial entitlement, or grant access on the basis of marketing UI. Checkout reloads
the offer and requires the displayed quote to match before launching Play.
Extra-place subscriptions are unaffected. Existing backend receipt verification,
acknowledgement and subscription notifications remain authoritative; a verified
active trial uses the subscription's actual Play expiry, not a guessed local date.

No Play Console configuration or Firebase deployment was performed here. Test
using a Play internal-testing release installed through Play and licence-test
accounts. A sideloaded test-signed APK can show the updated promotions and normal
unavailable-store handling but cannot establish real trial eligibility or checkout.
Test a new eligible Play account, previously used trial, existing subscriber,
cancelled trial, offline query, changed/withdrawn offer and renewal/cancellation
notifications. Confirm the paid price/period after the trial and final first-charge
date in the Play purchase sheet. Never use real charges merely to exercise UI.


## Automated account deletion and policy audit — 22 October 2026

See ACCOUNT_DELETION.md for the three new Firebase functions, required indexes,
rules/callable guards, TTL and staged validation. Deploy the entire functions
codebase plus rules/indexes together; the test APK does not deploy them. Public
privacy and external account-deletion pages live in firebase/desktop/privacy and
firebase/desktop/delete-account, with Callum@brightbark.co.uk as the confirmed
support contact. Publish/review them and then configure the app/Console policy
URL. PLAY_POLICY_AUDIT.md records remaining target-SDK/native-page-size and store
configuration blockers; PLAY_DATA_SAFETY.md is a draft inventory, not a submitted
declaration. No production account was deleted or backend deployed here.

## Shared shopping contributor badges

See [SHOPPING_ATTRIBUTION_HANDOFF.md](SHOPPING_ATTRIBUTION_HANDOFF.md) for trusted Added by / Requested by fields, compatibility, anonymization and the required household + deletion-worker deployment. New badges need both the updated app and backend; existing items are not backfilled.
