# Mealsy Android test APK — be509db

23 October 2026. Includes household member drawers, Owner/Adult/Restricted roles, shopping and meal approval requests, named shopping lists, checked-item settings and prior UI improvements.

Role controls and requests require the updated Firebase backend and staging validation. This build does not deploy that backend. See HOUSEHOLD_ROLES_HANDOFF.md on the source branch. Additional personal lists remain device-local.

427 shared tests and 130 backend tests passed on this source. Signature, ZIP integrity, 4 KB alignment and JNI startup references verified. Same development signer as previous cloud APKs. No device runtime or live Firebase review performed here.

Development-signed private test APK, not a Play production release. Known target SDK/16 KB compatibility and Firebase rollout blockers remain; see artifact-audit.json and source production handoff.
