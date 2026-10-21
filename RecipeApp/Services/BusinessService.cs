using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;

public class BusinessProfile
{
    public string Id{get;set;}public string Name{get;set;}public string Description{get;set;}public string Website{get;set;}public string Logo{get;set;}public string Kind{get;set;}
    public bool Verified{get;set;}public string Role{get;set;}public string Owner{get;set;}
}
public record BusinessList(List<BusinessProfile> Businesses,FeedCursor Cursor);
public record BusinessMember(string Uid,string Role,string Label=null);
public record BusinessInvitation(string Id,string TargetUid,string Role);
public record BusinessMeal(int Day,int Slot,string PostId);
public record BusinessOffer(string Label,string Url,string Code);
public class BusinessCollection
{
    public string Id{get;set;}public string BusinessId{get;set;}public string Title{get;set;}public string Description{get;set;}
    public List<string> PostIds{get;set;}=new();public List<BusinessMeal> Meals{get;set;}=new();public BusinessOffer Offer{get;set;}=new("","","");public bool Published{get;set;}
}
public class BusinessPost:FoodPost{public bool Hidden{get;set;}}
public record BusinessCampaign(string Id,string Name,string PostId,long BudgetMinor,long DailyBudgetMinor,long StartsAt,long EndsAt,string Status,decimal SpentMinor,long Impressions=0);
public record BusinessWorkspace(BusinessProfile Business,string Role,List<BusinessMember> Members,List<BusinessInvitation> Invitations,List<BusinessCollection> Collections,List<BusinessCampaign> Campaigns,List<BusinessPost> Posts,bool CheckoutEnabled);
public record PublicBusiness(BusinessProfile Business,bool Following,List<BusinessCollection> Collections);
public record BusinessCollectionDetail(BusinessCollection Collection,BusinessProfile Business,List<FoodPost> Posts);
public record BusinessMetricDay(string Day,int ProfileViews,int PostViews,int RecipeOpens,int Saves,int CollectionOpens,int Clicks,int PlanSaves);
public record BusinessAnalytics(int Followers,List<BusinessMetricDay> Days,bool Estimated);
public record BusinessCreated(string Id);
public record BusinessInviteCode(string Code);
public static class BusinessService
{
    public const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/business";
    public static Task<T> Call<T>(object data)=>HouseholdService.CallEndpointAsync<T>(Endpoint,data,true);
    public static async Task Track(string businessId,string eventName,string targetId=null){try{await Call<object>(new{action="track",businessId,@event=eventName,targetId});}catch{/* Analytics never block viewing or saving. */}}
    public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
}

public static class BusinessPlanImport
{
    public static List<BusinessPlanChoice> Validate(IEnumerable<BusinessPlanChoice> choices)
    {
        return CreatorPlanSelection.Validate(choices);
    }
    public static async Task Apply(IEnumerable<BusinessPlanChoice> choices,string account)
    {
        var selected=Validate(choices);AccountSession.Check(account);await RecipeService.CreateMealPlan(account);var plan=await RecipeService.GetMealPlan(account);AccountSession.Check(account);
        var imported=await SaveRecipes(selected,account);
        AccountSession.Check(account);var days=plan.Days.Select(d=>MealPlanning.Copy(d)).ToList();
        foreach(var choice in selected){var day=days.Single(d=>d.MealDayIndex==choice.Day);var local=imported[choice.Post.Id];var ids=MealPlanning.Ids(day,choice.Slot);if(!ids.Contains(local.Id))ids.Add(local.Id);MealPlanning.SetServings(day,choice.Slot,local.Id,int.Parse(choice.Servings));MealPlanning.Serialize(day);}
        await RecipeService.SaveMealPlanDays(days,plan.Id);AccountSession.Check(account);if(account!=null)_=AccountSession.SyncSafely();
    }
    public static async Task<Dictionary<string,Recipe>> SaveRecipes(IEnumerable<BusinessPlanChoice> choices,string account)
    {
        var selected=Validate(choices);AccountSession.Check(account);var available=await RecipeService.GetAllRecipes();AccountSession.Check(account);var imported=new Dictionary<string,Recipe>();
        foreach(var choice in selected){
            AccountSession.Check(account);var key="business-copy-"+choice.Post.Id;
            var local=available.FirstOrDefault(r=>r.FirebaseUID==account&&r.CloudKey==key);
            if(local==null){local=choice.Post.PrivateCopy(account);local.CloudKey=key;await RecipeService.AddRecipe(local);available.Add(local);}
            imported[choice.Post.Id]=local;
        }
        AccountSession.Check(account);if(account!=null)_=AccountSession.SyncSafely();return imported;
    }
}
