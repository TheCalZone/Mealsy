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
    [Theory]
    [InlineData("\"https://cdn.example.com/food.jpg\"")]
    [InlineData("[\"http://cdn.example.com/unsafe.jpg\",\"https://cdn.example.com/food.jpg\"]")]
    [InlineData("{\"@type\":\"ImageObject\",\"url\":\"https://cdn.example.com/food.jpg\"}")]
    [InlineData("{\"contentUrl\":\"https://cdn.example.com/food.jpg\"}")]
    public void PhotoMetadataSupportsRecipeImageShapes(string image)
    {
        var html="<script type=\"application/ld+json\">{\"@graph\":[{\"@type\":\"Organization\",\"image\":\"https://cdn.example.com/logo.jpg\"},{\"@type\":\"Recipe\",\"image\":"+image+"}]}</script>";
        Assert.Equal("https://cdn.example.com/food.jpg",WebsiteRecipe.PhotoUrl(html,"https://bbcgoodfood.com/recipes/test"));
    }
    [Theory]
    [InlineData("http://cdn.example.com/photo.jpg")]
    [InlineData("https://localhost/photo.jpg")]
    [InlineData("https://127.0.0.1/photo.jpg")]
    [InlineData("https://user:password@cdn.example.com/photo.jpg")]
    [InlineData("https://cdn.example.com:8080/photo.jpg")]
    [InlineData("https://printer.local/photo.jpg")]
    public void UnsafePhotoLinksAreRejected(string url)=>Assert.False(WebsiteRecipe.IsPhotoUrl(new Uri(url)));
    [Fact]
    public void MissingPhotoAndRelativePhotoAreHandled()
    {
        Assert.Null(WebsiteRecipe.PhotoUrl("<html>No image</html>","https://bbcgoodfood.com/recipes/test"));
        Assert.Equal("https://bbcgoodfood.com/photo.jpg",WebsiteRecipe.PhotoUrl("<script type=\"application/ld+json\">{\"@type\":\"Recipe\",\"image\":\"/photo.jpg\"}</script>","https://bbcgoodfood.com/recipes/test"));
    }
}
