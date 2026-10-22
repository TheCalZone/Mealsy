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
        if(account==null)
        {
            if(await Shell.Current.DisplayAlert("Start with a free Mealsy account",feature+" needs an account and Premium access. Creating an account is free and does not start a subscription. Basic personal recipes, shopping and meal planning stay free.","Create free account","Not now"))await AppNavigation.Open(new RecipeApp.Views.SignupPage());
            return false;
        }
        if(account!=null)
        {
            try{var plan=await PlanService.RefreshAsync();AccountSession.Check(account);if(PremiumEntitlement.IsActive(plan,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))return true;}
            catch(Exception){AccountSession.Check(account);if(HasCached)return true;throw new InvalidOperationException("Your Premium access could not be checked. Connect and retry; your saved recipes and libraries are kept.");}
        }
        if(await Shell.Current.DisplayAlert("Mealsy Premium",feature+" is included in Premium, for solo cooks and households. Basic recipes, shopping and meal planning remain free.\n\n"+"View Premium for the current app-store price and available plans."+(ClientPlatform.StorePurchasesAvailable?" Check whether Google Play offers you a 7-day free trial. Renewal terms appear before checkout.":""),"View Premium","Keep using free"))await AppNavigation.Open(new RecipeApp.Views.PlanPage());
        return false;
    }
}
