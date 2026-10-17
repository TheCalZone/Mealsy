using RecipeApp.Services;
using Xunit;
public class WebsiteRecipeTests
{
    [Theory]
    [InlineData("http://www.bbcgoodfood.com/recipe")]
    [InlineData("https://bbcgoodfood.com.attacker.invalid/recipe")]
    [InlineData("https://localhost/recipe")]
    [InlineData("https://user:password@bbcgoodfood.com/recipe")]
    [InlineData("https://bbcgoodfood.com:8080/recipe")]
    public void ImportsRejectUnsafeOrUnsupportedHosts(string value)=>Assert.Throws<InvalidOperationException>(()=>WebsiteRecipe.Validate(value));
    [Fact]
    public void StructuredRecipeImportsGraphInstructionsAndKeepsAttribution()
    {
        var html="""<script type="application/ld+json">{"@graph":[{"@type":"WebPage"},{"@type":["Recipe"],"name":"Chilli &amp; rice","recipeYield":["8 servings"],"prepTime":"PT15M","cookTime":"PT1H","recipeIngredient":["1 onion","2 tins tomatoes"],"recipeInstructions":[{"@type":"HowToSection","itemListElement":[{"@type":"HowToStep","text":"<p>Chop the onion.</p>"},{"text":"Cook and serve."}]}]}]}</script>""";
        var r=WebsiteRecipe.Parse(html,"https://www.bbcgoodfood.com/recipes/chilli");
        Assert.Equal("Chilli & rice",r.RecipeName);Assert.Equal(8,r.Serving);Assert.Equal(15,r.PrepTime);Assert.Equal(60,r.CookTime);Assert.Equal(2,r.Ingredients.Count);Assert.Contains("Chop the onion.",r.RecipeMethod);Assert.Contains("bbcgoodfood.com",r.SourceUrl);Assert.Equal(0,r.Id);
    }
    [Fact]
    public void MissingRecipeOrIncompleteIngredientsRequireManualEntry()
    {
        Assert.Throws<InvalidOperationException>(()=>WebsiteRecipe.Parse("<html>Nothing here</html>","https://www.bbcgoodfood.com"));
        Assert.Throws<InvalidOperationException>(()=>WebsiteRecipe.Parse("""<script type="application/ld+json">{"@type":"Recipe","name":"Incomplete"}</script>""","https://www.bbcgoodfood.com"));
    }
}
