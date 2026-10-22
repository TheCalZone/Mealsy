# Mealsy Premium promotions and eligible trials — 4b2bdff

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-4b2bdff/Mealsy-4b2bdff-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, 4b2bdff.

Includes Premium benefits cards on Home/Account, contextual sign-up and Premium
prompts on shopping/meal-plan screens, seven-day prompt snoozing, and account
creation actions when a guest tries a protected feature. Basic personal tools
remain free. Includes the earlier household-first loading and scroll fixes.

Subscription options now prefer the configured Google Play seven-day trial when
Play returns it as eligible. The screen shows the paid renewal price, period and
cancellation terms. Checkout uses that exact store offer and rejects changed
terms until refreshed. No local trial entitlement or card collection is added.
Ineligible users see standard pricing; unavailable store queries cannot promise
a trial. Extra-place plans are unaffected.

IMPORTANT: configure/activate offer ID seven-day-trial on the monthly/annual
mealsy_family base plans in Play Console as documented in FIREBASE_HANDOFF.md.
Actual trial eligibility and checkout must be tested with licence testers on an
internal-testing release installed via Google Play. This sideloaded APK is for
promotion/navigation testing; it does not establish a working Play purchase.
No Play Console/Firebase production changes were deployed by this work.

Android 6+. Same cloud test signing key as prior APKs; install as an update.
421 shared tests pass. Release packaging rebuilt cleanly; native startup metadata,
signature, alignment, archive integrity and actual download checksum verified.
Native phone UI and live store/backend interactions still need device testing.
