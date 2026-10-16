using RecipeApp.Models;
using System.Text.Json;
using Xunit;

public class FoodFeedTests
{
    [Fact]
    public void PublicSnapshotOmitsPrivateRecipeMetadataAndCopiesIngredients()
    {
        var privateRecipe = new Recipe { Id = 42, FirebaseUID = "private-owner", FirestoreID = "private-doc", RecipeName = "Pasta", RecipeMethod = "Cook", Serving = 2, PrepTime = 20,
            Ingredients = new() { new Ingredient { Id = 17, RecipeId = 42, Name = "Pasta", Quantity = "200 g", InBasket = true, Note = "Wholegrain" } } };
        var shared = FeedRecipe.FromRecipe(privateRecipe);
        var json = JsonSerializer.Serialize(shared);
        Assert.DoesNotContain("private-owner", json); Assert.DoesNotContain("private-doc", json); Assert.DoesNotContain("InBasket", json);
        privateRecipe.Ingredients[0].Name = "Changed later";
        Assert.Equal("Pasta", shared.Ingredients[0].Name);
    }
    [Fact]
    public void SavingSharedRecipeCreatesIndependentLocalRecords()
    {
        var snapshot = new FeedRecipe("Pasta", "Cook", 2, 20, new() { new("Pasta", "200 g", "Pantry", "Wholegrain") });
        var first = snapshot.ToRecipe("photo");
        var second = snapshot.ToRecipe("photo");
        Assert.Equal(0, first.Id); Assert.Null(first.FirebaseUID); Assert.Equal("photo", first.RecipeImage);
        Assert.Equal(0, first.Ingredients[0].Id); Assert.Equal(0, first.Ingredients[0].RecipeId); Assert.False(first.Ingredients[0].InBasket);
        first.Ingredients[0].Name = "Changed";
        Assert.Equal("Pasta", second.Ingredients[0].Name); Assert.Equal("Pasta", snapshot.Ingredients[0].Name);
    }
}
