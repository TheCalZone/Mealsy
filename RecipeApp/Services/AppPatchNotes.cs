namespace RecipeApp.Services;

public record AppPatchNote(DateTime Date, string Title, IReadOnlyList<string> Changes);

/// <summary>Bundled, newest-first release history. Add an entry when shipping app changes.</summary>
public static class AppPatchNotes
{
    public static IReadOnlyList<AppPatchNote> Entries { get; } = new AppPatchNote[]
    {
        new(new(2026,10,19), "Language support", new[] {
            "Added automatic device-language selection for English, Spanish, French, German and Portuguese, with English fallback.",
            "Choose a language in Settings and restart Mealsy to apply it. Recipes, lists and saved identifiers are kept unchanged.",
            "Translated core navigation, account forms, shopping controls, recipe editing and meal-planning controls. Regional dates, numbers and Play prices follow the device/store settings.",
            "Advanced/admin text, some help and backend errors still fall back to English. Recipe content and ingredient names are not automatically translated."
        }),
        new(new(2026,10,19), "Consistency improvements for testers", new[] {
            "Account and household shortcuts no longer show fixed subscription prices. Monthly and annual prices on the upgrade screen come from Google Play, with a clear unavailable state and refresh action.",
            "Premium descriptions now include shared household meal planning and explain which personal data stays on this device.",
            "Typed meals appear on the home dashboard. Recipe-link importing is also available directly from the recipe book.",
            "Improved household sign-in links, button styling and navigation between Premium plans and features. Social features remain paused."
        }),
        new(new(2026,10,19), "Administrator patch notes", new[] {
            "Added this read-only change history to the administrator panel.",
            "The administrator screen keeps its own title and opens without the email keyboard covering the tools. The complimentary Premium shortcut still focuses the email field."
        }),
        new(new(2026,10,19), "Startup crash fix & Mealsy splash title", new[] {
            "Fixed the test APK packaging mismatch that caused the app to crash immediately after the splash screen.",
            "Added Mealsy beneath the food illustration on the themed splash screen.",
            "Added clean packaging and startup checks to catch the same APK mismatch before sharing future test builds."
        }),
        new(new(2026,10,19), "Easier typing & recipe importing", new[] {
            "Android screens resize above the keyboard. Recipe Preview and Save remain in a fixed footer while editing.",
            "Recipe-link importing includes a Save recipe photo, if available checkbox. Missing or blocked photos do not stop the recipe from importing.",
            "Import a recipe link directly into a personal meal slot or a household meal. Review and save the recipe, then save the meal plan.",
            "Household recipe attachments ask before sharing a snapshot. Recipe-link importing remains a Premium feature."
        }),
        new(new(2026,10,19), "Cooking checklists & clearer options", new[] {
            "Recipe steps suggest ingredient checkboxes, with manual ingredient selection when needed. Each step has its own temporary checklist.",
            "Ingredient amounts follow the selected servings and show recipe totals; follow the method for divided quantities.",
            "Page toolbar actions are grouped under a labelled Options menu.",
            "Android startup and top-bar colours now match Mealsy's green theme."
        }),
        new(new(2026,10,18), "Shared household meals", new[] {
            "Connected household meal planning to its Firebase backend, including shared recipe snapshots and conflict handling.",
            "Enforced the social-feature pause on the server and integrated the iOS preparation work. iOS still requires its own Apple build and device testing."
        }),
        new(new(2026,10,17), "Faster, more flexible shopping lists", new[] {
            "Added offline ingredient suggestions while typing, including the Android quick-add widget.",
            "Added drag handles for shopping items and long-press dragging for sections. Layout order is saved for each list on your device.",
            "Recognise quantities in names such as 500g minced beef; an explicitly entered quantity takes priority.",
            "Improved aisle recognition for meat, herbs, pantry items, frozen foods and common household products.",
            "Paused social, friends and creator screens while keeping household shopping and meal planning available."
        })
    };
}
