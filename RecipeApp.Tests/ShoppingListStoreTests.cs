using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
using Xunit;

public class ShoppingListStoreTests
{
    public ShoppingListStoreTests() => Preferences.Clear();

    [Fact]
    public void RecipeShortcutsPreserveExistingCollectedItemsAndTheirDetails()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Milk", Category = "Dairy", Quantity = "2 L", Note = "Oat", InBasket = true });
        ShoppingListStore.Save(list);
        ShoppingListStore.AppendIngredients(new[] { new Ingredient { Name = "Rice", Quantity = "500 g", Tag = "Pantry", Note = "Brown" } });
        var restored = ShoppingListDocument.Deserialize(Preferences.Get(ShoppingListStore.StateKey, ""));
        var milk = Assert.Single(restored.Items, i => i.Name == "Milk");
        Assert.True(milk.InBasket);
        Assert.Equal("2 L", milk.Quantity);
        Assert.Equal("Oat", milk.Note);
        var rice = Assert.Single(restored.Items, i => i.Name == "Rice");
        Assert.Equal("500 g", rice.Quantity);
        Assert.Equal("Brown", rice.Note);
    }

    [Fact]
    public void OldTextListsMigrateBeforeNewRecipeItemsAreAdded()
    {
        Preferences.Set("ShoppingListText", "[Veg]\nApples\n[Pantry]\nRice");
        ShoppingListStore.AppendIngredients(new[] { new Ingredient { Name = "Milk", Tag = "Dairy" } });
        var restored = ShoppingListDocument.Deserialize(Preferences.Get(ShoppingListStore.StateKey, ""));
        Assert.Equal(3, restored.Items.Count);
        Assert.Equal("Produce", Assert.Single(restored.Items, i => i.Name == "Apples").Category);
        Assert.Equal(Preferences.Get("ShoppingListText", ""), Preferences.Get(ShoppingListStore.ExportKey, "different"));
    }

    [Fact]
    public void AnUnreadableSavedListCannotBeOverwrittenByARecipeShortcut()
    {
        Preferences.Set(ShoppingListStore.StateKey, "unreadable saved list");
        Assert.Throws<JsonException>(() => ShoppingListStore.AppendIngredients(new[] { new Ingredient { Name = "Milk" } }));
        Assert.Equal("unreadable saved list", Preferences.Get(ShoppingListStore.StateKey, ""));
    }

    [Fact]
    public void ClearedListsDoNotResurrectOldGeneratedIngredients()
    {
        Preferences.Set("IngredientsJson", "[{\"Name\":\"Old item\"}]");
        ShoppingListStore.Save(new ShoppingListDocument());
        ShoppingListStore.AppendIngredients(new[] { new Ingredient { Name = "New item" } });
        var restored = ShoppingListDocument.Deserialize(Preferences.Get(ShoppingListStore.StateKey, ""));
        Assert.Equal("New item", Assert.Single(restored.Items).Name);
    }
}
