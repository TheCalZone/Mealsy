# Firebase rollout — 23 October 2026

Applied the Firebase handoffs from `codex/android-runtime-fixes` through `872007d`
to the existing `mealsy-b732b` project. Older source-only/not-deployed statements
in the handoff documents describe their cloud implementation session, not this rollout.

## Deployed scope

- Household Adult/Restricted roles, invitation roles, bounded shopping/meal requests,
  atomic approval and current-permission enforcement, including older-client writes.
- Owner household renaming and trusted Added by / Requested by shopping attribution.
- Household item questions/replies, household-only inbox and opt-in Android alerts.
  Social feed, friends, creator tools and promotions remain paused.
- Administrator email confirmation with current Auth authorization and audit intent.
  No account was manually verified during rollout.
- Account-deletion callable, leased cleanup worker, five-minute recovery scheduler,
  callable/billing deletion guards, personal-data rules and cleanup anonymization.
  No real account deletion was requested or exercised.
- Required deletion-queue and household-inbox composite indexes, comment/report
  collection-group indexes, and completed-deletion marker TTL configuration.
- Updated Firebase Hosting privacy/deletion pages. The app's separately hosted
  GitHub Pages policy URLs were also verified publicly accessible.

All 16 functions deployed successfully using Node 22 runtime. Firestore is the
existing Standard/native `(default)` database in eur3; event handlers remain
in europe-west1 and callable/scheduler functions in us-central1.

## Verification and operational check

- 145 backend model tests, 442 shared .NET tests and 14 browser tests passed.
- Six targeted real Firestore emulator cases passed: conversations/alerts,
  shared meal conflicts, direct-access rules, administration, deletion and roles.
- Two full emulator runs each had one intermittent `Transaction is invalid or
  closed` failure in a different existing paused social concurrency test. Each
  of those cases passed in the other run; the full suite is not recorded as green.
- Eleven read-only production HTTP checks passed, including unsigned rejection
  on both new callable endpoints and the public website social-pause flag.
- Messaging APIs and runtime permissions verified; no notification was sent.
- Recovery scheduler enabled at `every 5 minutes`.

Use this read-only metadata check after deployment; nonzero means a required
function/index/TTL policy/scheduler is missing, still building or unavailable:

```powershell
node firebase/functions/check-household-deployment.js mealsy-b732b
node --test firebase/live.test.js
```

The metadata check prints no account data, tokens, device registrations or receipts.
Monitor deletion worker/recovery errors and queue age in the project console;
this command is an on-demand check, not an installed monitoring alert.

## Remaining acceptance

A separate deployed staging project, real Firebase Auth/Scheduler deletion lifecycle,
two-phone notification/role/offline flows and native iOS tests remain unverified.
Disposable-account deletion tests must precede relying on deletion in a public
release. Emulator tests use real Firestore transactions/rules but simulated Auth.
TTL cleanup is asynchronous and is not a promise of immediate record removal.

Play offer configuration, store billing tests and supported Android/iOS toolchain
migrations are separate release work. No store configuration, subscription price,
public social feature or paid campaign was enabled by this rollout.
