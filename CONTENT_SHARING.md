# Mealsy content sharing

Android source branch: `codex/android-runtime-fixes`.

Creators: Business & creators → workspace → create/edit a collection and optional
weekly plan → publish. Use **Share published meal plan / collection** in the
workspace or share from its public preview/profile. Drafts have no public share
button and cannot be read through a public collection link.

Recipients: open the link, sign in if necessary, select meals and choose days,
meal slots and servings. **Save recipes & shop selected meals** saves private
copies and opens the existing shopping creator, including cupboard and extras
review. It doesn't add meals to the planner or immediately alter shopping lists.
**Add selected meals to my plan** is a separate action and keeps existing meals.
Each recipe is saved once per account using its existing `business-copy-` key;
later imports preserve private edits. Repeated meal occurrences contribute all
selected portions to shopping. A shop is limited to 1,000 combined portions per
recipe, matching the shopping creator's portion limit. Unselected meals are omitted.

Posts: **Share post / Share link** opens the Android share sheet. WhatsApp,
Facebook, messages and other installed apps can receive the same link; platform
support determines how it is displayed. Link text and previews contain generic
Mealsy information, never protected captions or photos. Sharing a friends-only or
Only me link does not change the audience or grant access. Screenshots or copies
that users share separately cannot be recalled.

## Deployment and store setup

1. Deploy the changed `foodFeed` function after backend tests. Its new `getPost`
   action returns only visible legacy public posts to signed-in callers. Existing
   business collection APIs serve creator plan shares. Friends/private post links
   use the separate `social.getPost` contract in `FIREBASE_HANDOFF.md`, still to be
   implemented; they never fall back to legacy public reads.
2. Deploy the existing Firebase Hosting configuration with the new files under
   `firebase/desktop/share/`. The mobile landing page is `/share/`; desktop studio
   stays at `/` and invitations stay at `/join/`. No SPA rewrite is needed.
   `/share/?kind=plan&id=...` and `/share/?kind=post&id=...` retain targets.
3. In Google Play Console obtain **App integrity → App signing key certificate →
   SHA-256**. Serve Digital Asset Links JSON at `/.well-known/assetlinks.json`:

   ```json
   [{
     "relation": ["delegate_permission/common.handle_all_urls"],
     "target": {
       "namespace": "android_app",
       "package_name": "com.companyname.recipeapp",
       "sha256_cert_fingerprints": ["REPLACE_WITH_PLAY_APP_SIGNING_SHA256"]
     }
   }]
   ```

   This example is documentation only, not an active association. Use the actual
   package ID if it changes. Include the certificate of a locally signed test APK
   only if you intend those test builds to handle production links too.
   Add the actual JSON file to Hosting only after replacing the example value.
   Hosting currently ignores `**/.*`; update that ignore pattern for this file
   (for example restrict ignored dotfiles to other paths), and verify the file
   actually serves JSON with HTTP 200 on **both** `mealsy-b732b.web.app` and
   `mealsy-b732b.firebaseapp.com`. No HTML/redirect/login should be returned.
4. Publish Google Play or invite testers through the correct opt-in URL. Current
   page and `InvitationLinks.PlayStore` use the existing application package.
   Update both if you change it. A store listing link alone doesn't enrol closed
   testers. There is no iOS App Store link yet.

The explicit **Open Mealsy** button uses a custom scheme and works without domain
verification when the app is installed. Android can open HTTPS links directly
once verified. Other platforms' in-app browsers may need “Open in browser” first.
No automatic navigation timer or APK download is used. After first installation,
return to the page/reopen the link; automatic deferred deep linking isn't provided.
No Firebase Dynamic Links dependency is introduced (that service is retired).

## Verification

- `dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj`
- Android build with the existing MAUI/.NET 8/JDK 17/SDK 34 setup.
- `node --test firebase/functions/test/*.test.js`
- `node --test firebase/sharing.test.js` (installed Chromium + playwright-core).

On a phone, test cold and warm starts for each of `post`, `social` and `plan`:

```sh
adb shell am start -W -a android.intent.action.VIEW -d 'mealsy://content?kind=plan&id=YOUR_COLLECTION_ID' com.companyname.recipeapp
adb shell pm verify-app-links --re-verify com.companyname.recipeapp
adb shell pm get-app-links com.companyname.recipeapp
```

Open a real HTTPS share from a messaging app after checking domain verification.
Test signed-out → sign-in/create account → original destination, unpublished plans,
removed recipes, deleted/hidden posts and friends/Only me shares from an account
without access. Check “Send to Mealsy” via Android share, portion changes, repeated
recipes, unselected meals and cupboard review without replacing an existing list.
On a device without Mealsy, confirm the download/return instructions, then install
and reopen the original link. Native link dispatch and real Firebase integration
require device testing after deployment; Linux browser/build checks don't cover them.
