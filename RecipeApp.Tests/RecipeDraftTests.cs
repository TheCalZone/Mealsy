using RecipeApp.Models;
using RecipeApp.Services;
using SQLite;
using System.Text.Json;
using Xunit;

public class RecipeDraftTests
{
    private static RecipeDraft Valid() => new() { Recipe = new() { RecipeName = "  Tomato soup  ", Description = "A quick lunch", Tags = "Lunch, lunch;Quick" },
        Sections = new() { new() { Name = "Soup", Items = new() { new Ingredient { Name = " Tomatoes ", Quantity = "400 g", Tag = "Veg", Note = "Chopped", MainIngredient = true } } } },
        Steps = new() { new() { Text = "Simmer gently." }, new() { Text = "Blend and serve." } }, PrepMinutes = "10", CookMinutes = "20" };
    [Fact]
    public void BuildNormalizesFieldsWithoutMutatingDraftAndPreservesSections()
    {
        var draft = Valid(); var recipe = draft.Build();
        Assert.Equal("Tomato soup", recipe.RecipeName); Assert.Equal("  Tomato soup  ", draft.Recipe.RecipeName);
        Assert.Equal(2, recipe.Serving); Assert.Equal(30, recipe.TotalTime); Assert.Equal("lunch,quick,all", recipe.Tags);
        var item = Assert.Single(recipe.Ingredients); Assert.Equal("Soup", item.IngredientSection); Assert.Equal("Produce", item.Tag); Assert.Equal("Tomatoes", item.Name);
        Assert.Equal("1. Simmer gently.\n\n2. Blend and serve.", recipe.RecipeMethod);
    }
    [Fact]
    public void EditingDraftIsDetachedFromRecipeAndIngredientReferences()
    {
        var source = Valid().Build(); source.Id = 42; source.Ingredients[0].Id = 7; source.RecipeImage = "old photo";
        var draft = RecipeDraft.FromRecipe(source);
        draft.Recipe.RecipeName = "Changed"; draft.Recipe.RecipeImage = "new photo"; draft.Sections[0].Items[0].Name = "Rice";
        Assert.Equal("Tomato soup", source.RecipeName); Assert.Equal("old photo", source.RecipeImage); Assert.Equal("Tomatoes", source.Ingredients[0].Name);
        Assert.Equal(42, draft.Recipe.Id); Assert.Equal(7, draft.Sections[0].Items[0].Id);
    }
    [Theory]
    [InlineData("1 1/2 cups flour, sifted", "1 1/2 cups", "flour", "sifted")]
    [InlineData("½ tsp salt", "½ tsp", "salt", "")]
    [InlineData("1½ cups tomatoes, chopped", "1½ cups", "tomatoes", "chopped")]
    [InlineData("400g tomatoes", "400g", "tomatoes", "")]
    [InlineData("2 x 400 g tomatoes", "2 x 400 g", "tomatoes", "")]
    [InlineData("1,5 l milk", "1,5 l", "milk", "")]
    [InlineData("Salt to taste", "", "Salt to taste", "")]
    [InlineData("2 eggs", "2", "eggs", "")]
    public void IngredientPasteRetainsQuantitiesAndOptionalNotes(string text, string quantity, string name, string note)
    {
        var item = Assert.Single(Assert.Single(RecipeDraft.ParseIngredients(text)).Items);
        Assert.Equal(quantity, item.Quantity); Assert.Equal(name, item.Name); Assert.Equal(note, item.Note); Assert.True(item.MainIngredient);
    }
    [Fact]
    public void IngredientPasteRecognizesSectionsBulletsAndMixedLineEndings()
    {
        var parsed = RecipeDraft.ParseIngredients("[Sauce]\r\n- 2 tbsp oil\r\n• 1 onion\nTopping:\n[ ] 20 g cheese");
        Assert.Equal(2, parsed.Count); Assert.Equal("Sauce", parsed[0].Name); Assert.Equal(2, parsed[0].Items.Count);
        Assert.Equal("Topping", parsed[1].Name); Assert.Equal("cheese", parsed[1].Items[0].Name);
    }
    [Fact]
    public void NumberedLegacyStepsRoundTripWithoutDoubleNumbering()
    {
        var steps = RecipeDraft.ParseSteps("1. Heat oil.\r\n2. Add tomatoes.\n\n3. Serve.");
        Assert.Equal(new[] { "Heat oil.", "Add tomatoes.", "Serve." }, steps);
        var draft = Valid(); draft.Steps = new(steps.Select(s => new RecipeStep { Text = s }));
        Assert.Equal(steps, RecipeDraft.ParseSteps(draft.Method()));
    }
    [Fact]
    public void ValidationRejectsDataLossFromInvalidNumbersAndUnsafeSourceLinks()
    {
        var draft = Valid(); draft.Servings = "two"; draft.CookMinutes = "-1"; draft.Rating = "9"; draft.Recipe.SourceUrl = "javascript:alert(1)";
        Assert.Contains(draft.Validate(), e => e.StartsWith("Servings")); Assert.Contains(draft.Validate(), e => e.StartsWith("Cook time"));
        Assert.Contains(draft.Validate(), e => e.StartsWith("Rating")); Assert.Contains(draft.Validate(), e => e.Contains("source"));
        Assert.Throws<InvalidOperationException>(draft.Build);
    }
    [Fact]
    public void DuplicateSectionNamesCannotSilentlyMergeOnReopening()
    {
        var draft = Valid(); draft.Sections.Add(new() { Name = " soup " });
        Assert.Contains(draft.Validate(), e => e.Contains("different name"));
        Assert.Throws<InvalidOperationException>(draft.Build);
    }
    [Fact]
    public void GuestDraftCannotCollideWithAnAccountNamedDevice()
    {
        Assert.NotEqual(RecipeDraftStore.PathFor(null, 0), RecipeDraftStore.PathFor("device", 0));
    }
    [Fact]
    public async Task DraftsRecoverPerAccountAndRecipeAndClearIndependently()
    {
        var user = "draft-test-" + Guid.NewGuid(); var draft = Valid(); draft.PendingPaste = "Uncommitted paste";
        await RecipeDraftStore.Save(user, draft);
        Assert.Null(await RecipeDraftStore.Load(user + "other", 0)); Assert.Null(await RecipeDraftStore.Load(user, 17));
        var restored = await RecipeDraftStore.Load(user, 0);
        Assert.Equal("Uncommitted paste", restored.PendingPaste); Assert.Equal("Soup", restored.Sections[0].Name); Assert.Equal("Blend and serve.", restored.Steps[1].Text);
        await RecipeDraftStore.Clear(user, 0); Assert.Null(await RecipeDraftStore.Load(user, 0));
    }
    [Fact]
    public async Task CorruptDraftIsPreservedForRecovery()
    {
        var user = "corrupt-" + Guid.NewGuid(); var path = RecipeDraftStore.PathFor(user, 0); Directory.CreateDirectory(Path.GetDirectoryName(path));
        await File.WriteAllTextAsync(path, "not valid JSON"); await Assert.ThrowsAsync<JsonException>(() => RecipeDraftStore.Load(user, 0));
        Assert.Equal("not valid JSON", await File.ReadAllTextAsync(path + ".recovery")); File.Delete(path); File.Delete(path + ".recovery");
    }
    [Fact]
    public async Task NewFieldsRoundTripThroughRealRecipePersistenceAndUpdates()
    {
        var recipe = Valid().Build(); recipe.SourceUrl = "https://example.com/recipe"; recipe.Notes = "Private variation";
        await RecipeService.AddRecipe(recipe); var saved = await RecipeService.GetRecipe(recipe.Id);
        Assert.Equal("A quick lunch", saved.Description); Assert.Equal(20, saved.CookTime); Assert.Equal(recipe.SourceUrl, saved.SourceUrl); Assert.Equal("Private variation", saved.Notes);
        saved.CookTime = 35; await RecipeService.UpdateRecipeAndIngredientsAsync(saved, saved.Ingredients);
        Assert.Equal(35, (await RecipeService.GetRecipe(recipe.Id)).CookTime);
    }
    [Fact]
    public void ExistingRecipeTableIsMigratedWithoutLosingOldRows()
    {
        var path = Path.Combine(Path.GetTempPath(), "mealsy-migration-" + Guid.NewGuid() + ".db");
        using (var connection = new SQLiteConnection(path))
        {
            connection.Execute("CREATE TABLE Recipe (Id INTEGER PRIMARY KEY AUTOINCREMENT, RecipeName TEXT, PrepTime INTEGER)");
            connection.Execute("INSERT INTO Recipe (RecipeName, PrepTime) VALUES ('Old soup', 12)");
            connection.CreateTable<Recipe>(); var old = connection.Table<Recipe>().Single();
            Assert.Equal("Old soup", old.RecipeName); Assert.Equal(12, old.PrepTime); Assert.Equal(0, old.CookTime);
            old.Description = "Still here"; old.CookTime = 18; connection.Update(old);
            Assert.Equal(30, connection.Get<Recipe>(old.Id).TotalTime);
        }
        File.Delete(path);
    }
    [Fact]
    public void PublicSnapshotAndCopyKeepPrivateAnnotationsPrivate()
    {
        var source = Valid().Build(); source.Notes = "Private notes"; source.SourceUrl = "https://private.example.com/source";
        var publicJson = JsonSerializer.Serialize(FeedRecipe.FromRecipe(source)); var text = RecipePresentation.PlainText(source);
        Assert.DoesNotContain("Private notes", publicJson); Assert.DoesNotContain("private.example", publicJson);
        Assert.DoesNotContain("Private notes", text); Assert.DoesNotContain("private.example", text);
        var shared = FeedRecipe.FromRecipe(source).ToRecipe("");
        Assert.Equal(source.Description, shared.Description); Assert.Equal(20, shared.CookTime); Assert.Equal("Soup", shared.Ingredients[0].IngredientSection);
        Assert.True(shared.Ingredients[0].MainIngredient);
    }
    [Fact]
    public void OldCommunitySnapshotsRemainReadable()
    {
        var recipe = JsonSerializer.Deserialize<FeedRecipe>("{\"Title\":\"Old soup\",\"Method\":\"Cook\",\"Servings\":2,\"PrepMinutes\":10,\"Ingredients\":[{\"Name\":\"Onion\",\"Quantity\":\"1\",\"Category\":\"Produce\",\"Note\":\"\"}]}");
        Assert.Equal(0, recipe.CookMinutes); Assert.Equal("Ingredients", recipe.Ingredients[0].Section); Assert.True(recipe.Ingredients[0].IsKey);
    }
}
