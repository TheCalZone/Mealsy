# Mealsy food community

The first version is a chronological feed visible to **all signed-in Mealsy users**.
It includes captions, food photos, recipe snapshots, likes, comments, reporting,
author deletion and moderator hide/restore controls. Android users can save a
shared recipe into their own recipe book or add its ingredients to their personal
shopping list. The desktop Community Studio supports reading, publishing, liking,
commenting and moderation.

Private recipe/profile documents and household shopping lists are never used to
populate the feed. Publishing is an explicit, confirmed action. Posts use a chosen
public display name; Firebase account email addresses are not published. Shared
recipes are independent snapshots with no local recipe IDs, checked shopping states,
private Firebase document IDs or owner metadata. Editing the private original does
not edit the published snapshot; delete the post and publish a new version to change
it. Display names are not unique or verified usernames.

## Deploy the backend

This builds on the household Firebase setup in [README.md](README.md). Firebase
Authentication Email/Password, Cloud Firestore, Node.js 22 and the Blaze billing
plan are required. No live Firebase resources were created/deployed by this code
change. Pulling the branch alone does not activate the community.

From the repository root on your desktop:

```sh
npm ci --prefix firebase/functions
npm test --prefix firebase/functions
npx --prefix firebase/functions firebase login
npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only "functions:households,firestore:rules"
```

The existing `households` function codebase now contains both `household` and
`foodFeed`. This command deploys both, preserving household functionality. Review
and merge `firebase/firestore.rules` with any other applications' rules before
deploying. Its owner-only `/users/{uid}` permissions preserve existing recipe/profile
access, and its default deny rule protects all feed collections. Feed access always
goes through the authenticated `foodFeed` callable function in `us-central1`.

The Android endpoint is in `RecipeApp/Services/FoodFeedService.cs`. If you change
Firebase project/region, update it and the desktop configuration/region accordingly,
along with the Android mobile Firebase configuration.

## Desktop Community Studio

You can use the same repository from your desktop Firebase extension/plug-in.
The deployable files are under `firebase/desktop`; functions and rules are under
`firebase/functions` and `firebase/firestore.rules`. No particular editor plug-in is
required: the Firebase CLI commands below work from a desktop terminal as well.

1. Open **Firebase Console → Project settings → Your apps → Add app → Web** in
   project `mealsy-b732b`. Copy the public web Firebase configuration.
2. On Firebase Hosting, the studio now loads its public web configuration from
   `/__/firebase/init.json` automatically, once a web app is registered in the same
   project. Its project ID must match `mealsy-b732b`; a mismatched config is rejected.
   If Hosting cannot supply that configuration, or you use another static host,
   replace the `ADD_...` values in `firebase/desktop/firebase-config.js` with the
   registered web app's public `apiKey` and `appId`. Never use an Admin private key.
   Check API-key restrictions for this web app and its Hosting domain.
3. Deploy the web studio from the repository root:

   ```sh
   npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only hosting
   ```

   Use the Hosting URL printed by the CLI. Sign in with your Mealsy account, or
   create an account from the studio. Check Firebase Authentication's authorized
   domains if you use your own custom domain. The studio loads the pinned Firebase
   browser SDK from `www.gstatic.com`.
4. Anyone signed in can publish and interact. Moderator accounts additionally see
   **Moderation view**, including hidden posts and up to ten unresolved reports per
   post/page. Hide/restore posts, delete content, or mark reports reviewed there.
   The feed and comments are paged; use **Load more/Older comments** to continue.
   Changes from other devices appear when you refresh. No background polling runs.

The checked-in desktop config intentionally contains placeholders. Firebase Hosting
can resolve these from its reserved endpoint; other static hosts need manual public
config. If neither is available, the page explains the missing setup.

## Grant your account moderator access

Find your account's UID in **Firebase Console → Authentication → Users**.
On a trusted desktop/server with Firebase Admin **Application Default Credentials**
for your project, run:

