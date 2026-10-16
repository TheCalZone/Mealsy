# Mealsy

Mealsy is a .NET 8 MAUI Android app for recipes, meal planning, and shopping lists.

## Android development

Prerequisites: .NET SDK 8, the `maui-android` workload, JDK 17, and Android SDK
platform/build tools 34. The project uses MAUI 8.0.3. Install the workload with
`dotnet workload install maui-android`.

From the repository root, with `ANDROID_HOME` pointing to the Android SDK and
`JAVA_HOME` pointing to JDK 17:

```sh
dotnet restore RecipeApp/RecipeApp.csproj
dotnet build RecipeApp/RecipeApp.csproj -f net8.0-android -c Debug \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
```

The Debug APK embeds its managed assemblies and can be installed directly:

```sh
adb install -r RecipeApp/bin/Debug/net8.0-android/com.companyname.recipeapp-Signed.apk
adb shell am start -a android.intent.action.MAIN \
  -c android.intent.category.LAUNCHER -p com.companyname.recipeapp
```

Use an Android device or a running emulator (Android 5/API 21 or newer).
For startup diagnostics, inspect `adb logcat -b crash -d` and confirm the app
stays open. Exercise recipe creation and shopping-list navigation as a smoke test.
Run the platform-independent SQLite regression tests with:

```sh
dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj
```

These tests exercise the production service and models against a temporary SQLite
database; only the platform data-directory lookup is replaced. They do not test
native UI or Firebase connectivity.

Firebase account/sync features and AI chat require their configured remote
services. A successful local build or offline launch does not validate those
integrations.

AI chat uses your own OpenAI API key. Enter it on the Recipe Creator screen and
choose **Save API key on this device**. The key is kept in secure storage. The
previously hard-coded key should be revoked/rotated because it remains in Git
history. Firebase mobile configuration remains in the project; verify your
Firebase Authentication provider and Firestore rules in your own project.

Debug builds now use the normal Android development signing key. To upgrade an
existing installation, use the same signing key as that installation. Back up
local data before uninstalling an app to resolve a signature mismatch.

## Local smoke test

1. Start without signing in. Create a recipe without a photo, edit it, add and
   remove ingredients, restart, and verify the changes persisted.
2. Add snacks/essentials with and without tags. Create a shopping list, navigate
   away and back, and check that items do not duplicate. Try the append-to-list
   action and the ingredient confirmation option.
3. Put multiple recipes in a meal slot. Restart, reopen the plan, and create its
   shopping list. Clear the plan and verify the cleared state persists.
4. Register/sign in, edit your profile, sign out, and restart. Repeat local recipe
   editing while offline; a failed cloud save should preserve the local recipe.
5. Configure your own AI key, generate a recipe, and save it. Check that it appears
   in the local recipe book. Test invalid-key/network failures without losing the UI.

## Validation in the cloud environment

The Android Debug build and APK signature verification passed. The standalone
APK was installed on an Android 10/API 29 emulator and its activity reached
`RESUMED`, displaying the splash screen. Interactive checks could not complete:
this host has no KVM device, and the software emulator repeatedly encountered
Android System UI timeouts and system-service failures, including with 4 GB RAM.
Recipe editing, shopping-list interactions, and remote services still need a
smoke test on a device or hardware-accelerated emulator.
