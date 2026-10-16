using RecipeApp.Models;

namespace RecipeApp.Services;

/// <summary>One storage format shared by the shopping screen and recipe shortcuts.</summary>
public static class ShoppingListStore
{
    public const string StateKey = "ShoppingList.v2";
    public const string ExportKey = "ShoppingList.v2.lastExport";

    public static void Save(ShoppingListDocument document)
    {
        Preferences.Set(StateKey, document.Serialize());
        var text = document.PlainText(includeDetails: false);
        Preferences.Set("ShoppingListText", text);
        Preferences.Set(ExportKey, text);
    }

    public static void AppendIngredients(IEnumerable<Ingredient> ingredients)
    {
        var saved = Preferences.Get(StateKey, "");
        // Do not silently replace an unreadable stored list when called from another screen.
        var document = string.IsNullOrWhiteSpace(saved) ? new ShoppingListDocument() : ShoppingListDocument.Deserialize(saved);
        var legacy = Preferences.Get("ShoppingListText", "");
        if (legacy != Preferences.Get(ExportKey, "") || string.IsNullOrWhiteSpace(saved)) document.MergeLegacyText(legacy);
        foreach (var item in ShoppingListDocument.FromIngredients(ingredients)) document.Add(item);
        Save(document);
    }
}
