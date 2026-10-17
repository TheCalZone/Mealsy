namespace RecipeApp.Services;

public static class PremiumAccess
{
    public static async Task OpenWeeks()
    {
        if(AccountSession.UserId==null){await Require("Reusable weekly meal-plan templates");return;}
        await AppNavigation.Open(new RecipeApp.Views.SavedWeeksPage());
    }
    public static async Task OpenShopping()
    {
        if(AccountSession.UserId==null){await Require("Saved shopping trips");return;}
        await AppNavigation.Open(new RecipeApp.Views.SavedShoppingTripsPage());
    }
    public static bool HasCached => PlanService.AdFree && PremiumEntitlement.IsActive(PlanService.Cached,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    public static async Task<bool> Require(string feature)
    {
        var account=AccountSession.UserId;
        if(account!=null)
        {
            try{var plan=await PlanService.RefreshAsync();AccountSession.Check(account);if(PremiumEntitlement.IsActive(plan,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))return true;}
            catch(Exception){AccountSession.Check(account);if(HasCached)return true;throw new InvalidOperationException("Your Premium access could not be checked. Connect and retry; your saved recipes and libraries are kept.");}
        }
        if(await Shell.Current.DisplayAlert("Mealsy Premium",feature+" is included in Premium, for solo cooks and households. Basic recipes, shopping and meal planning remain free.\n\n"+SubscriptionPricing.Summary+" at the UK reference price.","View Premium","Keep using free"))await AppNavigation.Open(new RecipeApp.Views.PlanPage());
        return false;
    }
}
