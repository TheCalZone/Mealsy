using RecipeApp.Services;
using Xunit;

public class PastedListTests
{
    [Theory]
    [InlineData("☐ 200 g tomatoes (chopped)")]
    [InlineData("☑️ 200 g tomatoes (chopped)")]
    [InlineData("✓ 200 g tomatoes (chopped)")]
    [InlineData("✅ 200 g tomatoes (chopped)")]
    [InlineData("- [x] **200 g tomatoes (chopped)**")]
    [InlineData("1. 200 g tomatoes (chopped)")]
    [InlineData("• [200 g] tomatoes (chopped)")]
    [InlineData("\u200B☐\u00A0200\u202Fg tomatoes (chopped)\u200E")]
    public void RecipePasteRemovesListDecorationButKeepsAmountAndParentheses(string text)
    {
        var item = Assert.Single(Assert.Single(RecipeDraft.ParseIngredients(text)).Items);
        Assert.Equal("200 g", item.Quantity);
        Assert.Equal("tomatoes (chopped)", item.Name);
    }

    [Fact]
    public void ShoppingPasteAcceptsNotesChecksAndKeepsTheirState()
    {
        var items = ShoppingListDocument.ParseText("☐ Milk (whole)\r☑️ Bread\n✅ Apples\u2028○ Rice\u2029- [ ] [Coffee] (decaf)\n[x] Tea");
        Assert.Equal(new[] { "Milk (whole)", "Bread", "Apples", "Rice", "Coffee (decaf)", "Tea" }, items.Select(i => i.Name));
        Assert.Equal(new[] { false, true, true, false, false, true }, items.Select(i => i.InBasket));
    }

    [Fact]
    public void CleanupPreservesFoodPunctuationAccentsFractionsAndPackSizes()
    {
        var input = "☐ 1½ cups crème fraîche (full-fat), room-temperature";
        var item = Assert.Single(Assert.Single(RecipeDraft.ParseIngredients(input)).Items);
        Assert.Equal("1½ cups", item.Quantity);
        Assert.Equal("crème fraîche (full-fat)", item.Name);
        Assert.Equal("room-temperature", item.Note);
        Assert.Equal("2 × 400 g tins (chopped)", PastedListText.Clean("□ 2 × 400 g tins (chopped)", out _));
    }

    [Fact]
    public void BracketWrappedItemsAreNotLostAndLegacySectionsStillWork()
    {
        Assert.Equal("Milk", Assert.Single(ShoppingListDocument.ParseText("[Milk]")).Name);
        var ingredients = RecipeDraft.ParseIngredients("[Sauce]\r200 g tomatoes\r[1 onion]");
        Assert.Equal("Sauce", Assert.Single(ingredients).Name);
        Assert.Equal(2, ingredients[0].Items.Count);
        Assert.Equal("onion", ingredients[0].Items[1].Name);
        Assert.Equal("1", ingredients[0].Items[1].Quantity);
        Assert.Equal("Milk", Assert.Single(Assert.Single(RecipeDraft.ParseIngredients("[Milk]")).Items).Name);
    }

    [Fact]
    public void MultipleBracketWrappedFoodNamesRemainIngredients()
    {
        var ingredients = Assert.Single(RecipeDraft.ParseIngredients("[Milk]\n[Eggs]\n[Flour]"));
        Assert.Equal(new[] { "Milk", "Eggs", "Flour" }, ingredients.Items.Select(i => i.Name));
    }

    [Fact]
    public void EmptyCheckboxesAndFormattingNeverCreateItems()
    {
        Assert.Empty(ShoppingListDocument.ParseText("☐\n[x]\n- [ ]\n**\n[]\n☑️"));
        Assert.Empty(RecipeDraft.ParseIngredients("☐\n[x]\n- [ ]\n**\n[]\n☑️"));
    }

    [Fact]
    public void MethodPasteAlsoCleansCheckboxesAndMarkdownWithoutLosingTemperature()
    {
        Assert.Equal(new[] { "Heat oven to 180°C (fan).", "Mix flour and salt." }, RecipeDraft.ParseSteps("☐ 1. **Heat oven to 180°C (fan).**\r☑ 2. Mix [flour] and salt."));
    }
}
