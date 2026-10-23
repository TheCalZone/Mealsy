using RecipeApp.Models;
namespace RecipeApp.Services;

/// <summary>Exact-target storage shared by shopping shortcuts. Never falls back to another list.</summary>
public static class ShoppingDestinationStore
{
    public static void Check(WidgetListTarget target,string account)
    {
        if(target==null||!target.BelongsTo(account)||ShoppingListStore.AccountId()!=account)
            throw new InvalidOperationException("Your account changed. Choose a shopping list again.");
        if(target.HouseholdId==null&&!PersonalShoppingLists.All().Any(l=>l.Id==(target.PersonalListId??"main")))
            throw new InvalidOperationException("This shopping list was deleted. Choose another list.");
    }
    public static ShoppingListDocument Read(WidgetListTarget target,string account)
    {
        Check(target,account);var json=Preferences.Get(target.StorageKey,"");
        var list=string.IsNullOrWhiteSpace(json)?new ShoppingListDocument():ShoppingListDocument.Deserialize(json);
        if(target.HouseholdId==null&&target.PersonalListId==null)
        {
            var legacy=Preferences.Get(ShoppingListStore.LegacyKey,"");
            if(string.IsNullOrWhiteSpace(json)||legacy!=Preferences.Get(ShoppingListStore.ExportKey,""))list.MergeLegacyText(legacy);
        }
        return list;
    }
    public static void Save(WidgetListTarget target,string account,ShoppingListDocument list)
    {
        Check(target,account);
        if(target.HouseholdId==null&&target.PersonalListId==null)ShoppingListStore.Save(list);
        else Preferences.Set(target.StorageKey,list.Serialize());
        WidgetShoppingStore.NotifyChanged(target.StorageKey);
    }
}
