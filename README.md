# Mealsy

Mealsy is a .NET MAUI recipe book, meal planner, and shopping list app. The current project targets **iOS on .NET 8**; the Android platform source is present but is not enabled in the project's TargetFrameworks.

## Structure

- `Models`: recipes, ingredients, snacks, staples, and meal plan days.
- `Services/RecipeService.cs`: SQLite persistence (`MyData.db3` in app data) and in-memory caches.
- `Services/FirestoreService.cs`: Firebase account-scoped cloud data.
- `ViewModels` and `Views`: MVVM page behavior and MAUI XAML.
- `App.xaml.cs`: startup and attempted cloud synchronization.

## Build and run

Use a Mac with a .NET 8 SDK, a matching MAUI/iOS workload, and a compatible Xcode version. This legacy dependency set has not been verified with current Xcode versions; SDK/workload modernization remains separate work.

```sh
dotnet workload restore RecipeApp/RecipeApp.csproj
dotnet restore RecipeApp/RecipeApp.csproj
dotnet build RecipeApp/RecipeApp.csproj -f net8.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:EnableCodeSigning=false
```

Use `iossimulator-x64` for an Intel Mac. Device builds require your own Apple signing/provisioning setup. Firebase configuration is in `Platforms/iOS/GoogleService-Info.plist`; use a test Firebase project before exercising cloud writes. Do not add server-side Firebase service account credentials to this mobile app.

The AI recipe service reads a user-owned key from SecureStorage under `OpenAIApiKey`. There is currently no settings UI for supplying it. Without configuration, AI generation displays an error and leaves the rest of the app usable. A server-side API integration is the appropriate next step for a distributed app; do not embed a shared key in the client.

## Validation status

The stabilization changes were prepared through the GitHub connector because the development environment failed to provision. **No .NET compilation, automated tests, simulator tests, or Firebase integration tests have been run.** Source checks do not establish that the app builds or works on a device.

See [the regression checklist](docs/stabilization.md) for the required verification and unresolved issues.
