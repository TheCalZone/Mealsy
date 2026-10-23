using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;

public static class ShoppingListDestinations
{
    public static async Task<WidgetListTarget> ChooseAsync(string title,bool personalOnly=false)
    {
        var account=AccountSession.UserId;
        var choices=PersonalShoppingLists.All().Select(l=>new WidgetListTarget(account,null,l.Name,l.Id=="main"?null:l.Id)).ToList();
        var labels=choices.Select(t=>t.Name+(t.PersonalListId==null?" · Personal":" · Personal, this device")).ToList();
        if(!personalOnly&&account!=null)
        {
            try {
                var household=await HouseholdService.StatusAsync();AccountSession.Check(account);
                if(household is {FamilyEnabled:true,HouseholdId:not null}){
                    choices.Insert(0,new(account,household.HouseholdId,household.Name));
                    labels.Insert(0,household.Name+(household.Restricted?" · Household, request additions":" · Household"));
                }
            }
            catch(HouseholdException){
                AccountSession.Check(account);
                await Shell.Current.DisplayAlert("Household unavailable","Your household list could not be checked. You can choose a personal list now, or cancel and try again when connected.","OK");
            }
        }
        AccountSession.Check(account);
        var names=labels.Select((label,i)=>$"{i+1}. {label}").ToArray();
        var selected=await Shell.Current.DisplayActionSheet(title,AppLocalization.Text("Cancel"),null,names);
        AccountSession.Check(account);var index=Array.IndexOf(names,selected);
        return index<0?null:choices[index];
    }
    // Fetch the household only when chosen. Rebase unsynced local edits before adding anything.
    public static async Task<ShoppingListDocument> ReadAsync(WidgetListTarget target)
    {
        var account=AccountSession.UserId;ShoppingDestinationStore.Check(target,account);
        if(target.HouseholdId!=null)
        {
            var household=await HouseholdService.StatusAsync(true);AccountSession.Check(account);
            if(household.HouseholdId!=target.HouseholdId||!household.FamilyEnabled)throw new InvalidOperationException("Access to the chosen household list has ended. Choose a list again.");
            var response=await HouseholdService.CallAsync<ShoppingSyncResult>(new{action="read",scope="household",householdId=target.HouseholdId},bypassReadCache:true);
            AccountSession.Check(account);ShoppingDestinationStore.Check(target,account);
            var server=JsonSerializer.Serialize(response.Items);
            var local=ShoppingDestinationStore.Read(target,account).Serialize();
            var merged=ShoppingListSync.Reapply(Preferences.Get(target.StorageKey+".synced","[]"),local,server);
            Preferences.Set(target.StorageKey+".synced",server);
            ShoppingDestinationStore.Save(target,account,merged);return merged;
        }
        return ShoppingDestinationStore.Read(target,account);
    }
    public static async Task AddAsync(WidgetListTarget target,IEnumerable<Ingredient> ingredients,bool replace=false)
    {
        var account=AccountSession.UserId;
        if(replace&&target.HouseholdId!=null)throw new InvalidOperationException("Shared household lists cannot be replaced from this shortcut.");
        var list=await ReadAsync(target);AccountSession.Check(account);
        if(replace)list.Replace(ShoppingListDocument.FromIngredients(ingredients));
        else if(target.HouseholdId!=null&&(await HouseholdService.StatusAsync()).Restricted)
        {
            AccountSession.Check(account);
            foreach(var item in ShoppingListDocument.FromIngredients(ingredients))list.Add(item);
        }
        else list.AppendIngredients(ingredients);
        ShoppingDestinationStore.Save(target,account,list);
        await AppNavigation.ShoppingList(target.OpenRoute(account));
    }
    public static IEnumerable<Ingredient> Ingredients(IEnumerable<ShoppingListItem> items)=>items.Select(i=>new Ingredient{Name=i.Name,Quantity=i.Quantity,Note=i.Note,Tag=i.Category});
}
