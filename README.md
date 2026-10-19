# Mealsy cooking-step ingredients test — 93cf316

[Download signed APK](Mealsy-93cf316-test.apk).

Source: `codex/android-runtime-fixes`, commit `93cf316`.

Recipe cooking steps show suggested ingredient checklists based on local text
matching. Each step's addition ticks are independent from preparation and shopping
checks, so repeated uses can be checked again. Add/remove step ingredients manually
using Choose ingredients when a match is missing or ambiguous. These adjustments
and checks are temporary and do not change the saved recipe.

Amounts are whole-recipe totals, not per-step allocations. Follow the actual step
for half/divided ingredients. Structured ingredient quantities follow the reader's
serving adjustment. Shared recipe readers and editor previews use the same feature.
No AI calls, backend changes or Firebase deployment. Social remains paused.

Android 6 or later; same cloud test signing key as prior downloads. Allow browser
installation if prompted. Preserve local data if a different signing key prevents
updating a locally built app. Play purchases require a Play-installed test build.

326 app tests and Android build passed. APK signature/alignment/download checked.
Test step matching, manual additions/removals, independent checkmarks and serving
changes on a phone. The APK also retains the Options menu, themed startup, shopping
item/section drag handles, quantity recognition and ingredient autocomplete.
