# Recipe editor upgrade

Add and Edit now share a sectioned Android editor with live completion progress,
validation, preview, and account-specific local draft recovery. Drafts save after a
short pause and on leaving the page; they are device-local, not shared across devices.
Discard explicitly removes a draft. Editing does not mutate the saved recipe until
Save, and checks for changes made since editing began.

Ingredients support named sections, reordering, categories, notes and key-ingredient
flags. Paste one ingredient per line: `200 g flour`, `1½ tbsp oil` or
`2 x 400 g tomatoes`. Use `[Sauce]` or `Sauce:` for section headings. Review parsed
quantities before saving. Method steps can be pasted, added, removed and reordered.

New fields include an introduction, separate cook time, private notes and a private
source link. Existing recipes remain readable; SQLite adds the new columns without
replacing existing data. Total time is prep plus cook time. Recipe details and preview
include temporary cooking checkboxes and an optional keep-screen-awake switch.

Owner cloud sync includes all fields. Public snapshots include introductions, cook
time, ingredient sections and key flags; private recipe notes/source links are excluded
from public snapshots and copied text. Ingredient notes remain public when shared.
Saved community recipes and shopping shortcuts retain the ingredient information.
Desktop Studio displays the new public metadata and supports older snapshots.

## Deployment and local verification

Deploy the updated food-feed function to retain new snapshot metadata on publication:

```sh
npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only functions:households:foodFeed
```

Deploy Hosting as well if using the desktop Studio. No live Firebase deployment was
performed in this workspace. Existing clients and old feed snapshots are supported.

On an Android device, check:

1. Create a recipe with ingredient sections, fractional quantities, a photo and steps.
2. Leave and reopen Add to recover the draft; preview and then save.
3. Edit, reorder sections/steps, and discard; confirm the saved recipe is unchanged.
4. Save edits and restart the app; confirm times, notes, sections and photo persist.
5. Try invalid/empty fields, photo cancellation, dark mode, keyboard and small screens.
6. Use cooking checkboxes/keep-awake, then leave and confirm normal screen behaviour.
7. Add ingredients to shopping and publish/import a community snapshot; confirm
   public metadata is retained and private notes/source links are absent.

Automated checks exercise parsing, validation, detached copies, draft isolation and
recovery, old SQLite migration, metadata persistence, public snapshot privacy and
backend compatibility. Cloud builds do not replace Android device UI testing.
