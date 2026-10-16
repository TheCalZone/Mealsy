using RecipeApp.Models;
using RecipeApp.Services;
using System.Xml.Linq;
using Xunit;
public class AccountAndNavigationTests
{
    [Theory]
    [InlineData(" alice@example.com ","alice@example.com")]
    [InlineData("alex+mealsy@example.co.uk","alex+mealsy@example.co.uk")]
    public void EmailValidationTrimsWithoutDroppingPlusAddresses(string value,string expected)=>Assert.Equal(expected,AccountInput.Email(value));
    [Theory]
    [InlineData("")][InlineData("invalid")][InlineData("User <alice@example.com>")][InlineData("alice@localhost")]
    public void InvalidEmailsCannotStartAuthRequests(string value)=>Assert.Throws<InvalidOperationException>(()=>AccountInput.Email(value));
    [Fact]
    public void RegistrationRejectsMismatchedPasswordsAndAcceptsOptionalProfileFields()
    {
        Assert.Throws<InvalidOperationException>(()=>AccountInput.Registration("a@example.com","password1","password2","Alex"));
        Assert.Throws<InvalidOperationException>(()=>AccountInput.Registration("a@example.com","short","short","Alex"));
        AccountInput.Registration("a@example.com","long password ","long password ","Alex");
    }
    [Fact]
    public void FirebaseAuthErrorsDistinguishSetupFailuresFromWrongCredentials()
    {
        Assert.Contains("enable",AccountInput.Error("OperationNotAllowed"));Assert.Contains("configuration",AccountInput.Error("InvalidApiKey"));
        Assert.Equal(AccountInput.Error("WrongPassword"),AccountInput.Error("UserNotFound"));Assert.Contains("connection",AccountInput.Error("NetworkError"));
    }
    [Fact]
    public void ShellRootsExactlyMatchTheNavigationCatalogAndFitAndroidTabBar()
    {
        var doc=XDocument.Load(Path.Combine(AppContext.BaseDirectory,"Navigation","AppShell.xaml"));var roots=doc.Descendants().Where(e=>e.Name.LocalName=="ShellContent").Select(e=>(string)e.Attribute("Route")).ToArray();
        Assert.Equal(NavigationCatalog.Roots.Order(),roots.Order());Assert.Equal(5,roots.Length);Assert.Equal(roots.Length,roots.Distinct().Count());Assert.Empty(NavigationCatalog.Roots.Intersect(NavigationCatalog.Details));Assert.Equal(NavigationCatalog.Details.Length,NavigationCatalog.Details.Distinct().Count());
    }
    [Fact]
    public void RecipeSearchNeverLeaksOtherAccountOrGuestRecipesAndUsesExactTags()
    {
        var recipes=new[]{new Recipe{RecipeName="Private",FirebaseUID="alice",Tags="vegan,all"},new Recipe{RecipeName="Another",FirebaseUID="bob",Tags="nonvegan"},new Recipe{RecipeName="Device",Tags="vegan"}};
        Assert.Equal("Private",Assert.Single(RecipeBrowser.Visible(recipes,"alice",tag:"vegan")).RecipeName);Assert.Equal("Device",Assert.Single(RecipeBrowser.Visible(recipes,null)).RecipeName);Assert.Empty(RecipeBrowser.Visible(recipes,"bob",tag:"vegan"));
    }
    [Fact]
    public void RecipeSortingHandlesMissingTimesAndSearchesDescriptions()
    {
        var recipes=new[]{new Recipe{RecipeName="Unknown",Description="comfort"},new Recipe{RecipeName="Fast",PrepTime=5},new Recipe{RecipeName="Slow",CookTime=30}};
        Assert.Equal(new[]{"Fast","Slow","Unknown"},RecipeBrowser.Visible(recipes,null,sort:"Quickest").Select(r=>r.RecipeName));Assert.Equal("Unknown",Assert.Single(RecipeBrowser.Visible(recipes,null,search:"comfort")).RecipeName);
    }
    [Fact]
    public void ServingPreviewKeepsRecipeQuantitiesPrivateNotesAndSectionsUnchanged()
    {
        var original=new Recipe{RecipeName="Soup",Serving=4,Notes="Private",Ingredients=new(){new(){Name="Oil",Quantity="2 tbsp",IngredientSection="Soup"},new(){Name="Cheese",Quantity="100 g",IngredientSection="Topping"}}};
        var copy=RecipeBrowser.ForServings(original,2);Assert.Equal("1 tbsp",copy.Ingredients[0].Quantity);Assert.Equal("50 g",copy.Ingredients[1].Quantity);Assert.Equal("2 tbsp",original.Ingredients[0].Quantity);Assert.Equal("Topping",copy.Ingredients[1].IngredientSection);Assert.Equal("Private",original.Notes);
    }
    [Fact]
    public async Task CloudRecipeIdsCannotOverwriteUnrelatedLocalOrAnotherAccountsRows()
    {
        var owner="recipe-cloud-"+Guid.NewGuid();var other="recipe-cloud-"+Guid.NewGuid();var original=new Recipe{RecipeName="Original",FirebaseUID=owner,Ingredients=new(){new(){Name="Original ingredient"}}};await RecipeService.AddRecipe(original);
        var foreign=new Recipe{RecipeName="Foreign",FirebaseUID=other,CloudKey="remote-key"};await RecipeService.AddRecipe(foreign);
        var remote=new Recipe{Id=original.Id,RecipeName="Downloaded",CloudKey="remote-key",LastUpdated=DateTimeOffset.UtcNow.AddMinutes(1),Ingredients=new(){new(){Id=1,Name="Downloaded ingredient"}}};
        await RecipeService.MergeCloudRecipes(owner,new[]{remote});Assert.Equal("Original",(await RecipeService.GetRecipe(original.Id)).RecipeName);Assert.Equal("Foreign",(await RecipeService.GetRecipe(foreign.Id)).RecipeName);
        var downloaded=Assert.Single(RecipeService.Recipes,r=>r.FirebaseUID==owner&&r.CloudKey=="remote-key");Assert.NotEqual(original.Id,downloaded.Id);Assert.Equal("Downloaded ingredient",Assert.Single((await RecipeService.GetRecipe(downloaded.Id)).Ingredients).Name);
        var id=downloaded.Id;remote.RecipeName="Renamed remotely";remote.LastUpdated=remote.LastUpdated.AddMinutes(1);await RecipeService.MergeCloudRecipes(owner,new[]{remote});Assert.Equal(id,Assert.Single(RecipeService.Recipes,r=>r.FirebaseUID==owner&&r.CloudKey=="remote-key").Id);
    }
    [Fact]
    public async Task RecipeDeletionMarkersRemoveOnlyTheMatchingAccountCloudIdentity()
    {
        var owner="deleted-recipe-"+Guid.NewGuid();var original=new Recipe{RecipeName="Remove me",FirebaseUID=owner,CloudKey="remove",Ingredients=new(){new(){Name="Old ingredient"}}};await RecipeService.AddRecipe(original);
        await RecipeService.MergeCloudRecipes(owner,new[]{new Recipe{CloudKey="remove",IsDeleted=true}});Assert.Null(await RecipeService.GetRecipe(original.Id));
    }
    [Fact]
    public async Task DeviceRecipeImportIsExplicitDetachedAndIdempotent()
    {
        var owner="import-"+Guid.NewGuid();var source=new Recipe{RecipeName="Device import "+Guid.NewGuid(),Serving=2,Ingredients=new(){new(){Name="Flour",Quantity="200 g"}}};await RecipeService.AddRecipe(source);
        await RecipeService.CopyDeviceRecipes(owner);Assert.Equal(0,await RecipeService.CopyDeviceRecipes(owner));
        var copy=Assert.Single(RecipeService.Recipes,r=>r.FirebaseUID==owner&&r.RecipeName==source.RecipeName);Assert.NotEqual(source.Id,copy.Id);Assert.NotEqual(source.CloudKey,copy.CloudKey);
        var original=await RecipeService.GetRecipe(source.Id);Assert.Null(original.FirebaseUID);Assert.Equal("200 g",Assert.Single(original.Ingredients).Quantity);
    }
    [Fact]
    public async Task DownloadedRecipeTimestampsDoNotBecomeFakeNewLocalEdits()
    {
        var owner="timestamp-"+Guid.NewGuid();var time=DateTimeOffset.UtcNow.AddDays(-1);var remote=new Recipe{CloudKey="timestamp",RecipeName="Old soup",LastUpdated=time};
        await RecipeService.MergeCloudRecipes(owner,new[]{remote});remote.RecipeName="Changed soup";remote.LastUpdated=time.AddHours(1);await RecipeService.MergeCloudRecipes(owner,new[]{remote});
        var local=Assert.Single(RecipeService.Recipes,r=>r.FirebaseUID==owner);Assert.Equal(remote.LastUpdated,local.LastUpdated);Assert.Equal("Changed soup",local.RecipeName);
    }

}
