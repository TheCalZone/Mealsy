# Social features paused

The current Android release focuses on personal recipes, quick shopping, cupboard
checks/essentials, household shopping and meal planning. Social and creator code
is retained for possible future use, with no user-facing switch to turn it on.

`RecipeApp/Services/ProductFeatures.cs` sets `SocialEnabled` to false. It hides Home,
Recipes, recipe-toolbar and Account community/friends/creator controls and public
username editing. Account stops fetching the community profile while paused.
Admin keeps complimentary Premium grants and audit tools, while social moderation
and business verification entry points are hidden. Pricing and billing are unchanged.

Navigation also blocks social/creator page names, removes the FoodFeed shell route,
ignores incoming post/creator-plan links and social notification taps, and suppresses
social pushes. Existing opted-in device registration is disabled on app resume;
notifications are ignored locally even if unregistering cannot reach the server.
Saved social drafts, pending links, recipes, plans and remote data are retained.
Household meal pages inherit shared UI helpers but remain explicitly available;
they still require the backend described in the existing Firebase handoff.

The website release switch is `firebase/desktop/product-features.js`. While false,
the desktop community studio redirects to the shopping-focused `/share/` landing
page without loading its application. Old social/creator links on that page offer
only generic app onboarding. App-download, app-sharing and household invitation
pages remain available. Existing public app assets/APIs are not an access-control
boundary: this is a client release pause, not deletion or shutdown of Firebase.
Older installed builds and previously deployed websites are not changed until
users update or the new Hosting files are deployed. No backend/data changes or
production deployments are part of this pause.

## Restoring later

1. Only resume social work when requested. Review the unfinished social/privacy and
   shared-planner backend contracts in `FIREBASE_HANDOFF.md` before enabling anything.
2. Change the Android `SocialEnabled` property and web `socialEnabled` flag together.
   Rebuild/release Android and deploy Hosting. There is no Remote Config or settings
   override. Review shopping-focused onboarding, pricing/feature copy and landing
   text before advertising restored features.
3. Recheck every navigation path, private audience enforcement, moderation and
   creator permissions. Do not restore social notifications without renewed opt-in.
4. Run app tests and Android build, then the browser suites. The desktop studio
   browser fixture deliberately enables the retained studio for regression tests;
   `firebase/sharing.test.js` checks the real disabled release configuration.
   Update pause-specific expectations when intentionally restoring the feature.

Current checks: 269 app tests and 14 browser tests pass; Android build succeeds.
Native navigation and notification behavior still need phone verification.
