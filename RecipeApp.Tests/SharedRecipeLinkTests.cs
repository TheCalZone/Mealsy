using RecipeApp.Services;
using Xunit;
namespace RecipeApp.Tests;
public class SharedRecipeLinkTests
{
    [Theory]
    [InlineData("My favourite recipe\nhttps://www.bbcgoodfood.com/recipes/chilli", "https://www.bbcgoodfood.com/recipes/chilli")]
    [InlineData("Try this (https://www.bbcgoodfood.com/recipes/chilli).", "https://www.bbcgoodfood.com/recipes/chilli")]
    [InlineData("<a href=\"https://www.bbcgoodfood.com/recipes/chilli?a=1&amp;b=2\">Recipe</a>", "https://www.bbcgoodfood.com/recipes/chilli?a=1&b=2")]
    [InlineData("https://short.example/link https://www.recipetineats.com/chilli", "https://www.recipetineats.com/chilli")]
    [InlineData("https://example.com/recipe", "https://example.com/recipe")]
    public void BrowserSharesExtractAndPreferSupportedRecipes(string text,string expected)=>Assert.Equal(expected,SharedRecipeLink.Extract(text));
    [Theory]
    [InlineData("")]
    [InlineData("Recipe text without a link")]
    [InlineData("file:///private/recipe.html")]
    [InlineData("content://images/photo")]
    [InlineData("https://user:password@www.bbcgoodfood.com/recipe")]
    [InlineData("https://www.bbcgoodfood.com:8080/recipe")]
    public void NonLinksFilesAndCredentialUrlsAreRejected(string text)=>Assert.Throws<InvalidOperationException>(()=>SharedRecipeLink.Extract(text));
    [Fact]
    public void OversizedSharePayloadsAreBounded()=>Assert.Throws<InvalidOperationException>(()=>SharedRecipeLink.Extract(new string('a',10001)));
}
