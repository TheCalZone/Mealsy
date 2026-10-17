using RecipeApp.Models;
namespace RecipeApp.Services;
public static class SocialService
{
    public const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/social";
    public static Task<T> Call<T>(object data)=>HouseholdService.CallEndpointAsync<T>(Endpoint,data);
    public static async Task<bool> Available()
    {
        try{return SocialPolicy.PrivacyReady(await Call<SocialCapabilities>(new{action="capabilities"}));}
        catch(HouseholdException){return false;}
    }
    public static async Task Require()
    {
        if(!await Available())throw new InvalidOperationException("Friends and private sharing need the updated account service. They may not be deployed yet, or your connection is unavailable. No private post has been sent as public.");
    }
    public static async Task Publish(string id,string caption,FeedRecipe recipe,string photo,string audience)
    {
        SocialPolicy.Audience(audience);await Require();
        await Call<object>(new{action="publish",id,caption,recipe,photo,audience});
    }
}
public static class HouseholdMealsService
{
    public const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/householdMeals";
    public static Task<T> Call<T>(object data)=>HouseholdService.CallEndpointAsync<T>(Endpoint,data);
    public static async Task Require()
    {
        try{var capability=await Call<HouseholdMealsCapabilities>(new{action="capabilities"});if(capability.Version>=1)return;}
        catch(HouseholdException){ }
        throw new InvalidOperationException("Shared household meal plans need the updated household service. It may not be deployed yet, or your connection is unavailable. Your personal meal plan is unchanged.");
    }
}
