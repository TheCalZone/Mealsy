using RecipeApp.Models;

namespace RecipeApp.Services;

/// <summary>One storage format shared by the shopping screen and recipe shortcuts.</summary>
public static class ShoppingListStore
{
    public static Func<string> AccountId { get; set; } = () => null;
    public static string StateKey => "ShoppingList.v2" + Suffix;
    public static string LegacyKey => "ShoppingListText" + Suffix;
    public static string IngredientsKey => "IngredientsJson" + Suffix;
    private static string Suffix => string.IsNullOrEmpty(AccountId()) ? "" : ".user." + AccountId();
    public static string ExportKey => StateKey + ".lastExport";

    public static void Save(ShoppingListDocument document)
    {
        Preferences.Set(StateKey, document.Serialize());
        var text = document.PlainText(includeDetails: false);
        Preferences.Set(LegacyKey, text);
        Preferences.Set(ExportKey, text);
    }

    public static void AppendIngredients(IEnumerable<Ingredient> ingredients)
    {
        var saved = Preferences.Get(StateKey, "");
        // Do not silently replace an unreadable stored list when called from another screen.
        var document = string.IsNullOrWhiteSpace(saved) ? new ShoppingListDocument() : ShoppingListDocument.Deserialize(saved);
        var legacy = Preferences.Get(LegacyKey, "");
        if (legacy != Preferences.Get(ExportKey, "") || string.IsNullOrWhiteSpace(saved)) document.MergeLegacyText(legacy);
        document.AppendIngredients(ingredients);
        Save(document);
    }
}
