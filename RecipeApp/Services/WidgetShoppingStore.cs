using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;
public record WidgetListTarget(string Account,string HouseholdId,string Name,string PersonalListId=null)
{
    public bool BelongsTo(string account)=>Account==account;
    public string OpenRoute(string account) {
        if(!BelongsTo(account))throw new InvalidOperationException("Choose a list for the account currently signed in.");
        return HouseholdId==null?(PersonalListId==null?"//ShoppingListPage?OpenPersonal=True":"//ShoppingListPage?PersonalListId="+Uri.EscapeDataString(PersonalListId)):"//ShoppingListPage?OpenHouseholdId="+Uri.EscapeDataString(HouseholdId);
    }
    public string StorageKey=>HouseholdId==null ? "ShoppingList.v2"+(Account==null ? "" : ".user."+Account)+(PersonalListId==null?"":".list."+PersonalListId) : $"ShoppingList.household.{Account}.{HouseholdId}";
}
public static class WidgetShoppingStore
{
    public static event Action<string> Changed;
    public static void NotifyChanged(string key)=>Changed?.Invoke(key);
    private static string Key(int id)=>"Mealsy.widget."+id;
    public static WidgetListTarget Target(int id){try{return JsonSerializer.Deserialize<WidgetListTarget>(Preferences.Get(Key(id),"null"));}catch(JsonException){return null;}}
    public static void SetTarget(int id,WidgetListTarget target)=>Preferences.Set(Key(id),JsonSerializer.Serialize(target));
    public static void Delete(int id)=>Preferences.Remove(Key(id));
    public static string Add(WidgetListTarget target,string account,ShoppingListItem item)
    {
        if(target.HouseholdId==null&&ShoppingListStore.AccountId()!=account)throw new InvalidOperationException("Your account changed. Reopen quick add.");
        if(!target.BelongsTo(account))throw new InvalidOperationException("Choose a list for the account currently signed in.");
        if(item==null)throw new InvalidOperationException("Enter an item.");
        item.Quantity??="";item.Note??="";
        if(string.IsNullOrWhiteSpace(item.Name)||item.Name.Length>200||item.Quantity.Length>200||item.Note.Length>500)throw new InvalidOperationException("Enter an item name and keep its details short.");
        if(target.PersonalListId!=null&&!PersonalShoppingLists.All().Any(l=>l.Id==target.PersonalListId&&l.Id!="main"))throw new InvalidOperationException("This list was deleted. Choose another list in widget settings.");
        var json=Preferences.Get(target.StorageKey,"");var document=string.IsNullOrWhiteSpace(json)?new ShoppingListDocument():ShoppingListDocument.Deserialize(json);
        if(target.HouseholdId==null&&target.PersonalListId==null)
        {
            var legacy=Preferences.Get(ShoppingListStore.LegacyKey,"");
            if(string.IsNullOrWhiteSpace(json)||legacy!=Preferences.Get(ShoppingListStore.ExportKey,""))document.MergeLegacyText(legacy);
        }
        // Stable item IDs make a retry safe even if storage failed after writing the main JSON.
        if(!document.Items.Any(i=>i.Id==item.Id))document.Add(item);
        if(target.HouseholdId==null&&target.PersonalListId==null)ShoppingListStore.Save(document);else Preferences.Set(target.StorageKey,document.Serialize());
        NotifyChanged(target.StorageKey);
        return document.Serialize();
    }
}
