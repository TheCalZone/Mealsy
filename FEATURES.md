# Current Mealsy features

This inventory describes implemented code, not confirmation that every remote service
has been deployed. Firebase, Google Play, AdMob and OpenAI require the configuration
noted below. The active app target is Android; iOS/Windows are not active build targets.

See [recipe editor details and device checks](RECIPE_EDITOR.md) for the latest UI upgrade.

## Free personal features

**Recipe book**
- Create, view, edit and delete recipes using one shared editor, with a photo,
  introduction, servings, separate prep/cook times, rating and tags.
- Preview before saving; local account-specific drafts recover after navigation.
  Editing uses a detached copy and detects changes made to the saved recipe.
- Reorder ingredient sections and method steps; paste ingredient lists with quantities,
  fractions and section headings, or paste method steps.
- Private cooking notes and a source link stay out of community snapshots and copied
  recipe text. Photo selection resizes images and removes EXIF metadata.
- Cooking view has temporary ingredient/step checkboxes and an optional keep-awake
  switch. Total times appear in recipe-book and meal-planning cards.
- Organize ingredients into sections/groups; enter quantities, categories and notes.
  Add individual ingredients or use bulk text entry.
- Search recipes by name and filter by tags. Copy a recipe as text, use the recipe's
  favourite/star control, and send ingredients to the personal shopping list.
- Store recipes locally using SQLite. Signed-in accounts can synchronize recipes,
  photos/tags and essentials/snacks with the configured Firebase backend.
- Explicitly share an independent recipe snapshot to the food community.

**Personal shopping list**
- Quick-add an item, quantity, category and optional note; edit or remove items.
- Category groups, collected checkboxes, crossed-out collected names, progress and
  remaining-item counts, search and Hide collected.
- Swipe actions, clear collected items, confirmed clear-all, and 30-step undo/redo
  while the screen is open. Remote replacements reset undo history safely.
- Paste multiline lists with category headings/bullets/checkboxes. Copy/share the
  complete list or only the items still needed.
- Automatic device saves, signed-in personal cloud sync, separate lists per account,
  offline pending changes and explicit conflict/recovery handling.
- Keep the original signed-out device list and explicitly copy it into a signed-in
  account when desired. Recipe shortcuts preserve existing item details/checkmarks.

**Shopping-list creator, essentials and snacks**
- Select recipes, household essentials/staples and snacks to build a shopping list.
- Search/select/deselect entries, review ingredients, and consolidate repeated
  ingredients. Create a fresh personal list or append selected ingredients.
- Maintain reusable essentials and snacks with quantities/categories/details.

**Meal planning**
- Add/manage plan days; assign multiple recipes to breakfast, lunch and dinner.
- Record meal notes/extras, save/reopen the plan, and clear it when needed.
- Turn the plan's recipes into a personal shopping list.
- Plans are personal/local; shared household meal-plan editing is not implemented.

**AI recipe creator**
- Generate a general recipe or dessert suggestion with metric measurements.
- Include the signed-in profile's dietary restrictions and favourite cuisines.
- Review the generated ingredients/method and save the result to the recipe book.
- Uses the user's own OpenAI API key, saved in device secure storage. OpenAI usage
  is separate from the £24/year plan and is not included as a paid AI allowance.

**Account**
- Firebase email/password registration, sign-in/out and password-reset requests.
- Profile first/last name, dietary restrictions and favourite cuisines; profile editing
  keeps the account email read-only.
- Subscription status, restore purchases, Google Play manage/cancel link, annual
  price lookup, advertising privacy choices and publisher privacy-policy link.

## Food community (free for signed-in users)

- Newest-first paged feed visible to all signed-in Mealsy users.
- Publish a caption, small food photo and/or recipe snapshot under a public display
  name. Photos selected through the clients are resized/re-encoded to remove EXIF.
- Per-account local drafts, explicit publication confirmation, and publication retry
  protection. Private household lists and unshared recipes never populate the feed.
- Like/unlike, comment and remove your own content; report posts for review.
- View shared recipes, save an independent private copy, or add their ingredients
  to your personal shopping list. Shared snapshots retain ingredient sections,
  key-ingredient flags, introductions and prep/cook times.
- A desktop Community Studio supports publishing, browsing, likes, comments and
  moderator access through Firebase Hosting.
- Trusted moderators can inspect reports, hide/restore posts, delete posts/comments
  and mark reports reviewed. Moderator rights are server-issued custom claims.
- No follows, direct messages, push notifications or recommendation algorithm yet.

## Mealsy Family — £24/year in the UK

- One owner subscription covers the whole household, up to 20 members.
- Create a household, invite members using random single-use codes valid for seven
  days, revoke invitations, remove members, leave or delete a household.
- Every person uses their own login and retains their own personal list.
- All members can add, edit, remove and collect items on one shared shopping list.
- Foreground sync approximately every five seconds, manual sync, offline pending
  edits and conflict detection/recovery. Different item/field changes merge safely.
- Ad-free use for the subscriber and members of their paid household.
- Restore from Google Play; renewal/cancellation/refund state comes from server-side
  Play verification. Expiry pauses sharing/invitations while keeping the data.
- No shared recipe-book or shared meal-plan permissions are implied by this plan.

## Advertising

- Banner placements on Home and Food feed; no interstitial/rewarded placements.
- Consent/age controls through Google's UMP, non-personalized requests and ad privacy
  review. No request until the SDK permits it.
- Banners stop on page departure, consent changes or a verified paid upgrade.
- Official test ads in Debug. Live Release ads require publisher configuration.

## Setup-dependent and unfinished areas

- Household/cloud lists/community need deployed Firebase functions and reviewed
  Firestore rules; the desktop studio also needs Firebase web configuration/Hosting.
- Subscriptions need a real Play Console annual product, internal-track testing,
  Android Publisher permissions, RTDN topic and deployed billing/reconciliation.
- Ad revenue needs your real AdMob IDs, payment/consent setup and privacy policy.
  These are deliberately not fabricated or activated in source control.
- The app still needs a current supported toolchain/target-API update before a public
  Play submission. Android 6+ is now the minimum for the integrated billing/ads SDKs.
- The URL recipe-import method is unfinished and does not populate/save an imported
  recipe. It is not a working general website importer. Some legacy Account buttons
  such as My Recipes remain placeholders; the Recipes tab is the working book.
- SQLite/state tests and Android compilation do not establish native layout, gallery,
  clipboard, live payments/ads or remote integrations work. Those need device tests.

Setup guides: [Android and app](README.md), [households](firebase/README.md),
[food community](firebase/FEED.md), [monetization](MONETIZATION.md).
