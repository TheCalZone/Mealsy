using RecipeApp.Models;
using System.Globalization;
namespace RecipeApp.Services;
public static class SocialPolicy
{
    public static readonly string[] Audiences={"public","friends","onlyMe"};
    public static readonly string[] AudienceLabels={"Everyone in Mealsy","Friends only","Only me"};
    public static string Audience(string value)=>Audiences.Contains(value)?value:throw new InvalidOperationException("Choose who can see this post.");
    public static string AudienceLabel(string value)=>AudienceLabels[Array.IndexOf(Audiences,Audience(value))];
    public static bool PrivacyReady(SocialCapabilities capabilities)=>capabilities is {Version:>=1,PrivacyEnforcedVersion:>=1};
    public static bool NeedsPrivacyService(string audience)=>Audience(audience)!="public";
    public static void ValidatePost(FoodPost post)
    {
        Audience(post.Audience);
        if(string.IsNullOrWhiteSpace(post.Id)||string.IsNullOrWhiteSpace(post.AuthorId)||(NeedsPrivacyService(post.Audience)&&!post.PrivacyManaged))throw new InvalidOperationException("This post's privacy metadata could not be verified. Refresh before opening it.");
    }
    public static string DateKey(DateTime date)=>date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    public static void ValidateMeal(HouseholdMeal meal)
    {
        if(!Guid.TryParseExact(meal.Id,"N",out _)||!DateTime.TryParseExact(meal.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)||!new[]{"breakfast","lunch","dinner"}.Contains(meal.Slot))throw new InvalidOperationException("Choose a valid meal day and slot.");
        if(string.IsNullOrWhiteSpace(meal.Title)||meal.Title.Length>160||meal.Servings<1||meal.Servings>1000||(meal.Note??"").Length>500)throw new InvalidOperationException("Add a meal name, 1–1,000 portions and a note of at most 500 characters.");
    }
}
