# Household shopping lists

Every person signs in with their **own Firebase Authentication account**. A household
has one owner and up to 20 members. Each account has a private personal shopping list
and can join one household with one shared list. All members can add, edit, remove,
and collect items. Owners can invite/remove members, revoke invitations, and delete
the household. Other members can leave. Deleting a household permanently deletes
its shared list, revokes its invitation, and removes all memberships; personal lists
remain. An owner can delete their household before joining another one.

## One-time Firebase setup (required)

Pulling/building the Android app does **not** deploy this backend. Until it is
deployed, lists still save on the device, but household management and cloud sync
will report that the service is unavailable.

1. In Firebase Console select **mealsy-b732b**, verify **Authentication → Sign-in
   method → Email/Password** is enabled, and create a **Cloud Firestore** database
   if it does not already exist. Cloud Functions deployment requires Firebase's
   **Blaze billing plan**. Calls and database reads/writes may incur charges.
2. Install **Node.js 22 LTS** on your computer. In a terminal at the repository root:

   ```sh
   npm ci --prefix firebase/functions
   npm test --prefix firebase/functions
   npx --prefix firebase/functions firebase login
   ```

3. Review `firebase/firestore.rules` against any existing rules before deployment.
   They preserve owner-only access to `/users/{uid}` and its recipes/snacks/staples,
   and deny direct client access to all other paths. New list and membership access
   goes through the authenticated function, which performs its own checks. If your
   Firebase project has other applications/collections, merge their rules first.
4. From the repository root deploy the supplied backend and reviewed rules:

   ```sh
   npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only "functions:households,firestore:rules"
   ```

   This creates the `household` callable function in **us-central1**. The matching
   Android endpoint is in `RecipeApp/Services/HouseholdService.cs`. If using another
   Firebase project or region, update both this endpoint and the function region,
   and use the matching Android `google-services.json`.

5. Pull/build the Android branch in Visual Studio and install on two devices.
   Sign in using **different accounts**. On the owner's device open **Account →
   Your household → Create a household → Invite a family member**. Share the code
   via the phone's share sheet or copy it. On the second device choose **Your
   household → Join with invitation code**. Use the selector at the top of the
   shopping screen to choose **Personal list** or **Household list**.

No service-account private key or email-provider secret is needed by the app.
Invitations are shared by the owner through the native share sheet; the backend
**does not send email**. The code grants membership to whoever redeems it: share
it privately with the intended person. Codes use 192 bits of randomness, are
stored as SHA-256 hashes, expire after seven days, and can be used once. Only one
unused invitation exists per household; generating another revokes the previous
one. Joining explicitly tells the user their account email is visible to members.

## Saving, sync and conflict handling

- Signed-in personal lists and household lists sync while the shopping screen is
  open, approximately every five seconds, with **Sync now** available in the menu.
  Leaving the screen also attempts to send pending changes. The application does
  not run a background sync service; reopen the shopping screen to retry pending
  changes. Polling stops when the screen is closed.
- The device keeps a separate cache and last acknowledged server state for each
  account/list. Offline edits survive app restarts. The status line distinguishes
  device saves, pending/unavailable cloud access, and acknowledged sync.
- Existing unscoped lists stay under **Device list** when signed out. After sign-in,
  **Your household → Copy this device's original list to my personal list** explicitly
  copies them into that user's private list. This avoids uploading a previous
  device user's list into another account. The original is kept.
- Recipe shortcuts and newly generated recipe/meal-plan lists target the personal
  list. Switch to the household list to add/paste household items. A personal list
  can also be copied using Share/Copy and then Paste on the household list.
- Sync sends item changes against the last acknowledged state. The server applies
  them in a Firestore transaction and rechecks membership. Concurrent additions and
  edits to different fields merge. Repeated requests after a lost response are
  idempotent. Conflicting edits/deletes stop automatic sync; neither version is
  silently overwritten.
- **Resolve sync conflict** lets you share/copy your device version before loading
  the latest cloud version. Reloading keeps a device recovery copy; use the same
  menu later to share that copy and reapply any desired changes. Undo history resets
  when a remote update replaces the visible document, preventing undo from removing
  another member's changes. Undo of an already-synced local change is a new patch
  subject to the same conflict checks.
- Lists are limited to 500 items / 450 KB; names, categories and quantities to 300
  characters and notes to 2,000 characters. Oversized changes stay on the device
  with an error until shortened.
- Household membership is verified before reopening a cached shared list. If access
  cannot be confirmed, the personal list opens. Removed members cannot make further
  server reads/writes. Their previously downloaded device cache is not a remote
  access grant and is never loaded into another user's account.

## Tests

Backend domain and application tests (no cloud credentials, transaction test double):

```sh
npm ci --prefix firebase/functions
npm test --prefix firebase/functions
```

The actual callable wrapper and Auth token protocol can be tested with Node.js 22:

```sh
npx --prefix firebase/functions firebase emulators:exec --project demo-mealsy --only auth,functions "node --test firebase/callable.test.js"
```

Real Firestore transactions and security rules require **Java 21** and the Firestore
emulator download. The test uses a demo project and refuses to run without an emulator:

```sh
npx --prefix firebase/functions firebase emulators:exec --project demo-mealsy --only firestore "npm run test:emulator --prefix firebase/functions"
```

The cloud environment passed the domain/application tests and Auth/Functions HTTP
checks. Its network policy blocked the Firestore emulator JAR download from
`storage.googleapis.com` (HTTP 403, Domain forbidden), so **real Firestore/rules
integration remains unverified here**. The Auth/Functions checks used the host's
Node 24; deployment is pinned to Node 22. Android UI and two-device live sync need
local validation after Firebase deployment.

On two devices verify simultaneous additions, check/uncheck, quantity/note edits,
private list isolation, offline edit/restart/reconnect, conflicting edits, invitation
reuse/expiry/revocation, member removal, leaving and owner deletion. Confirm a
removed account cannot reopen the household list and that everyone's personal lists
remain after leaving/removal/deletion.

## Food community

The same backend also includes `foodFeed` and an optional desktop studio. See
[Food feed deployment and moderation](FEED.md) for web configuration, Hosting, and
moderator roles.
