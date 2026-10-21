using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
public class ShoppingItemInputTests
{
    [Theory]
    [InlineData("500 g beef","","beef","500 g")]
    [InlineData("500g beef","  ","beef","500 g")]
    [InlineData("500 g beef","750 g","beef","750 g")]
    [InlineData("500 g beef","one pack","beef","one pack")]
    [InlineData("2 x 400 g tomatoes","","tomatoes","2 × 400 g")]
    [InlineData("½ kg flour","","flour","500 g")]
    [InlineData("1 1/2 tbsp oil","","oil","1.5 tbsp")]
    [InlineData("2 apples","","apples","2")]
    [InlineData("7UP","","7UP","")]
    [InlineData("2% milk","","2% milk","")]
    [InlineData("5 spice powder","","5 spice powder","")]
    [InlineData("Beef 500 g","","Beef 500 g","")]
    [InlineData("500 g","","500 g","")]
    public void RecognizesLeadingAmountsButExplicitQuantityWins(string name,string quantity,string expectedName,string expectedQuantity)
    {
        var parsed=ShoppingItemInput.Parse(name,quantity);Assert.Equal(expectedName,parsed.Name);Assert.Equal(expectedQuantity,parsed.Quantity);
    }
    [Fact]
    public void QuickAddAndPasteParseAmountsWithoutChangingNotesOrCheckmarks()
    {
        var document=new ShoppingListDocument();var item=new ShoppingListItem{Name="500 g beef",Quantity="750 g",Note="Lean",InBasket=true};document.Add(item);
        Assert.Equal("beef",item.Name);Assert.Equal("750 g",item.Quantity);Assert.Equal("Lean",item.Note);Assert.True(item.InBasket);
        var pasted=Assert.Single(ShoppingListDocument.ParseText("☑ 500 g beef — 1 kg (Lean)"));Assert.Equal("beef",pasted.Name);Assert.Equal("1 kg",pasted.Quantity);Assert.Equal("Lean",pasted.Note);Assert.True(pasted.InBasket);
    }
    [Fact]
    public void RecipeAmountsAreExtractedBeforePortionScalingAndMerge()
    {
        var recipe=new Recipe{RecipeName="Chilli",Serving=2,Ingredients=new(){new(){Name="500 g beef",Tag="Meat",MainIngredient=true},new(){Name="500 g beef",Quantity="250 g",Tag="Meat",MainIngredient=true}}};
        var rows=ShoppingPlan.Build(new[]{new RecipeShoppingChoice(recipe){Selected=true,Servings="4"}},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        var row=Assert.Single(rows);Assert.Equal("beef",row.Item.Name);Assert.Equal("1.5 kg",row.Item.Quantity);Assert.Contains("Chilli",row.Sources);Assert.False(row.CheckCupboard);
        Assert.Equal("500 g beef",recipe.Ingredients[0].Name);Assert.Null(recipe.Ingredients[0].Quantity);
    }
    [Fact]
    public void ImportedIngredientRowsUseExplicitAmountsBeforeCombining()
    {
        var rows=ShoppingListDocument.FromIngredients(new[]{new Ingredient{Name="500 g beef",Quantity="250 g"},new Ingredient{Name="500 g beef"}});
        Assert.Equal("beef",Assert.Single(rows).Name);Assert.Equal("750 g",rows[0].Quantity);
    }
}
