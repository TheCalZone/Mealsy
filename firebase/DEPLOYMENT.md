# Mealsy backend deployment

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
