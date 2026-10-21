using RecipeApp.Services;
using RecipeApp.Models;
using Xunit;
public class ContentLinksTests
{
    [Theory]
    [InlineData("post")][InlineData("social")][InlineData("plan")]
    public void SharesRoundTripWithoutContainingRecipeOrProfileData(string kind)
    {
        var target=ContentLinks.Target(kind,"abc_-123");Assert.Equal(target,ContentLinks.Parse(ContentLinks.Url(target)));
        Assert.Equal(target,ContentLinks.Parse($"mealsy://content?kind={kind}&id=abc_-123"));
        Assert.Equal(target,ContentLinks.Parse($"https://mealsy-b732b.firebaseapp.com/share/?kind={kind}&id=abc_-123"));
    }
    [Theory]
    [InlineData("https://evil.test/share/?kind=post&id=x")]
    [InlineData("https://mealsy-b732b.web.app.evil.test/share/?kind=post&id=x")]
    [InlineData("http://mealsy-b732b.web.app/share/?kind=post&id=x")]
    [InlineData("https://user@mealsy-b732b.web.app/share/?kind=post&id=x")]
    [InlineData("https://mealsy-b732b.web.app:444/share/?kind=post&id=x")]
    [InlineData("https://mealsy-b732b.web.app/share/?kind=post&id=../users")]
    [InlineData("mealsy://content?kind=post&id=x&kind=social")]
    [InlineData("mealsy://content?kind=post&id=x#kind=social")]
    [InlineData("mealsy://content?kind=admin&id=x")]
    [InlineData("mealsy://content?kind=post&id=%2Fusers")]
    [InlineData("mealsy://content/elsewhere?kind=post&id=x")]
    [InlineData("mealsy://content?kind=post")]
    public void RejectsUntrustedAmbiguousOrMalformedTargets(string link)=>Assert.Throws<InvalidOperationException>(()=>ContentLinks.Parse(link));
    [Fact]
    public void ShoppingCombinesRepeatedRecipePortionsAndExcludesUnselectedMeals()
    {
        var post=new FoodPost{Id="pasta",Recipe=new FeedRecipe("Pasta","Cook",4,10,new())};
        var choices=new[]{new BusinessPlanChoice{Post=post,Day=0,Slot=2,Servings="2"},new BusinessPlanChoice{Post=post,Day=1,Slot=2,Servings="3"},new BusinessPlanChoice{Selected=false,Post=post,Day=2,Slot=2,Servings="99"}};
        Assert.Equal(5,CreatorPlanSelection.ShoppingServings(choices)["pasta"]);
        var shop=new RecipeShoppingChoice(post.Recipe.ToRecipe("")){Selected=true,Servings="5"};Assert.Equal(1.25m,shop.Factor());
        choices[1].Day=0;Assert.Throws<InvalidOperationException>(()=>CreatorPlanSelection.Validate(choices));
    }
    [Theory]
    [InlineData("0")][InlineData("1001")][InlineData("2.5")][InlineData("wrong")]
    public void InvalidServingsCannotReachImportOrShopping(string servings)=>Assert.Throws<InvalidOperationException>(()=>CreatorPlanSelection.ShoppingServings(new[]{new BusinessPlanChoice{Post=new FoodPost{Id="meal",Recipe=new("Meal","",2,0,new())},Day=0,Slot=0,Servings=servings}}));
}
