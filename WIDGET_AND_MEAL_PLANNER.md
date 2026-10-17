# Android widget, subscription options and meal planner

## Home-screen shopping widget

Open Shopping list → menu → **Home-screen quick add**. Supported Android launchers
can pin the widget from that helper. Otherwise long-press an empty home-screen area,
choose Widgets, find **Mealsy quick add**, and drag it onto the home screen.

Each widget independently remembers a list. **Choose list** opens the configuration
and item-entry screen; select the personal/device list, or **Find my household list**,
then remember the selection. **+ Add item** opens the focused item/quantity/note form.
Android home-screen RemoteViews do not host an editable MAUI form; entry is in the
app's dedicated quick-add screen. No shopping items or household names are shown on
the widget itself. Removed widgets clear their configuration, and launcher widget
restoration remaps saved settings to the replacement widget IDs.

Personal quick add is free and saves offline. A household target requires current
membership and active Family access, verified online before adding. An unavailable
household never silently falls back to a personal list. Switching accounts requires
explicitly selecting a target for the new account. Writes use stable item IDs so
storage retries don't duplicate additions or reset collected items.

Signed-in writes attempt foreground synchronization. If sync fails after the local
save, the item is kept in the selected list's device cache, with a retry message.
Open that list to retry or resolve conflicts. No background network service or
notification permission is required. Android launcher placement, app restore, cold
start and keyboard behaviour still require real-device testing.

## Monthly and annual Family

Monthly **£3/month** and annual **£24/year** are two auto-renewing base plans of the
same `mealsy_family` Google Play subscription (`monthly` and `annual`). Both cover
the owner and their household with identical shared-shopping and ad-free benefits.
Google Play supplies the actual local price and confirmation. Existing subscribers
use Google Play management; the app doesn't launch a second active subscription.

The Account page and upgrade screen link to **What's included**, explaining all
Family benefits, included free tools, household limits, private personal data, restore,
cancellation, and separately billed OpenAI usage. See [monetization setup](MONETIZATION.md)
for Play Console configuration and the required Firebase billing/RTDN/reconciliation
deployment. These live setup steps were not performed from the cloud workspace.

## Meal planner

- Dated seven-day overview with progress, day shortcuts and a reusable week date.
- Search/select multiple recipes per meal; set portions for each recipe in each slot.
- Add, edit and remove notes for leftovers, eating out or flexible meals.
- Detached day editing with Save and discard confirmation through toolbar/hardware back.
- Copy a day to another day with replacement confirmation; clear a day or the week.
  Week-wide changes run in one SQLite transaction.
- Copy the weekly plan as text, including notes and planned servings.
- Shopping review combines total planned servings, then opens the existing cupboard,
  snack/essential and quantity review flow. Missing recipes require replacement/removal.
- Each signed-in account gets its own local plan. The previous unscoped plan remains
  the device plan; **Copy previous device plan…** explicitly copies it into an account.
  Plans are local, not cloud-backed or editable by household members.

Changing the week date keeps the meals for reuse; it doesn't create a history of
multiple weeks. Save day edits before leaving or restarting the app. Switching tabs
preserves the open page's draft, but drafts aren't persisted across process restarts.
Recipes unavailable to the current account show as unavailable rather than exposing
their names. Planned portions survive reload; removed recipes and notes serialize as
empty arrays so older entries cannot reappear after saving.

## Verification

Automated tests cover widget target/account isolation, selected-cache writes, retry
IDs, corrupted-storage preservation, in-flight sync merging, personal plan isolation,
legacy IDs, portions, cleared meals, detached copies and transactional week rollback.
Backend tests exercise both billing periods and household/ad-free inheritance.

On an Android device/internal Play test track, check:

1. Place, resize, remove and restore two widgets with different targets.
2. Add items from cold/warm starts, offline personal mode and an authorized household.
3. Sign out/switch accounts, expire household access and retry a failed sync.
4. Set recipes and portions, edit/remove notes, discard edits, copy/clear days and restart.
5. Shop a week containing the same recipe with different portions across meals.
6. Test both Play base plans, restore, cancellation, pending payment and household ads.

No emulator home-screen interaction, live Firebase call or real Google Play purchase
was completed here. Android build/signature and automated tests cover code integration;
real-device and service configuration checks are still required.
