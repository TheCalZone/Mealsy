# Mealsy backend deployment

## Latest deployment — 18 October 2026

Functions and the paused Hosting release are deployed to `mealsy-b732b`.
`householdMeals` now supports authenticated shared-week reads and versioned meal
edits/deletes; `cleanupHousehold` recursively removes retained household weeks.
The server social release flag pauses `foodFeed`, `business`, `notifications` and
`promotions`; `deliverActivity` does not send social alerts while paused. The
`administration`, household shopping, billing and reconciliation services continue.
No new friends/privacy service or paid campaigns have been enabled.

Eight callable endpoints passed unsigned-request authentication checks; the ninth
live check verified the paused website flag and invitation/share landing pages.
112 backend unit tests, seven real Firestore emulator tests, 14 browser tests,
321 shared app tests and the Android build passed. Physical two-phone/iOS tests
remain outstanding. The current database is still Standard/default in `eur3`;
no rule relaxation, database migration or new index is required for embedded week
documents. Callable endpoints are in `us-central1`; Firestore event handlers are
in `europe-west1`.

See `FIREBASE_HANDOFF.md` and `IOS_SETUP.md` in the repository root for current
scope and platform limitations. The following notes describe the older baseline.

Target project: `mealsy-b732b` (also the default in `.firebaserc`).
The existing `(default)` Firestore database is **Standard edition**, native mode,
in `eur3`. Functions retain `us-central1` to match the installed Android client.
No database migration is required.

**Deployment completed on 17 October 2026:** all five functions below and the
existing Firestore rules were successfully deployed. The three public callable
endpoints passed the live authentication checks. Firebase configured Artifact
Registry cleanup to remove deployment images older than one day.

The backend contains:

| Function | Purpose |
| --- | --- |
| `household` | Private and household shopping lists, invitations and membership |
| `foodFeed` | Posts, recipe snapshots, likes, comments and moderation |
| `billing` | Google Play subscription verification and plan status |
| `playSubscriptionUpdates` | Subscription updates from the `mealsy-play-billing` topic |
| `reconcileSubscriptions` | Hourly subscription reconciliation |

Profiles, recipes, snacks and essentials use the existing Firebase mobile SDK and
owner-scoped `/users/{uid}` documents. Meal plans, editor drafts and device settings
remain local, as described by the app. Billing entitlements and household/feed data
are accessed through authenticated functions rather than direct client writes.

## Deploy

With Node.js 22 and Firebase CLI dependencies installed:

```sh
npm ci --prefix firebase/functions
npx --prefix firebase/functions firebase login
npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only "functions:households,firestore:rules"
```

The predeploy hook runs backend tests directly with Node, so it does not depend on
an `npm` executable in the deployment subprocess. On the first deployment, Firebase
asks to accept retries for subscription notifications; these use idempotent receipt
verification. In an interactive terminal, review and accept that prompt.

## Verification

Validated on 17 October 2026:

- 65 backend tests, including malformed callable payloads and shopping changes.
- 5 Auth/Functions emulator tests of the callable HTTP/token protocol.
- 3 Firestore emulator integration tests using real transactions and security rules.
- Android package/project/endpoint consistency and Firestore rule compilation.
- 3 live endpoint checks: `household`, `foodFeed` and `billing` each returned
  HTTP 401 with `UNAUTHENTICATED` for unsigned requests.

Local emulator tests used Node 24; the deployed runtime is configured as Node 22.
The Firestore tests ran with the pinned CLI 14.22.0 and Java 11 on local port 8185
because port 8080 was occupied. Use Java 21 for newer Firebase CLI releases.
The .NET tests could not start because this computer lacks the SDK required by
`global.json` (8.0.400 or a newer .NET 8 feature band).

After deployment, run the read-only production endpoint checks:

```sh
node --test firebase/live.test.js
```

These send unsigned requests and require all three callable endpoints to return
Firebase's `UNAUTHENTICATED` response. They create no users or documents. They do
not replace signed-in Android/two-device tests or Google Play purchase testing.

## Separate publisher setup

Deploying billing functions does not create Google Play subscription products or
grant the runtime service account Android Publisher permissions. Configure the
products, service-account permissions and Play notification topic using
[MONETIZATION.md](../MONETIZATION.md) before testing real subscriptions.

The optional desktop Community Studio also needs a registered web app and Hosting
deployment; see [FEED.md](FEED.md). It is separate from the Android backend rollout.
