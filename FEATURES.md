# Current Mealsy features

**Current release:** Social, friends, creator tools and post/creator-plan sharing are
paused and inaccessible through the app UI. Their code remains below as an inventory
of retained work. Shopping, private recipes, essentials and household tools remain
available. See [social pause](SOCIAL_PAUSE.md).

This inventory describes implemented code, not confirmation that every remote service
has been deployed. Firebase, Google Play, AdMob and OpenAI require the configuration
noted below. The active app target is Android; iOS/Windows are not active build targets.

See [recipe editor details](RECIPE_EDITOR.md), [shopping creator behaviour](SHOPPING_CREATOR.md),
and [widget, plans and meal planner checks](WIDGET_AND_MEAL_PLANNER.md) for setup and device verification.

See [account/navigation changes and Firebase recovery](FIREBASE_RECOVERY_AND_NAVIGATION.md)
for the latest sign-in, dashboard and recipe-screen upgrade.

## Friends and household meals — frontend ready, backend required

- Search people, view/edit profiles and bios, manage friend requests, remove friends,
  block/unblock accounts and report profiles or posts.
- Friends-only feed, profile posts, likes, comments/replies and saving recipe copies.
- Choose Everyone, Friends only or Only me when posting; drafts retain privacy.
  Protected publishing requires the new privacy service and never falls back to public.
- Shared household week with breakfast/lunch/dinner, servings, notes, recipe snapshots,
  editing and conflict review. Personal plans remain separate. Family entitlement applies.
- Activity can open friend profiles and privacy-managed conversations.

These screens require the new Firebase endpoints; they are not deployed by this change.
See [Firebase implementation handoff](FIREBASE_HANDOFF.md) for contracts, permissions,
privacy migration and rollout tests. Existing public feed services remain in use.

## Creator plan and social sharing

- Share published creator collections/weekly plans or community posts through
  Android's share sheet (available social/messaging apps).
- Open exact posts or plans in Mealsy; sign-in retains the shared destination.
  Private links preserve the author's access restrictions.
- Save selected creator recipes and shop selected meals directly, combining repeated
  recipe portions before cupboard review. Importing to a personal week is optional.
- Mobile download/open-app landing page and generic social preview artwork.

Code is ready; Hosting, `foodFeed.getPost` deployment, Play availability and Android
App Links certificate association are required. Private links also depend on the
new social backend. See [content sharing setup](CONTENT_SHARING.md).

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
- Search names/descriptions/tags, filter exact tags/favourites and sort by recent,
  name, rating or time. Preview serving quantities, track cooking-step progress,
  jump to ingredients/method, and shop the selected portions. Copy as text, use the recipe's
  favourite/star control, and send ingredients to the personal shopping list.
- Store recipes locally using SQLite. Signed-in accounts can synchronize recipes,
  photos/tags and essentials/snacks with the configured Firebase backend.
- Explicitly share an independent recipe snapshot to the food community.

**Personal shopping list**
- Offline ingredient autocomplete in shopping quick-add and the widget entry screen.
  Type two or more characters for up to five suggestions from common ingredients,
  your account's local recipes/checklists, current list and usage history. Selecting
  a suggestion fills the name, keeps explicit quantities and suggests a section;
  it doesn't add the item until you tap Add. No AI or Firebase calls are used.
- Each item has a drag handle: long-press and drag above/below another item in the
  same section. Checkboxes and item taps keep working; swipe to edit/change section.
  Order saves per list/account on this device, persists through sync and filters,
  and is used in copy/share exports.
- New uncategorized items are placed automatically using local ingredient rules:
  `500g minced beef` → Meat; fresh produce, dairy, pantry, frozen, drinks, bakery,
  household/pet products and chilled alternatives are recognized. Assigned categories
  are kept; existing saved lists are not silently reclassified.
- Recognize leading quantities such as `500 g beef` in quick-add, pasted items,
  widget additions and recipe shopping. A filled quantity field takes precedence;
  recipe amounts are extracted before serving adjustments. Saved recipe names
  stay unchanged.
- Long-press shopping section headers and drag them above/below another section,
  or use Shopping → Reorder sections (also in shopping tools). Drag custom categories
  too; optional movement buttons are available in the reorder menu. Restore default
  alphabetical order with Other last. The layout saves per list/account on this
  device, including household lists, without changing another member's layout.
  Search, collected filters, sync and new shops retain the layout; copy/share uses
  the selected order too.
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

**Android home-screen widget**
- Add a Mealsy quick-add widget from the launcher or the shopping screen's widget helper.
- Each widget remembers its personal/device or household list selection.
- Opens a focused item/quantity/note form. Personal saves work offline; household
  writes verify active access online. Pending sync is retained locally.
- Account changes require a new explicit selection; no shopping items appear on the
  home screen. Personal use is free, household use follows Family access.

**Shopping-list creator, essentials and snacks**
- Three-step flow: choose meals, check the cupboard and extras, review the list.
- Search/tag filters, servings per batch and repeated-batch controls.
- Non-main ingredients, common herbs/spices/oils and saved essentials become cupboard
  checks. Snacks and essentials are reusable checklists, selected only when needed.
- Save recipe ingredients as essentials; manage usual purchase quantities, categories
  and notes with safe editing, cancellation and confirmed deletion.
- Compatible metric quantities, fractions and repeated packs consolidate; uncertain
  quantities and differing notes remain visible for review. No guessed density conversions.
- Review/edit amounts and notes before appending or confirming personal-list replacement.
  Compatible uncollected rows merge; existing collected purchases retain their state.