```sh
node firebase/functions/set-moderator.js mealsy-b732b YOUR_FIREBASE_UID
```

For example, a desktop with the Google Cloud CLI can establish ADC using
`gcloud auth application-default login` with an account authorized to administer
Firebase Authentication. Alternatively use `GOOGLE_APPLICATION_CREDENTIALS` pointing
to an Admin service-account file kept **outside this repository and all web/mobile
assets**. The script preserves your other custom claims and adds `feedModerator`.
Normal app users cannot grant this role themselves. Firebase CLI login and Admin
ADC are separate credential stores.

Sign out/in on Android, or press **Refresh** in the desktop studio, to obtain the
new role in your ID token. Remove moderator access with:

```sh
node firebase/functions/set-moderator.js mealsy-b732b YOUR_FIREBASE_UID --remove
```

Existing issued ID tokens can retain their old claims until expiry (up to one hour).
Only trusted administrators should run this script. Never paste Admin credentials
into the public web configuration.

## Data and behaviour

| Firestore path | Purpose |
| --- | --- |
| `feedPosts/{postId}` | Public snapshot, author UID/name, server-generated timestamp, counts, visibility |
| `feedPosts/{postId}/likes/{uid}` | One like per account; desired-state writes are idempotent |
| `feedPosts/{postId}/comments/{commentId}` | Comment text, public display name, author UID, server timestamp |
| `feedPosts/{postId}/reports/{uid}` | One private report per user/post, reason, review state |
| `feedLimits/{uid}` | Server-enforced post/comment rate limits |

All client reads/writes to these paths are denied by Firestore rules, including
moderators' direct SDK access. The callable function uses verified Firebase Auth and
Admin SDK transactions. Administrators using the Firebase console or trusted Admin
SDK bypass Firestore rules; prefer the provided moderation API so visibility and
counters stay consistent.

- Posts allow 2,000 caption characters, a 60-character public name, and an optional
  recipe with up to 100 ingredients and 15,000 method characters.
- Photos are small JPEG/PNG copies stored in the post document (up to 150 KB, at
  most 2048 × 2048 pixels). Android and the studio resize/re-encode selected images
  to remove EXIF/location metadata. This keeps the first version independent of
  Cloud Storage configuration; a larger media library should move to Storage.
- Publishing is limited to one new post per ten seconds/account; comments to one
  every three seconds/account; changed/new reports to one every ten seconds/account.
  Identical report retries do not reopen a report already reviewed by a moderator.
  A combined 500,000-byte UTF-8 payload limit rejects unusually large posts before
  any database write, and recipe servings must be at least one. Post/comment IDs make retries safe after a lost
  response. Likes use a requested final state, preventing duplicate counts.
- Hidden/deleted posts cannot be liked, commented on or read through the comments
  endpoint. Authors can remove their own posts/comments; moderators can remove
  others' content. Reports and moderation lists are restricted to moderator tokens.
- Deletion replaces the public post/comment with a tombstone and removes its shared
  content immediately. Nested likes/comments/reports may remain for administrative
  cleanup and are inaccessible through public APIs. This is not recursive erasure
  of all retained data. Private original recipes are kept.
- Publish drafts are saved per account on each device/browser. Drafts are not
  synchronized. A failed publish keeps the draft. **Discard draft** starts a new
  post if you want to abandon it. If a lost response was followed by changing an
  already-published draft, refresh to check the feed and discard the draft before
  creating a new post; the original post will not be overwritten.
- Android uses a virtualized feed collection; image data is fetched in pages of 15
  posts. Comments use pages of 20. Hidden/deleted records still advance cursors.
  This first release does not include follows, direct messages or push notifications.

Before a broad public launch, configure appropriate Firebase quotas, billing alerts
and the moderation process for the size of your community. App Check enforcement
is not enabled in this version; enabling it requires provisioning matching web and
Android providers and updating both clients to send their App Check tokens.

