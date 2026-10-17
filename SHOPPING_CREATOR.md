# Shopping creator and reusable checklists

The creator now follows three steps:

1. **Choose meals.** Search by recipe name, filter by tag, choose servings per batch,
   and set how many batches to cook. You can also build a trip without recipes.
2. **Check the cupboard.** Non-main ingredients, common herbs/spices/oils, and recipe
   ingredients named in your saved essentials are offered for checking. Tick only
   what needs buying. Browse/search your essentials and snacks and choose this trip's
   extras. No reusable item is selected automatically for a new plan.
3. **Review.** Check every proposed item, adjust quantities or shopping notes, then
   add to the personal list or explicitly confirm replacement of the personal list.
   Going back preserves review decisions and edits for unchanged source amounts.

Recipe details, community recipe shopping shortcuts and meal-plan shopping all open
this flow. An explicit personal-list route prevents a previously open household list
from hiding the newly generated personal list. The shopping screen also links to the
creator. Creating a list resets this trip's selections; the reusable library remains.
Selections are kept while this creator instance is open, not backed up across restarts.

## Snacks and essentials

Reusable items have a name, usual purchase quantity, category and note. Add/edit uses
a detached form with validation, cancel and confirmed deletion. Renaming preserves
its cloud identity. Library items are private to their account; signed-out device
items remain separate. Existing tables are migrated without replacing their rows.

Use **Remember as an essential** on a recipe ingredient to save it for future trips.
Usual purchase quantities are independent of recipe requirements: set `1 jar` for
oregano even if recipes use teaspoons. If that essential is selected for a trip, the
usual purchase amount replaces its recipe amounts once, and the source text shows
what the recipes need. Without a usual amount, the recipe quantities are retained.
Simply saving an essential doesn't select it for this trip.

Library cloud sync preserves quantities, notes and stable document identities rather
than matching SQLite IDs from different devices. Newer local edits survive older
cloud copies. Deletes use private cloud markers so an older device can remove its
cached copy on its next library sync. Signed-in deletion requires the cloud write to
succeed before removing the local row. Failed saves keep a local copy for later sync.
Library refresh uses the existing app/account sync flow; it is not a live shared
household inventory. Concurrent library edits use timestamps, not shopping-list
field conflict resolution. Update all devices before relying on new library metadata
and deletion markers; older clients don't understand them.

## Quantities and formatting

- `500 g + 0.5 kg` → `1 kg`; `750 ml + 1 litre` → `1.75 L`.
- Decimal, ASCII mixed fractions and common Unicode fractions are accepted.
- Compatible repeated packs combine: `2 x 400 g + 1 x 400 g` → `3 × 400 g`.
  Packs and loose weights remain separate.
- Cups, teaspoons and tablespoons combine only with the same unit. No density,
  regional cup/spoon conversion, or singular/plural ingredient-name matching is guessed.
- Different shopping categories or preparation notes remain separate. Existing
  uncollected compatible rows merge, retaining their IDs; collected purchases stay
  unchanged and new needs get uncollected rows.
- Ranges, ambiguous comma/thousands notation and free text stay visible for review.
  Missing amounts remain blank instead of inventing ingredient counts. Scaled free
  text is shown explicitly, e.g. `2 × (a handful)`.
- Numeric totals display up to three decimal places; smaller positive amounts retain
  enough digits to avoid becoming zero. Review quantities before buying.
- Copy/paste supports the app's `Rice — 500 g (Brown)` format, category headings and
  compatible quantity merging. The paste update participates in shopping-list undo.
- Newly generated AI recipes now store parsed names and quantities separately.
  Existing names with quantities embedded in them are kept as entered.

Meal-plan shopping uses total planned servings across slots. A four-serving recipe
used unchanged in three slots contributes twelve servings, while individual portion
adjustments contribute their actual planned amounts. The creator opens with those
totals and lets you review quantities afterward. Repeated legacy IDs within a slot
are counted once.

## Validation and device checks

Run `dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj`. Tests cover conversions,
fractions, packs, uncertain quantities, scaling, source tracking, cupboard defaults,
purchase overrides, old-table migration, account isolation, metadata sync, deletion
markers, collected-item preservation and undo.

Build the Android target using the repository's existing setup. No Firebase functions
or rule deployment is needed specifically for this change; library data uses the
existing private user collections. Live Firebase synchronization was not exercised
from this cloud environment. Existing Firebase setup is still required for cloud use.

On an Android device check small screens, dark mode, keyboard/scroll behaviour and:

- Select/filter recipes and change servings/batches; navigate back through the flow.
- Check herbs, add snacks and essentials, save a usual amount, rename and cancel edits.
- Review and change amounts/notes, then append; confirm collected items are unchanged.
- Repeat with explicit replacement; confirm household lists are unchanged.
- Shop a repeated meal plan and a community recipe through the same review.
- Restart/sign in on a second device and sync library metadata, renames and deletion.
