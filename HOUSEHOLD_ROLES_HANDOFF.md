# Household roles and requests — desktop Firebase rollout

23 October 2026. Source implementation only: **not deployed by the cloud work**.
Do not rely on restricted permissions until the updated backend is deployed and
staging acceptance passes. Existing members and legacy invitations default to
Adult; no existing member is silently demoted. Owner is derived from the actual
household.owner field, never the client or role map. The UI exposes role controls
only when household status advertises permissionsVersion >= 1.

## Behaviour

- Owner: current membership/invitation/subscription controls plus role assignment.
- Adult: shared shopping/meal edits and request approval/decline. Cannot change
  roles, invite/remove members or delete the household.
- Restricted: read shared data, check/uncheck existing shopping rows, request
  shopping additions/new meals, and cancel own pending requests. Cannot edit or
  delete shared items/meals or approve requests. Personal lists stay unrestricted.
- The owner chooses Adult or Restricted when generating an invitation. The role
  is stored with the single-use invitation, not accepted from join input.
- Role assignment is in each member drawer. Requests are reachable from Household,
  Shopping, household meal plans and the Home household drawer. Badges reuse status
  reads; there are no push notifications/listeners. Refresh for latest decisions.
- Shopping patch additions from restricted members become requests inside the same
  transaction, including widget/older-client calls. Only check-state changes can
  directly affect existing items. Old clients trying auto-delete/edits are denied.
- New restricted meals become requests. An adult can also explicitly suggest a
  meal. Approval atomically writes the original requested item or meal exactly
  once; decline/cancel never changes shopping/plans. Meal approval does not buy
  ingredients automatically. Recipe snapshots use the existing privacy sanitizer.
- New clients display server-confirmed request receipts. Offline additions are
  labelled pending sync, not confirmed requests. A stale/offline edit after role
  demotion is rejected; a device recovery copy is kept for review/reload rather
  than silently replacing shared data.

## Backend and data

Changes are in application.js, household-meals.js and household-requests.js.
Existing callable endpoints/codebase are reused, with the existing authenticated
and account-deletion guards. Firestore rules remain deny-by-default for shared
records: direct client SDK writes cannot bypass the callables. No new index,
collection or scheduled task is required for requests.

households/{id} contains roles, requests and requestRates. Requests carry a
server-derived author, stable hashed request ID, kind, payload, status and decision
metadata. Decisions recheck live membership, role and owner entitlement in the
transaction. Retries and concurrent decisions cannot add an item/meal twice.

Bounded storage: 50 pending requests per household, at most 100 pending/recent
records total, 600 KB aggregate request payload and 100 new requests per author
per UTC day. Older terminal records are pruned for count/size; full pending queues
reject further additions without partial writes. History is recent, not a permanent
archive. Leaving/removal discards authored requests and role/quota metadata and
scrubs the departing decision actor. Account deletion removes corresponding
request metadata; deleting an owned household removes the entire tree.

## Rollout

1. Fetch codex/android-runtime-fixes (or shared iOS source on
   codex/ios-client-support). Inspect the diff against your local Firebase work;
   merge rather than replacing your deployed/custom backend wholesale.
2. With Node 22, from repo root:
   `npm ci --prefix firebase/functions`
   `node --test firebase/functions/test/*.test.js`
3. From firebase/functions:
   `npx firebase emulators:exec --config ../../firebase.json --project demo-mealsy --only firestore "npm run test:emulator"`
   This includes a real transaction/rules case for restricted additions, approval,
   direct SDK denial and promotion. Cloud emulator downloads were previously
   blocked; this suite must pass locally. Model tests are not deployment proof.
4. Use a separate staging project and disposable owner/adult/restricted accounts.
   From repo root deploy the reviewed functions codebase:
   `firebase deploy --project STAGING_PROJECT_ID --only functions:households`
   Confirm existing shared-data rules are still deployed and private. No permissive
   rule changes are needed. Deploy updated functions before enabling restricted
   roles in released clients. Deploy the reviewed code to mealsy-b732b only after
   the staging checks below pass. No production deployment/deletion occurred here.
5. Test with the updated APK and an older client. Attempt forged roles/author IDs,
   stale credentials/membership and direct writes. Test all checkbox, quick-add,
   paste, widget and recipe destinations; personal-list writes must stay personal.
   Test role changes during in-flight/offline edits, request retries, lost replies,
   simultaneous approval/decline, full queues, daily limits, recipe snapshots,
   filled meal weeks, entitlement expiry, leave/remove and account deletion.
6. Verify pending items do NOT enter the server list/plan, approve once and confirm
   all devices can see the result. Decline/cancel must leave shared content intact.
   Check that role demotion disables auto-delete for checks in new clients, and
   old clients cannot bypass enforcement. Resolve rejected offline edits using the
   recovery flow. Confirm decision-cache invalidation on the acting client and
   refresh behaviour on other devices. Re-run Android/iOS native tests as relevant.

This role is an access restriction, not age verification or a child-specific
account system. If the product will intentionally serve children, review the
actual age/audience, parental consent, ads and privacy requirements before launch.
Update public disclosures/Data safety to match the deployed request data. This
change adds no messaging/social feed or notification delivery service.
