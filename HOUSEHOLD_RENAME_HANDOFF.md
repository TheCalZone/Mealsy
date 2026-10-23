# Household list naming

Owners: Account → Household → Household membership → Rename household list.
The household name labels shared shopping and meal plans; renaming leaves IDs, items, membership, roles and invitations unchanged. Names are trimmed and limited to 1–80 characters. Renaming also works while sharing is paused.

Deploy the updated `household` callable from `firebase/functions/application.js` using the existing Firebase project and deployment workflow (`firebase deploy --only functions:households:household` from the repository root). No rules change, migration or new index is required. This environment has not deployed it.

Status advertises `canRenameHousehold`; older servers show an update-needed message instead of attempting an unsupported mutation. The server enforces owner permissions independently of the UI. Client read/warmup caches are invalidated after rename; other devices fetch the name on their normal refresh. An already-open screen on another device does not receive a name push.

Staging: rename as owner, reopen shared shopping and meal plans, confirm items are unchanged, confirm another member sees the name after refreshing, and verify adult/restricted accounts cannot rename. Confirm a paused household owner can rename. Existing widget labels may require reselecting their list to refresh the saved display name.
