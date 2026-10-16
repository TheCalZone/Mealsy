# Account, navigation and Firebase recovery

## What changed

Sign-up and sign-in share a proper form with email checks, password visibility,
matching password confirmation, optional profile preferences, password reset, busy
states and readable errors. Creating an Auth account and saving its Firestore profile
have separate outcomes: a profile-write outage no longer reports registration failure
or asks the user to register a duplicate account. Profiles saved locally are retried
through **Account → Sync my account**. Signing in doesn't start a paid subscription.

Firebase's native SDK restores its own session. Mealsy removes the old saved email/
password entries and no longer stores or replays passwords through SecureStorage.
Profile cache/pending changes are account-specific, and auth state comes directly
from Firebase instead of a second mutable user-ID helper.

The Account page includes profile editing, cloud sync status/retry, household and
plan links, working My recipes navigation, and explicit device-recipe copying.
Device recipes remain separate by default. Copying retains originals and skips
already imported recipes; it doesn't silently claim another account's local data.

Home now has personal shopping progress, quick add, recipe/shop/meal shortcuts,
today's planned meals, favourites/recent recipes, community, AI and widget access.
The recipe book has private account filtering, search, exact tag filters, favourites
and sorting by recent/name/time/rating. Unknown times sort after known times.

The recipe reader has serving previews, ingredient/method shortcuts, cooking
checkboxes/progress, keep-awake, visible edit/shopping actions and favourite/copy/
share/delete controls. Previewed servings carry into shopping review; saved recipes
are unchanged. Private notes and source links stay out of copied/shared snapshots.

## Navigation

Android now has five persistent tabs: Home, Recipes, Shopping, Meals and Account.
The shopping creator, food community, AI, widget help and upgrade pages are reachable
from Home and/or their related screens. Detail pages push onto the current tab, so
Back returns to their origin. Root routes aren't also registered as global detail
routes; the old duplicate route registrations were removed.

Recipe details/editing check ownership before displaying cached data. Changing
accounts clears or blocks old editor screens rather than exposing another account's
draft. Legacy saved-checklist edit routes redirect through the new scoped editor.

Tests verify root-route/catalog consistency and account filtering. The Android build
checks page types and XAML handlers. This is not proof that every tap works on a
phone; the device checklist below remains necessary.

## Recipe cloud data

Recipes now keep a stable `cloud_key`. Different devices' SQLite IDs no longer
identify the same recipe. Legacy cloud records retain their existing document key;
new records use independent keys. Downloading allocates a local SQLite ID, replaces
only the matching account/cloud-key row and retains the cloud timestamp. Deletion
markers prevent a later device sync from restoring a deleted recipe. New sync and
delete operations on one client are serialized to avoid stale download races.

Update all devices before relying on stable recipe identities and deletion markers.
Older clients don't understand this metadata. Recipe synchronization still uses
whole-recipe timestamps, not shopping-list field-level conflict merging. Back up
important recipes before simultaneous editing on multiple devices.

## Firebase checks to perform on your desktop

No live Firebase settings, Auth users or production data were changed here. The
public configuration check passed for Android package `com.companyname.recipeapp`
and Firebase project `mealsy-b732b`.

1. Open Firebase Console, select **mealsy-b732b**, then Authentication → Sign-in
   method. Enable **Email/Password**. The app implements email/password, not Google,
   phone or social sign-in. Keep any existing project password policy; Firebase
   enforces it and the UI explains rejected passwords.
2. In Project settings → Your apps, check the Android app's package is exactly
   `com.companyname.recipeapp`. Download its current `google-services.json` if the
   project/app changed and replace `RecipeApp/Platforms/Android/google-services.json`.
   Rebuild the APK after changing mobile configuration.
3. Check that the default Cloud Firestore database exists. Deploy the repository's
   owner-only rules; don't switch to public/test rules to fix a permission error.
   A working Auth login does not automatically mean Firestore rules/database are ready.
4. If the API key has Android application restrictions, include the package and
   SHA-1 certificate of the actual installed build. Visual Studio debug, this cloud
   test key and Play App Signing can have different fingerprints. In Google Cloud
   API restrictions allow the Firebase/Auth APIs required by the project. Don't
   publish service-account keys or put admin credentials in the app.
5. Check Authentication email templates for password reset and any sender/action
   configuration. Reset responses intentionally don't confirm whether an account exists.
6. Deploy the existing functions/rules for household lists, community and billing:

   ```sh
   npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only "functions:households,firestore:rules"
   ```

   Billing still needs Play Console/API/RTDN setup described in MONETIZATION.md;
   a Firebase deployment doesn't create the £3/month or £24/year Play base plans.
7. From the repository run:

   ```sh
   python3 scripts/check-firebase-config.py
   ```

   This prints only public identifiers and checks package/project consistency. It
   doesn't contact Firebase or verify provider enablement, restrictions or deployment.

**Common symptoms:** `OperationNotAllowed` means the sign-in provider needs checking;
invalid API key/app authorization means check Android config and key restrictions;
network errors mean check connectivity; a signed-in account with failed sync means
check the Firestore database/rules and retry Sync my account. Never create another
account just because profile/recipe sync failed after successful registration.

## Phone checklist

- Guest: Home → create recipe → preview → save → recipe book → details → edit → Back.
- Sign up with matching/mismatched passwords; try an existing email; ensure busy
  controls prevent duplicate requests. Check optional preferences and profile editing.
- Sign in with a wrong password, reset the password, and sign in again. Restart the
  app and confirm the native session persists without storing the password in Mealsy.
- Break Firestore access temporarily in a test project: successful authentication
  should remain signed in with an understandable sync/recovery state.
- Guest recipe copy → account collection → repeat copy → confirm no duplicates.
- Switch accounts/sign out; recipe searches, home suggestions, profiles and editors
  must never display another account's cached collection.
- Details from Home and Recipes → edit/save or cancel → Back to the origin; adjust
  servings and check shopping review uses them; test favourite/copy/share/delete.
- Visit community, shopping creator, weekly planner, widget help, household and plan
  pages; verify Back, the five tabs, small screens, keyboard and dark mode.

Automated checks and Android builds ran in the cloud; real Firebase auth, live sync
and a full phone navigation walkthrough were not performed from this environment.
