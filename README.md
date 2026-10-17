# Mealsy Android test build

[Download the signed test APK](Mealsy-909d8df-test.apk).

Source: `TheCalZone/Mealsy`, branch `codex/android-runtime-fixes`, commit `909d8df`.
Android 6 or later is required. This is a standalone sideload test build signed
with the cloud Android test certificate; it is not a Google Play production build.

Includes Premium reference prices and solo tools, saved weekly templates and
shopping trips, sharing/onboarding and complimentary Premium administrator tools,
along with the previous household, creator and social features.

Allow installation from your browser when Android prompts. An app signed with a
different desktop/test key cannot be updated by this APK. Do not uninstall your
existing app without backing up local-only recipes, plans, templates and drafts;
use a separate device/profile or rebuild with the original key if needed.

Google Play purchases need a Play-installed test release. This APK does not change
store prices, grant Premium, deploy Hosting or enable paid business promotions.
See the source branch's `ONBOARDING_AND_INVITES.md` and `CLOUD_HANDOFF.md`.

The APK signature and ZIP alignment are verified before publication. All 213
shared app tests passed. Real phone interactions still require device testing.
Signing keys and application source are not included in this download branch.
