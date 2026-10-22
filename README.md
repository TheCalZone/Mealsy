# Mealsy privacy and account-deletion test APK — ddb74ff

[Download the signed APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-ddb74ff/Mealsy-ddb74ff-test.apk)

Source: TheCalZone/Mealsy, codex/android-runtime-fixes, ddb74ff.

Settings/Account → Privacy, data & account deletion now includes automated account
deletion with password confirmation, review of owned shared spaces, literal DELETE
confirmation, subscription warning and a server request receipt. Server cleanup
uses a durable retryable job, a later settling sweep and final Auth deletion.
Local account caches clear only after acceptance; other private accounts are kept.
The confirmed email fallback is Callum@brightbark.co.uk.

IMPORTANT: this APK DOES NOT deploy the new Firebase functions, rules, indexes,
TTL or public pages. Automated deletion requires the rollout in ACCOUNT_DELETION.md
and end-to-end staging verification first. Do not treat accepted as completed.
Deleting an account does not cancel Google Play subscriptions. Other devices can
retain offline copies; clear app data there as appropriate.

Optional diagnostics now defaults off without a saved choice; Android backup and
AD_ID permission are disabled in source. Updated privacy/deletion HTML is supplied
for publisher review and deployment. The actual public policy URL remains unset
until a deployed notice is verified. Live ads and social features remain disabled.

**NOT PLAY PRODUCTION READY:** API 34 and legacy native 16 KB compatibility remain
release blockers. See PLAY_POLICY_AUDIT.md and PLAY_DATA_SAFETY.md. The attached
artifact-audit.json records actual manifest/native inspection; it is not a policy
approval. Google policy pages and Firestore emulator download were network-blocked.

422 shared tests and 121 backend model tests pass. Android release rebuilt cleanly;
signature, startup metadata, archive, alignment and download checksum verified.
No real account was deleted and no Firebase production change was deployed.
Native device UI and live backend/emulator integration remain unverified.
Same cloud test signer as previous APKs; install as an update on a compatible device.
