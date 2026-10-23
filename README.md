# Mealsy

Mealsy is a .NET 10 MAUI app for recipes, meal planning, and shopping lists.

For the latest implementation, deployed state, test commands and remaining store/iOS work, start with the [cloud handoff](CLOUD_HANDOFF.md).

## Plans and feature inventory

The free tier keeps personal cooking/planning. Social and creator features are paused. **Mealsy
Premium (£5.99/month or £49.99/year in the UK)** adds recipe-link imports, saved weekly templates and reusable shopping trips, plus household sharing and ad removal for the
owner and their household. Live billing/ads require publisher setup; see
[monetization setup](MONETIZATION.md) and the [complete current features](FEATURES.md).

## Android development

Prerequisites: .NET SDK 10.0.402 (see global.json), the `maui-android` workload,
JDK 21, and Android SDK platform/build tools 36. The project uses MAUI 10.0.120.
Android minimum SDK is 24 and target SDK is 36. Install the workload with
`dotnet workload install maui-android`.

From the repository root, with `ANDROID_HOME` pointing to the Android SDK and
`JAVA_HOME` pointing to JDK 21:

```sh
dotnet restore RecipeApp/RecipeApp.csproj
dotnet build RecipeApp/RecipeApp.csproj -f net10.0-android -c Debug \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
```

The bundle-derived Release test APK can be installed directly (ARM64 phones):

```sh
adb install -r artifacts/android/Mealsy-v2-api36-min24-phone-test.apk
adb shell am start -a android.intent.action.MAIN \
  -c android.intent.category.LAUNCHER -p com.companyname.recipeapp
```

Use an ARM64 Android device (Android 7/API 24 or newer). This APK uses the upload
signer; an older development-signed install cannot be updated in place. Preserve
device-only data before considering uninstalling an older build.
For startup diagnostics, inspect `adb logcat -b crash -d` and confirm the app
stays open. Exercise recipe creation and shopping-list navigation as a smoke test.
Run the platform-independent SQLite regression tests with:

```sh
dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj
```

These tests exercise the production service and models against a temporary SQLite
database; only the platform data-directory lookup is replaced. They do not test
native UI or Firebase connectivity.

Firebase account/sync features require their configured remote services. A
successful local build or offline launch does not validate those integrations.

Use what you have replaces the AI recipe creator with a free offline ingredient
search of the current account’s saved recipes. No API key is needed. Historical
AI keys should still be revoked because earlier versions remain in Git history.
Firebase mobile configuration remains in the project; verify your Firebase
Authentication provider and Firestore rules in your own project.

Debug builds now use the normal Android development signing key. To upgrade an
existing installation, use the same signing key as that installation. Back up
local data before uninstalling an app to resolve a signature mismatch.

## Local smoke test

1. Start without signing in. Create a recipe without a photo, edit it, add and
   remove ingredients, restart, and verify the changes persisted.
2. Add snacks/essentials with and without tags. Create a shopping list, navigate
   away and back, and check that items do not duplicate. Try the append-to-list
   action and the ingredient confirmation option.
3. Put multiple recipes in a meal slot. Restart, reopen the plan, and create its
   shopping list. Clear the plan and verify the cleared state persists.
4. Register/sign in, edit your profile, sign out, and restart. Repeat local recipe
   editing while offline; a failed cloud save should preserve the local recipe.
5. Configure your own AI key, generate a recipe, and save it. Check that it appears
   in the local recipe book. Test invalid-key/network failures without losing the UI.

## Shopping list

The shopping screen now has a progress summary, quick-add fields, optional
quantities and notes, category groups, search, and a **Hide collected** switch.
Tap the checkbox or item name to collect an item. Collected names are crossed out.
Use **Edit** for the item's name, quantity, category and note; swipe left for
Edit/Remove actions. Removal is also available inside the editor.

Changes save automatically on this device, including collected states. **Undo**
and **Redo** cover the last 30 changes while the screen is open. **Clear done**
removes collected items even when search is active; clearing the entire list asks
for confirmation. Search and hiding items never change the saved list.

The menu's **Paste list** appends clipboard lines without replacing the current
list; `[Category]` headings, bullet points, and `[x]` checkboxes are recognized.
**Share** lets you send or copy either the full list or only the items still needed.
Recipe shortcuts append ingredients without resetting existing items. Creating a
new list from the recipe selector or meal planner replaces the personal list and can
be undone immediately. Older text-based lists migrate when opened.

On your device, check the following after pulling this update:

- Add an item with a quantity/note, collect it, restart, and confirm all details remain.
- Try search and Hide collected, then undo a removal and Clear done.
- Edit a category and verify regrouping. Cancel an edit and confirm it changes nothing.
- Add ingredients from a recipe while a partly collected list exists.
- Generate a fresh recipe list, undo it, then redo it.
- Paste a multiline list, copy/share the remaining items, and test light/dark mode.

Android compilation and automated list/state/store tests are verified in the
cloud. Native layout, keyboard, clipboard and share-sheet behavior still require
device testing; the software emulator is not used for this update.

## Food community

The **Food feed** tab is a community for signed-in Mealsy users: publish captions,
food photos and recipe snapshots, like posts, comment, report content, and save shared
recipes into your own book. A recipe's **Share to food feed** menu opens a composer;
publishing always asks for confirmation. Private recipes and household lists stay
private until you explicitly share a recipe copy.

A desktop **Community Studio** is included for publishing and moderation. Deploy the
`foodFeed` Firebase function, configure the studio's public Firebase web-app settings,
and deploy Hosting using the [food feed setup guide](firebase/FEED.md). Pulling the
Android code alone does not deploy these services.

## Household sharing

Family members now use their own logins, with a private personal shopping list and
one shared household list. Use **Account → Your household** to create/join a
household, invite family, or manage members. The selector at the top of the
shopping screen switches lists. Signed-in lists sync while that screen is open;
device saves keep offline changes for retry.

**A one-time Firebase Functions and Firestore rules deployment is required.**
Pulling the Android code alone does not activate household sharing. Follow the
[Firebase setup, invitation and testing guide](firebase/README.md).

## Validation in the cloud environment

The Android Debug build and APK signature verification passed. The standalone
APK was installed on an Android 10/API 29 emulator and its activity reached
`RESUMED`, displaying the splash screen. Interactive checks could not complete:
this host has no KVM device, and the software emulator repeatedly encountered
Android System UI timeouts and system-service failures, including with 4 GB RAM.
Recipe editing, shopping-list interactions, and remote services still need a
smoke test on a device or hardware-accelerated emulator.
