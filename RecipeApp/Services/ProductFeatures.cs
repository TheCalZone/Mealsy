namespace RecipeApp.Services;

/// <summary>Release switches changed in source, never by an end-user setting.</summary>
public static class ProductFeatures
{
    public static bool SocialEnabled => false;
    private static readonly HashSet<string> SocialPages=new(StringComparer.Ordinal)
    {
        "FoodFeedPage","FeedRecipePage","FeedCommentsPage","FeedComposerPage","FeedPreferencesPage",
        "FriendsPage","FriendProfilePage","EditFriendProfilePage","FriendPostPage","ActivityPage",
        "SharedPostPage","SharedLinkSignInPage","BusinessDirectoryPage","BusinessPublicPage",
        "BusinessCollectionEditorPage","BusinessCollectionPage","BusinessCampaignPage","BusinessVerificationPage",
        "BusinessHubPage","BusinessProfileEditorPage","BusinessWorkspacePage","BusinessTeamPage",
        "BusinessAnalyticsPage","BusinessRecipeImportPage","CampaignWalletPage"
    };
    public static bool PageAvailable(string name)=>SocialEnabled||!SocialPages.Contains(name??"");
    public static bool RouteAvailable(string route)=>route!=null&&route.Split(new[]{'/','?','&','='},StringSplitOptions.RemoveEmptyEntries).All(part=>PageAvailable(Uri.UnescapeDataString(part)));
}
