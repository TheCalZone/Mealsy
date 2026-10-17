using RecipeApp.Services;
using Xunit;
public class ProductFeaturesTests
{
    [Theory]
    [InlineData("FoodFeedPage")][InlineData("FeedComposerPage")][InlineData("FriendPostPage")]
    [InlineData("FriendsPage")][InlineData("FriendProfilePage")][InlineData("ActivityPage")]
    [InlineData("BusinessHubPage")][InlineData("BusinessCollectionPage")][InlineData("BusinessVerificationPage")]
    [InlineData("CampaignWalletPage")][InlineData("SharedPostPage")][InlineData("SharedLinkSignInPage")]
    public void SocialDestinationsAreDisabledAndUnregistered(string page)
    {
        Assert.False(ProductFeatures.SocialEnabled);Assert.False(ProductFeatures.PageAvailable(page));
        Assert.False(ProductFeatures.RouteAvailable("//Homepage/"+page));Assert.DoesNotContain(page,NavigationCatalog.Details);
    }
    [Theory]
    [InlineData("HouseholdPage")][InlineData("HouseholdMealsPage")][InlineData("HouseholdMealEditorPage")]
    [InlineData("ShoppingListPage")][InlineData("ShoppingListMakerPage")][InlineData("RecipeWebsitePage")]
    [InlineData("AdminPage")][InlineData("MealPlanOverviewPage")]
    public void PersonalHouseholdAndPremiumToolsStayAvailable(string page)=>Assert.True(ProductFeatures.PageAvailable(page));
    [Fact]
    public void EncodedSocialRoutesAreBlockedWithoutDisablingOrdinaryRoots()
    {
        Assert.False(ProductFeatures.RouteAvailable("//Homepage/%46oodFeedPage"));
        foreach(var root in NavigationCatalog.Roots)Assert.True(ProductFeatures.RouteAvailable("//"+root));
    }
}
