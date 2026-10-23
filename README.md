# Mealsy test APK — b24e731

Source: codex/android-runtime-fixes, b24e731. Includes owner-only household naming, compact shopping drawers and animated home sections.

432 shared tests and 135 backend tests passed; Android Debug build passed. Release APK uses the existing development signer, with startup JNI metadata, ZIP integrity, APK signature and alignment checks. Not tested on a physical phone here.

Household rename requires deploying the updated household function. See HOUSEHOLD_RENAME_HANDOFF.md in the source branch. This test APK is not a Play production release; existing target SDK and native 16 KB compatibility limitations remain.
