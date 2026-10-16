using RecipeApp.Models;
using RecipeApp.Services;
using SQLite;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

public class LocalDataTests
{
    private static Recipe NewRecipe(string name) => new()
    {
        RecipeName = name, Ingredients = new() { new Ingredient { Name = "onion" } }
    };

    [Fact]
    public async Task ConcurrentFirstUseCanReadAllRelationships()
    {
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => RecipeService.GetAllRecipes()));
        var recipe = NewRecipe("first use");
        await RecipeService.AddRecipe(recipe);
        var loaded = Assert.Single((await RecipeService.GetAllRecipes()).Where(r => r.Id == recipe.Id));
        Assert.Equal("onion", Assert.Single(loaded.Ingredients).Name);
    }

    [Fact]
    public async Task EditingRecipeReplacesIngredientsWithoutChangingOtherRecipes()
    {
        var recipe = NewRecipe("editable");
        var other = NewRecipe("untouched");
        await RecipeService.AddRecipe(recipe);
        await RecipeService.AddRecipe(other);
        recipe.RecipeName = "updated";
        await RecipeService.UpdateRecipeAndIngredientsAsync(recipe,
            new() { new Ingredient { Name = "rice" }, new Ingredient { Name = "beans" } });
        var loaded = await RecipeService.GetRecipe(recipe.Id);
        Assert.Equal("updated", loaded.RecipeName);
        Assert.Equal(new[] { "rice", "beans" }, loaded.Ingredients.Select(i => i.Name));
        Assert.All(loaded.Ingredients, i => Assert.Equal(recipe.Id, i.RecipeId));
        Assert.Equal("onion", Assert.Single((await RecipeService.GetRecipe(other.Id)).Ingredients).Name);
    }

    [Fact]
    public async Task DeletingRecipeAlsoDeletesItsIngredients()
    {
        var recipe = NewRecipe("delete me");
        await RecipeService.AddRecipe(recipe);
        await RecipeService.RemoveRecipe(recipe.Id);
        Assert.Null(await RecipeService.GetRecipe(recipe.Id));
        using var connection = new SQLiteConnection(RecipeService.DatabasePath);
        Assert.Empty(connection.Table<Ingredient>().Where(i => i.RecipeId == recipe.Id).ToList());
    }

    [Fact]
    public async Task MissingRecipeReturnsNull()
        => Assert.Null(await RecipeService.GetRecipe(long.MaxValue));

    [Fact]
    public async Task SnackAndStapleCachesReflectInsertAndDelete()
    {
        var snack = new Snack { Name = "cache snack" };
        var staple = new Staple { Name = "cache staple" };
        await RecipeService.InsertSnack(snack);
        await RecipeService.InsertStaple(staple);
        Assert.Contains(RecipeService.Snacks, s => s.Id == snack.Id);
        Assert.Contains(RecipeService.Staples, s => s.Id == staple.Id);
        await RecipeService.DeleteSnack(snack);
        await RecipeService.DeleteStaple(staple);
        Assert.DoesNotContain(await RecipeService.GetAllSnacks(), s => s.Id == snack.Id);
        Assert.DoesNotContain(RecipeService.Snacks, s => s.Id == snack.Id);
        Assert.DoesNotContain(RecipeService.Staples, s => s.Id == staple.Id);
    }

    [Fact]
    public async Task EditingMissingRecipeCannotInsertOrOverwriteIngredients()
    {
        var recipe = NewRecipe("kept");
        await RecipeService.AddRecipe(recipe);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RecipeService.UpdateRecipeAndIngredientsAsync(new Recipe { Id = long.MaxValue }, recipe.Ingredients));
        Assert.Equal("onion", Assert.Single((await RecipeService.GetRecipe(recipe.Id)).Ingredients).Name);
    }

    [Fact]
    public async Task ConcurrentMealPlanCreationIsIdempotent()
    {
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => RecipeService.CreateMealPlan()));
        var plan = await RecipeService.GetMealPlan();
        Assert.Equal(Enumerable.Range(0, 7).Select(i => (long)i), plan.Days.OrderBy(d => d.MealDayIndex).Select(d => d.MealDayIndex));
        using var connection = new SQLiteConnection(RecipeService.DatabasePath);
        Assert.Single(connection.Table<MealPlan>().ToList());
    }

    [Fact]
    public async Task MealPlanRestoresMultipleRecipesAndToleratesEmptyNotes()
    {
        var first = NewRecipe("breakfast one");
        var second = NewRecipe("breakfast two");
        await RecipeService.AddRecipe(first);
        await RecipeService.AddRecipe(second);
        await RecipeService.CreateMealPlan();
        var plan = await RecipeService.GetMealPlan();
        var day = plan.Days[0];
        day.BreakfastIDJson = JsonSerializer.Serialize(new[] { first.Id, second.Id });
        day.BreakfastNotesJson = "null";
        day.LunchNotesJson = "invalid legacy JSON";
        await RecipeService.SaveMealPlanDay(day, plan.Id, day.MealDayIndex);
        var restored = (await RecipeService.GetMealPlan()).Days.Single(d => d.Id == day.Id);
        Assert.Equal(2, restored.BreakfastRecipes.Count);
        Assert.Empty(restored.BreakfastNotes);
        Assert.Empty(restored.LunchNotes);
    }
}
