using System.Text.Json;
namespace RecipeApp.Services;

public record PersonalShoppingList(string Id,string Name);

/// <summary>Account-isolated list names and additional device lists. Main list keeps its existing cloud key.</summary>
public static class PersonalShoppingLists
{
    private static string CatalogKey=>ShoppingListStore.StateKey+".lists";
    public static List<PersonalShoppingList> All()
    {
        var json=Preferences.Get(CatalogKey,"");
        var lists=string.IsNullOrWhiteSpace(json)?new List<PersonalShoppingList>{new("main","Personal")}
            :JsonSerializer.Deserialize<List<PersonalShoppingList>>(json)??throw new InvalidOperationException("List names could not be loaded. Your lists have not been changed.");
        if(lists.Count==0||lists[0]==null||lists[0].Id!="main"||lists.Any(l=>l==null||string.IsNullOrWhiteSpace(l.Name)||!Valid(l.Id))||lists.Select(l=>l.Id).Distinct().Count()!=lists.Count)
            throw new InvalidOperationException("List names could not be loaded. Your lists have not been changed.");
        return lists;
    }
    private static bool Valid(string id)=>id=="main"||Guid.TryParseExact(id,"N",out _);
    public static string StorageKey(string id)
    {
        if(!Valid(id))throw new InvalidOperationException("Choose a valid shopping list.");
        return id=="main"?ShoppingListStore.StateKey:ShoppingListStore.StateKey+".list."+id;
    }
    public static PersonalShoppingList Create(string name,bool premium)
    {
        var lists=All();
        if(!premium&&lists.Count>=2)throw new InvalidOperationException("Free includes two personal lists. Premium lets you create more; existing lists always stay accessible.");
        var next=new PersonalShoppingList(Guid.NewGuid().ToString("N"),Name(name,lists));
        lists.Add(next);Save(lists);return next;
    }
    public static void Rename(string id,string name)
    {
        var lists=All();var index=lists.FindIndex(l=>l.Id==id);
        if(index<0)throw new InvalidOperationException("This list no longer exists.");
        lists[index]=lists[index] with {Name=Name(name,lists.Where(l=>l.Id!=id))};Save(lists);
    }
    public static void Delete(string id)
    {
        if(id=="main")throw new InvalidOperationException("Your main personal list cannot be deleted. You can clear its items instead.");
        var lists=All();if(lists.RemoveAll(l=>l.Id==id)==0)return;
        Save(lists);
        var key=StorageKey(id);
        foreach(var suffix in new[]{"",".synced",".recovery",".itemOrder",".sectionOrder"})Preferences.Remove(key+suffix);
    }
    private static string Name(string name,IEnumerable<PersonalShoppingList> lists)
    {
        name=name?.Trim();
        if(string.IsNullOrWhiteSpace(name)||name.Length>40)throw new InvalidOperationException("Choose a list name from 1–40 characters.");
        if(lists.Any(l=>l.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("A list already uses that name.");
        return name;
    }
    private static void Save(List<PersonalShoppingList> lists)=>Preferences.Set(CatalogKey,JsonSerializer.Serialize(lists));
}