## Tests and remaining local checks

```sh
dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj
npm test --prefix firebase/functions
npx --prefix firebase/functions firebase emulators:exec --project demo-mealsy --only auth,functions "node --test firebase/callable.test.js"
```

Desktop UI checks use installed Chrome/Chromium and **mocked Firebase responses**:

```sh
npm run test:desktop --prefix firebase/functions
```

Set `CHROMIUM_PATH` to your Chrome/Chromium executable if it is not
`/usr/bin/chromium` (on Windows, set it to the installed `chrome.exe` path).
The suite checks production HTML/JavaScript rendering, publication confirmation,
likes, moderator controls, text escaping, sign-out and a narrow viewport.

Real Firestore/rules integration needs Java 21 and the emulator JAR:

```sh
npx --prefix firebase/functions firebase emulators:exec --project demo-mealsy --only firestore "npm run test:emulator --prefix firebase/functions"
```

The cloud network policy blocked the Firestore JAR download from
`storage.googleapis.com` (403 Domain forbidden), so that integration suite remains
unrun here. Auth/Functions protocol checks do run locally against their emulators;
backend transaction tests use a deterministic database double. Emulator checks use
the host Node 24; deployment is pinned to Node 22.

After deployment, test Android and the hosted studio using two ordinary accounts
and a moderator: publish a photo/recipe, refresh on the other device, like/unlike,
comment, save the recipe, report, hide/restore, delete, sign out and reconnect after
a failed publish. Confirm private household lists/recipes never appear in the feed.
Native photo picker, keyboard, layout and share-to-feed navigation require device
validation; cloud build success does not establish those interactions work.


## Deploying the current version

The latest changes were tested locally; **no live project deployment or database
edition inspection was possible in the cloud because Firebase CLI was not signed
in**. No production database, rules, users, billing plan or content was changed.
The Functions use the existing default Firestore database and current native SDK;
there is no migration or new database provisioning in this update. Do not create
another database just to activate the feed.

After pulling `codex/android-runtime-fixes`, from the repository root:

```sh
npm ci --prefix firebase/functions
npx --prefix firebase/functions firebase login
npx --prefix firebase/functions firebase firestore:databases:list --project mealsy-b732b
npx --prefix firebase/functions firebase firestore:databases:get '(default)' --project mealsy-b732b
npm test --prefix firebase/functions
npm run deploy:social --prefix firebase/functions
```

The deploy script explicitly targets `mealsy-b732b` and deploys the **whole existing
households Functions codebase** (household, feed, billing and scheduled billing
reconciliation), Firestore rules and Hosting together. It does not delete another
Functions codebase. A predeploy hook runs the backend tests and blocks deployment
if they fail. Review the rules first if other apps use this Firebase project.
Enable Email/Password sign-in, use the existing default database, register a web app,
and ensure Blaze billing and appropriate deployment permissions are available.
Billing functions also need the existing Play billing IAM/Pub/Sub setup described
in [MONETIZATION.md](../MONETIZATION.md); deploying does not configure Google Play.

Hosting now sends a Content Security Policy, anti-framing, MIME protection,
referrer and permissions headers. JavaScript revalidates its cache on navigation
so redeployments pick up new public configuration. The CSP names this project's
Auth and callable hosts: update it if you change project, region or login provider.
The present feed pagination uses `createdAt` and the document ID, supported by
Firestore's automatic indexes; no new composite index is required by this change.
Do not disable these indexes. Photo data remains bounded inline content, so no
Storage bucket or storage access rules are required for this version.

Feed pages and their like state now use one transaction snapshot. Hidden/deleted
posts are excluded consistently within that snapshot; content already delivered to
a reader cannot be recalled. Moderator reports stay on the server and direct client
reads/writes remain denied. App Check enforcement is still off because both Android
and web providers/tokens must be configured before enabling it; switching it on at
the server alone would lock out the current app. There are no push notifications
or automatic external moderation services configured by this update.
