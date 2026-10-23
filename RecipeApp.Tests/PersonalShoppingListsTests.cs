using RecipeApp.Services;
using RecipeApp.Models;
using Xunit;

public class PersonalShoppingListsTests
{
    public PersonalShoppingListsTests(){Preferences.Clear();ShoppingListStore.AccountId=()=>null;}
    [Fact]
    public void FreeLimitCountsMainAndDowngradeKeepsAllListsAccessible()
    {
        var first=PersonalShoppingLists.Create("Weekly",false);
        Assert.Throws<InvalidOperationException>(()=>PersonalShoppingLists.Create("Party",false));
        var extra=PersonalShoppingLists.Create("Party",true);
        PersonalShoppingLists.Rename(extra.Id,"Birthday");
        Assert.Equal(3,PersonalShoppingLists.All().Count);
        Assert.Throws<InvalidOperationException>(()=>PersonalShoppingLists.Create("More",false));
        PersonalShoppingLists.Delete(extra.Id);
        Assert.Equal(first.Id,PersonalShoppingLists.All()[1].Id);
        Assert.Throws<InvalidOperationException>(()=>PersonalShoppingLists.Delete("main"));
    }
    [Fact]
    public void ListNamesAndItemsAreAccountIsolatedAndWidgetTargetsExactList()
    {
        ShoppingListStore.AccountId=()=>"alice";
        var list=PersonalShoppingLists.Create("BBQ",false);
        var target=new WidgetListTarget("alice",null,"BBQ",list.Id);
        WidgetShoppingStore.Add(target,"alice",new ShoppingListItem{Name="Burgers"});
        Assert.Equal(PersonalShoppingLists.StorageKey(list.Id),target.StorageKey);
        Assert.Contains("PersonalListId="+list.Id,target.OpenRoute("alice"));
        Assert.Equal("Burgers",Assert.Single(ShoppingListDocument.Deserialize(Preferences.Get(target.StorageKey,"[]")).Items).Name);
        Assert.Equal("",Preferences.Get(ShoppingListStore.StateKey,""));
        ShoppingListStore.AccountId=()=>"bob";
        Assert.Single(PersonalShoppingLists.All());
        Assert.Throws<InvalidOperationException>(()=>WidgetShoppingStore.Add(target,"bob",new ShoppingListItem{Name="Wrong account"}));
        ShoppingListStore.AccountId=()=>"alice";
        PersonalShoppingLists.Delete(list.Id);
        Assert.Throws<InvalidOperationException>(()=>WidgetShoppingStore.Add(target,"alice",new ShoppingListItem{Name="Deleted list"}));
        Assert.Equal("",Preferences.Get(target.StorageKey,""));
    }
    [Fact]
    public void InvalidNamesAndCorruptCatalogNeverOverwriteStoredData()
    {
        Assert.Throws<InvalidOperationException>(()=>PersonalShoppingLists.Create(" ",false));
        Assert.Throws<InvalidOperationException>(()=>PersonalShoppingLists.Create("PERSONAL",false));
        var key=ShoppingListStore.StateKey+".lists";
        Preferences.Set(key,"broken");
        Assert.ThrowsAny<Exception>(()=>PersonalShoppingLists.Create("Safe",true));
        Assert.Equal("broken",Preferences.Get(key,""));
    }
}