- Recipe details, community recipes and meal-plan imports use the same review flow.
- Private library sync preserves metadata and stable identities across renames/devices.
- See [shopping creator behaviour and device checks](SHOPPING_CREATOR.md).

**Meal planning**
- Dated weekly overview, day shortcuts, progress and a reusable week-start date.
- Search/select multiple recipes per meal, adjust planned portions, add/edit/remove
  notes for leftovers or eating out, and save a detached day with discard protection.
- Copy days, clear a day or the full week with confirmation, and copy the weekly plan.
- Shopping review uses total planned portions across all meal slots.
- Plans are local and account-specific; the previous device plan stays separate and
  can be explicitly copied into an account. Shared household meal-plan editing is
  not implemented.

**AI recipe creator**
- Generate a general recipe or dessert suggestion with metric measurements.
- Include the signed-in profile's dietary restrictions and favourite cuisines.
- Review the generated ingredients/method and save the result to the recipe book.
- Uses the user's own OpenAI API key, saved in device secure storage. OpenAI usage
  is separate from the £49.99/year plan and is not included as a paid AI allowance.

**Account**
- Redesigned email/password registration/sign-in, password visibility/confirmation,
  neutral password-reset responses, native Firebase session persistence and sign-out.
- Account-specific profile cache, pending profile writes, sync status/retry and explicit
  non-duplicating copies of device recipes into a signed-in account.
- Profile first/last name, dietary restrictions and favourite cuisines; profile editing
  keeps the account email read-only.
- Subscription status, restore purchases, Google Play manage/cancel link, monthly/annual
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

## Mealsy Premium including Family — £5.99/month or £49.99/year in the UK

- Monthly and annual options unlock the same benefits; Google Play supplies local
  prices. One owner subscription covers four people; extra places cost £0.99/month each at the UK reference price, up to 20 total.
- An upgrade features page explains Family benefits, free tools and separate AI costs.
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
- Subscriptions need a real Play Console product with annual/monthly base plans, internal-track testing,
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

### Themes and device settings

Open **Settings** from the Home or Account toolbar, including in device mode. Choose Forest, Ocean, Berry, Terracotta or Slate; follow the system display mode or force light/dark. Colours update immediately across navigation, shopping, recipe, planner, household and community screens. Larger reading text and optional recipe photos help tailor readability.

Set shopping defaults for hiding collected items and expanding quick-add details, choose the recipe book's default order, and enable keeping the screen awake when opening recipes. Cooking screens release their screen-awake setting when closed. Preferences save automatically on this device and survive restart; they are not synced to other household members. Resetting preferences restores appearance and defaults without touching recipes, lists, drafts, accounts or meal plans. Native Android widgets retain their separate system styling and list selection.

### Pasting from Notes and other recipe apps

Recipe ingredients, pasted method steps and shopping-list paste clean common Unicode checklist symbols, Markdown checkboxes (`[ ]` / `[x]`), bullets, numbered-list prefixes, square brackets, bold markers and invisible clipboard spacing. Ordinary brackets, accented food names, fractions, pack sizes and temperature symbols remain. Copy one item per line from Notes, a message or a recipe and use Mealsy's paste action. Shopping checks are imported as collected/uncollected; recipe checks are discarded. Common recipe headings such as `[Sauce]` and exported shopping categories remain supported; use a trailing colon for custom section headings. Bracket-wrapped food names such as `[Milk]` are kept as items. Cleanup applies to pasted text and does not rewrite existing saved recipes or lists.

### Firebase social backend and hosting

The existing authenticated food feed backend supports posts, bounded photos, recipe snapshots, likes, comments, reports and moderator controls. Hosting can discover its public web app configuration automatically in the same Firebase project; manual public config remains available for other static hosts. Hosting applies a Content Security Policy and browser security headers. Feed visibility/likes use a consistent read-only database snapshot, reports have account-level throttling and safe retries, and combined post sizes and recipe servings are validated before writing. A documented combined deploy script and backend-test predeploy hook are included. Live Firebase activation still requires desktop authentication, a registered web app, the existing Firestore database and Blaze billing; see [firebase/FEED.md](firebase/FEED.md#deploying-the-current-version).

### Premium for solo cooks and households

The same Premium plan includes supported recipe-link importing, saved weekly meal-plan templates and explicitly saved shopping trips. Find templates in Meals or Account, and saved trips in Shopping or Account. Templates preserve recipes, portions, typed meals and notes; reuse confirms replacing the current week, keeps the date and refuses missing recipes. Shopping trip reuse resets checks and adds fresh item IDs with quantities/notes to the existing personal list, combining compatible amounts. Each library holds up to 100 named entries privately on this device for the signed-in account. Libraries are retained after expiry; saving/reusing needs verified paid access, with the existing short offline entitlement grace. Basic manual recipes, ingredient/method paste, personal shopping, cooking and weekly planning remain free. Existing recipes imported before upgrading remain usable.

### Invitations, onboarding and complimentary Premium

Share Mealsy from Home, Account or Settings. Household invitations include an Android download/get-started link and a single-use code. The hosted join page copies codes and opens the installed Android app; new installs may require pasting the code manually. The optional four-step Get started guide covers free/Premium, accounts, joining and first actions, and remains reopenable. Administrators have a direct complimentary-Premium shortcut with account lookup, 30/90/365-day or custom grants, confirmation, reasons and revocation using existing audited server permissions. Hosting deployment and a published/eligible Google Play test build are still required for public downloads. See [setup and phone checks](ONBOARDING_AND_INVITES.md).
