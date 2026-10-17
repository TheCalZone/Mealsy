# Mealsy Android account/loading fix

[Download signed APK](Mealsy-4f4fa4c-test.apk). Source: `codex/android-runtime-fixes`, commit `4f4fa4c`.

Fixes the inline Account sign-in view remaining stale until you leave the tab. Adds native progress indicators for authentication, account hydration and main loading screens, and working captions for slower actions. Keeps stale account/page requests from updating the current account view.

Android 6 or later. Standalone sideload test build, signed with the same cloud test key as the previous cloud downloads. Allow installation from your browser if prompted. A differently signed desktop build cannot be updated by this APK; keep its local data rather than uninstalling without backup.

217 app tests and the Android build passed. APK signature and ZIP alignment verified. Check sign-in without leaving Account, incorrect credentials, offline/slow loads, sign-out and account switching on your phone. Google Play purchases require a Play-installed test build. No server deployment or Premium grant was made.
