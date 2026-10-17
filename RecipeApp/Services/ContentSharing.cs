using RecipeApp.Models;
using RecipeApp.Views;
namespace RecipeApp.Services;

public static class ContentSharing
{
    private const string PendingKey="Mealsy.pendingContentLink";
    public static bool HasPending=>!string.IsNullOrEmpty(Preferences.Get(PendingKey,""));
    public static Task SharePost(FoodPost post){SocialPolicy.ValidatePost(post);return Share(ContentLinks.Target(post.PrivacyManaged?"social":"post",post.Id));}
    public static Task SharePlan(string id)=>Share(ContentLinks.Target("plan",id));
    private static Task Share(ContentTarget target)=>Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareTextRequest{
        Title=target.Kind=="plan"?"Share Mealsy meal plan":"Share Mealsy post",
        Text=(target.Kind=="plan"?"Plan your meals, save the recipes and review your shopping in Mealsy.":"Open this post in Mealsy. Sign in with your own account; the author's current privacy settings apply.")+"\n\n"+ContentLinks.Url(target)});
    public static async Task Open(ContentTarget target)
    {
        ContentLinks.Target(target.Kind,target.Id);
        if(AccountSession.UserId==null){Preferences.Set(PendingKey,ContentLinks.Url(target));await AppNavigation.Open(new SharedLinkSignInPage());return;}
        Preferences.Remove(PendingKey);
        await AppNavigation.Open(target.Kind=="plan"?new BusinessCollectionPage(target.Id):new SharedPostPage(target));
    }
    public static async Task Resume()
    {
        var raw=Preferences.Get(PendingKey,"");if(string.IsNullOrEmpty(raw)||AccountSession.UserId==null)return;
        ContentTarget target;try{target=ContentLinks.Parse(raw);}catch(InvalidOperationException){Preferences.Remove(PendingKey);return;}
        await Open(target);
    }
}
