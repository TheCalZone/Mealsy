# Shopping lists — 23 October 2026

The existing main personal shopping list retains its account-specific storage and
Firebase sync. One active household list retains the existing membership checks,
cloud sync and conflict recovery. When entering Shopping normally, verified
active household access takes priority. Explicit main-personal, named-personal,
household widget links and recipe import destinations override the default.

Additional named personal lists are device-local, isolated by account (or guest),
and labelled accordingly. Names live in ShoppingListStore.StateKey + `.lists`;
additional documents use `.list.{guid}`. This is not a new Firebase schema or a
claim of extra cloud lists. Existing local account cleanup removes uid-scoped
preferences. Uninstall/device loss removes local lists; use Share for a copy.

Free includes two personal lists, counting the existing main list. Additional
creation checks Premium; downgrade never hides, deletes or locks existing lists.
Household lists do not count towards the personal limit. Main may be renamed but
cannot be deleted. Deleting an extra list asks for irreversible confirmation.
Shared list management remains in Household. Existing shortcuts that directly
append ingredients still target the main personal list; the recipe shopping-page
import flow and recipe/checklist shopping creator offer a choice when multiple personal lists exist. A widget can
explicitly target any named personal list; deleting it makes that widget require
reconfiguration rather than silently recreating the list. Widget list labels are
saved snapshots of their configured names; reconfigure after renaming if needed.

Keep PlanPage's Premium benefits accurate. If cloud sync for all named lists is
added later, implement a server-authorized list catalog, per-list IDs, membership
and migration without mixing the main document with additional device lists.
Android widget in-place additions for device lists deliberately skip the cloud
personal endpoint. iOS shared UI is included; native iOS build/device validation
still requires a Mac. No Firebase deployment is needed for these local additions.

## Destination selection across shortcuts

Recipe/meal-plan shopping review, Home quick-add, saved-trip saving/reuse and guest-list import now choose an exact destination. Household choices are available to entitled members; restricted members' additions remain requests. Named personal lists are labelled as device-only. Cancelling never defaults to the main list. Replacement offers personal lists only, then confirms the chosen name. Legacy ingredient routes ask unless an explicit destination was already supplied, and append rather than replace.

ShoppingListDestinations provides the chooser and shared-list preparation; ShoppingDestinationStore checks the account/catalog and reads/writes the exact target. Shared additions load/rebase the chosen list before combining quantities, then open it for the existing sync path. Failed access checks do not redirect ingredients into a personal list. Widget targets remain explicit and do not ask again for each quick add. Additional personal lists remain device-only; this change does not introduce cloud sync for them.

## Shopping controls — 24 October

List tabs are always visible below the selected list's name. The animated quick drawer holds search, hide checked, Clear checked, Go shopping and the List tools shortcut. Less-used actions have grouped sections on List tools: list management/share/paste, checked behaviour, organisation/reuse, undo/redo, household actions, cloud/recovery and clear entire list. Duplicate toolbar/dropdown actions are removed. Returning from a child screen keeps the chosen list; initial/account-change household defaults still apply.

Clear checked uses the existing ClearCollectedCommand and existing patch deletions, with Undo and unchanged restricted-member permissions. It does not require a Firebase deployment. Version code 5 reserves the next Android build after the submitted v4 bundle. The Android build and model/sync tests are checked here; physical UI verification remains necessary.
