# Automated Mealsy account deletion

Source implementation; **not deployed by this change**. Do not test destructive
requests on a real account until a staging/emulator rollout passes.

## User flow

Settings/Account → Privacy, data & account deletion → Delete my account
automatically. User confirms their password with Firebase, reviews owned shared
spaces, types DELETE and consents to removal of any owned households/workspaces.
The backend checks Firebase auth_time (five-minute window), derives uid from auth,
rejects extra payload keys and queues an idempotent job. It never accepts an
arbitrary target uid. The response is **accepted**, not a claim of completion.
The app stores a random receipt, blocks edits, clears this account's local caches
and signs it out. Cleanup failure is retried at startup before normal data load.
Offline caches on OTHER devices and private copies saved by other people cannot
be remotely erased; clear app data on those devices where necessary.

Google Play subscriptions are NOT cancelled or refunded. The UI warns and links
to Google Play management before requesting deletion. A trial can still renew if
the user fails to cancel it; deletion must not obscure that fact.

## Backend lifecycle

`accountDeletion` callable creates `accountDeletions/{uid}`. Firestore-created
worker disables Auth/revokes refresh tokens, cleans known account-owned records,
then waits at least two minutes for a later sweep before deleting Auth. The
five-minute recovery scheduler resumes queued, failed, expired-lease and settling
jobs. Work is leased/paginated with cursor checkpoints. A failed step never marks
a job complete. Deployment/index errors leave retryable jobs requiring operator
attention. Monitor queue age, retries and safe error codes.

Deletion covers users/{uid} recursively (recipes/photos/checklists/lists), feed
profile/settings/limits, authored posts and nested contributions, comments/reports,
likes/follows, notification records/tokens, invitation/membership records, grants,
purchase links/entitlements and owned business records. Owned household trees
(including shared weeks) and owned creator spaces are removed only after explicit
consent. Member indexes are changed conditionally; other people's private users
and records remain intact. Security audit identities are scrubbed. Legacy campaign
dedup/aggregate events without direct identity fields retain their existing expiry
schedules; review any historical schema outside this repository before production.
Existing photos are inline data in Firestore records; no independent Firebase
Storage upload path exists in this source. If deployment added storage buckets or
other datasets, extend and test cleanup before claiming coverage.

All callables other than deletion check the durable marker; personal Firestore
rules deny new writes/reads for that account. Billing verification also checks
inside its transaction so notifications/reconciliation cannot recreate access.
A minimal completed job/uid marker, timestamp and random reference are scheduled
for TTL removal after 30 days. This is an explicit retry/safety retention, not
continued recipe/profile retention. TTL is asynchronous. Provider backups and
already-delivered diagnostics follow provider retention; confirm/disclose those.

## Rollout checklist

1. Use a staging Firebase project/emulators and disposable users/households first.
   Run `node --test firebase/functions/test/*.test.js` and `dotnet test`.
2. Review Firestore indexes: collection-group comments.authorId and
   reports.reporterId, ready-job status/retryAfter composite index, and completed
   job deleteAfter TTL. Wait for indexes to become READY; confirm TTL policy.
3. Deploy the functions codebase, rules and indexes together from the authorized
   Firebase workstation using the repository config, e.g.:
   `firebase deploy --project mealsy-b732b --only functions:households,firestore:rules,firestore:indexes`
   This includes updated callable guards and billing handlers, not only the three
   new functions. Scheduler, Firestore events, IAM and billing must be configured.
   Confirm database/event regions match the actual project (source uses existing
   europe-west1 Firestore triggers and us-central1 callables/scheduler).
4. Test recent/stale credentials, tampered targets, duplicate requests, owner vs
   member, multiple spaces, no spaces, real inline photos, expired leases, failure
   after each cleanup phase, lost HTTP response, account switching, cold restart,
   offline copies and concurrent writes/Play notifications. Verify final Auth,
   Firestore, membership and local cache removal. Local model tests are not proof
   of deployed Firestore transaction/index behaviour.
5. Verify jobs actually reach completed after the second sweep; alert on retry or
   age/backlog. Confirm completed-marker TTL. Do not delete the Auth login alone:
   that does not recursively delete associated Firestore content.
6. Activate the prepared GitHub Pages policy site as described in
   PUBLIC_POLICY_HOSTING.md and verify both URLs anonymously in a browser. App
   URLs already use those planned Pages addresses; set the Play Console URLs
   only after verification. The web page offers an external email request without
   requiring app installation; Callum@brightbark.co.uk must be monitored and
   identity verification exercised. Do not accept a uid emailed by a stranger as
   proof of ownership. Never request passwords/card details by email.

No production deletion, deployment, email transmission or automatic subscription
cancellation was performed during this implementation. The APK offers a request
UI but needs the above Firebase rollout to execute automated cleanup.

Validation: 422 shared tests and 121 Node model tests pass. The real Firestore
integration case is supplied in firebase/emulator.test.js; execution was blocked
by the emulator download's network-policy 403. It must run on the Firebase
workstation before deployment. Native password confirmation, mail fallback,
local-cache lifecycle and background completion remain device/staging checks.
