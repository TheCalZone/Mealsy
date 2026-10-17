# Cupboard ingredient recognition

Recipe creation/editing now offers Automatic, Buy each time, and Check cupboard
for every ingredient. Automatic recognises complete ingredient names in an
editable list and existing account Essentials; it also preserves legacy non-main
ingredient behavior. Explicit Buy each time and Check cupboard choices take
precedence. Recognition never claims stock is available: items remain visible in
the shopping planner's cupboard checks and can be ticked when needed.

Manage the recognition list from the recipe editor, shopping planner toolbar, or
Settings > Shopping > Cupboard ingredients. Defaults cover common herbs, spices,
oils, sugar and baking staples, including parsley, herbs de Provence, ancho chilli
flakes and fennel seeds. Matching handles case, accents, preparation prefixes such
as fresh/chopped/freshly ground, and parenthetical notes. It does not use arbitrary
substring matches: fennel bulb, salted butter, sugar snap peas and basil pesto do
not match fennel seeds, salt, sugar or basil. Quantities are preserved, not guessed.

The recognition list is stored in device Preferences separately for each account
and device mode. It is not shared across devices or household members. An empty
list disables built-in recognition; Restore suggestions resets it. Existing
Essentials and explicit recipe choices continue to apply independently.

The per-recipe choice is Ingredient.ShoppingPreference: 0 = automatic (default for
existing rows), 1 = always buy, 2 = check cupboard. SQLite's additive table migration
adds the column without replacing recipes; draft/copy/JSON paths preserve it.
The shopping_preference Firestore mapping includes it in private recipe sync.
Community recipe snapshots do not expose this private shopping preference.

Validation: all 167 .NET tests pass, including the supplied 22-line rigatoni
example (11 cupboard checks, 11 normal shopping items), false-positive protection,
explicit overrides, account-separated list storage, draft round-trips and SQLite
migration. Android builds successfully; native screen interactions need phone
testing. No backend deployment is required for this update.
