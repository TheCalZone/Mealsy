# Mealsy Android test APK — 35f70ac

23 October 2026. Clear selected-tab highlight and bold label; setup completion clears its source tab; admin reviewer-account creation without changing the current login. Includes prior household roles/requests UI.

Reviewer creation uses existing Firebase sign-up and admin grant services and requests verification by email. You must open the inbox link to confirm ownership. Manual admin email confirmation requires the updated administration backend; roles/requests also require their backend rollout. No live accounts, grants, emails or deployments were changed by the cloud tests. See REVIEWER_ACCOUNT_HANDOFF.md and HOUSEHOLD_ROLES_HANDOFF.md on source branches.

432 shared tests, 133 backend tests and Android Debug build passed. Clean APK, JNI startup references for all four ABIs, signature, ZIP integrity and 4 KB alignment verified. Same development signer as previous cloud APKs; no phone runtime test was performed.

Private test build, not a Play production release. Existing target SDK/16 KB native compatibility and deployment blockers remain; see artifact-audit.json.
