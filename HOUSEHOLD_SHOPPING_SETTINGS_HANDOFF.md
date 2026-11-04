# Household shopping settings — 27 October 2026

Account → My household → Shopping permissions lets the owner switch approval for restricted members' shopping additions on/off. Existing households (including missing fields) keep approval ON; the owner must explicitly turn it off. Adults remain unrestricted. With approval off, restricted members can add uncollected items directly with trusted Added by labels and check items. They still cannot edit/delete items or clear lists. New meal requests retain their existing approval rules. Existing pending shopping requests are not automatically applied or deleted by changing the switch.

An owner's member drawer includes Change shopping name colour. Eight named, dark colours are supported on white badges for contrast in either appearance; Automatic restores the deterministic member colour. Owners can set colours for every current member, including themselves. Names remain account profile names. Server responses supply current colours for existing items, requests and conversation members; colours are not copied into trusted stored item authorship or accepted from item edits. Refreshing other clients applies updated settings/colours; no live listener is introduced. Older apps retain their old presentation but the server applies the current approval rule.

## Backend rollout — not deployed here

**27 October desktop rollout:** `household`, `householdQuestions` and
`processAccountDeletion` were deployed together to `mealsy-b732b`. The deployment
metadata check passed: required functions ACTIVE, indexes READY, TTL ACTIVE and
deletion recovery enabled. All 148 backend tests passed. Phone acceptance below
remains outstanding; no real account was removed or household setting changed
as part of this rollout. See `PLAY_RELEASE_2026_10_27.md` for the Android build.

Deploy together from the repository root using your existing authenticated local Firebase workflow:

```
firebase deploy --only functions:households:household,functions:households:householdQuestions,functions:households:processAccountDeletion
```

`household` supports owner-only setShoppingApproval (boolean required) and setMemberColor (current member, palette colour or null). Both run after current owner authorization and transaction membership checks. Status advertises shoppingSettingsVersion=1, shoppingApprovalRequired and member colour values. Updated clients disable settings against an older backend. Shopping read/patch responses include the approval flag so active clients update their captions on sync. Colours/settings use the existing household document: shoppingApprovalRequired and memberColors. No new collection, index or rules change. Normal list reads gain no new document reads; settings writes occur only when changed.

Leaving/removal deletes that member's colour alongside role metadata. Account deletion also clears member colours. Deploy the deletion worker with the household updates so colour-map identifiers follow the same cleanup. Shared badge colour changes do not create item mutations or override conflict detection.

## Validation and phone checks

Automated tests cover owner-only settings, invalid values/recipients, legacy approval defaults, switching back to approvals, pending-request preservation, trusted direct-add attribution, restricted edit/delete denial, colour reset/removal and the client palette fallback. Check the switch saving/failure and refreshed state on a phone; use a second restricted account to test both modes, existing requests, recipe/widget additions and member colour changes. Meal additions should still request approval. Test older clients against the new server, and verify removal/account-deletion cleanup. Android compilation/shared tests are checked here; no live Firebase, physical-phone or Mac/iOS deployment validation is claimed.
