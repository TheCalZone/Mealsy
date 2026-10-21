using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
namespace RecipeApp.Tests;

public class LeftoverRecipesTests
{
    private static Recipe Recipe(long id,string name,string owner,params string[] ingredients)=>new(){Id=id,RecipeName=name,FirebaseUID=owner,Ingredients=ingredients.Select(n=>new Ingredient{Name=n}).ToList()};
    [Theory]
    [InlineData("I have minced beef left")]
    [InlineData("500g beef mince")]
    [InlineData("Some ground beef to use up")]
    [InlineData("mince beef")]
    [InlineData("What can I make with minced beef?")]
    public void CommonMinceWordingFindsActualIngredients(string input)
    {
        var recipes=new[]{Recipe(1,"Bolognese",null,"500 g minced beef","Tomatoes"),Recipe(2,"Beef ideas",null,"Lamb mince")};
        var result=Assert.Single(LeftoverRecipes.Find(recipes,null,input));
        Assert.Equal(1,result.Recipe.Id);Assert.Single(result.Matched);Assert.Empty(result.Missing);
    }
    [Fact]
    public void MultipleIngredientsRankCoverageAndExplainPartialMatches()
    {
        var recipes=new[]{Recipe(1,"Beef only",null,"Ground beef"),Recipe(2,"Beef and carrot pie",null,"Beef mince","Carrots")};
        var results=LeftoverRecipes.Find(recipes,null,"I have minced beef left, carrots and onions");
        Assert.Equal(new long[]{2,1},results.Select(r=>r.Recipe.Id));
        Assert.Equal(2,results[0].Matched.Count);Assert.Equal("onion",Assert.Single(results[0].Missing));
        Assert.Equal(2,results[0].IngredientNames.Count);Assert.Equal(2,results[1].Missing.Count);
    }
    [Fact]
    public void NeverMatchesTitlesSubstringsOrOtherAccounts()
    {
        var recipes=new[]{Recipe(1,"Eggs",null,"Eggplant"),Recipe(2,"Peas",null,"Chickpeas"),Recipe(3,"Eggs","alice","Eggs"),Recipe(4,"Peas","bob","Peas")};
        Assert.Empty(LeftoverRecipes.Find(recipes,null,"eggs and peas"));
        Assert.Equal(3,Assert.Single(LeftoverRecipes.Find(recipes,"alice","egg")).Recipe.Id);
        Assert.Empty(LeftoverRecipes.Find(recipes,"alice","peas"));
    }
    [Fact]
    public void BasicIngredientsDoNotStandInForProcessedProducts()
    {
        var recipes=new[]{Recipe(1,"Soup",null,"Chicken stock"),Recipe(2,"Roast",null,"Chicken thighs"),Recipe(3,"Cake",null,"Rice flour")};
        Assert.Equal(2,Assert.Single(LeftoverRecipes.Find(recipes,null,"chicken")).Recipe.Id);
        Assert.Equal(1,Assert.Single(LeftoverRecipes.Find(recipes,null,"chicken stock")).Recipe.Id);
        Assert.Empty(LeftoverRecipes.Find(recipes,null,"rice"));
    }
    [Fact]
    public void EmptyOrExcessiveQueriesHaveClearLimits()
    {
        Assert.Empty(LeftoverRecipes.Ingredients("I have some leftovers to use up"));
        Assert.Empty(LeftoverRecipes.Find(new[]{Recipe(1,"Eggs",null,"Eggs")},null,""));
        Assert.Single(LeftoverRecipes.Ingredients("eggs, egg and eggs"));
        Assert.Throws<InvalidOperationException>(()=>LeftoverRecipes.Ingredients(new string('a',1001)));
        Assert.Throws<InvalidOperationException>(()=>LeftoverRecipes.Ingredients(string.Join(',',Enumerable.Range(0,11).Select(i=>"food"+(char)('a'+i)))));
    }
}
