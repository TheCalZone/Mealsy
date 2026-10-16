# Mealsy Android test build

Standalone signed APK built from app commit `260d6b9`.

[Download the APK](https://github.com/TheCalZone/Mealsy/raw/refs/heads/android-test-260d6b9/Mealsy-260d6b9-test.apk)

Android 6 or newer; supports ARM64, ARM32, x86 and x86_64. Size approximately 67 MiB.

Open the downloaded file on your phone and allow your browser or Files app to install unknown apps when Android prompts. This uses an Android test signing key, not a production publishing key. A differently signed installed build cannot be updated in place; back up local data before uninstalling an old build.

The Android Release build, embedded assemblies, APK signature and widget resources were verified. Device interaction and live Firebase/Google Play services were not tested here. Google Play purchase testing requires an internal-track Play installation and the setup documented on the source branch.

This branch only distributes the requested test APK. App source remains on `codex/android-runtime-fixes`. The adjacent SHA256 file can be used to verify the download.
