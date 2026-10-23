using RecipeApp.Services;
using RecipeApp.Models;
using Xunit;
public class ShoppingDestinationStoreTests
{
    public ShoppingDestinationStoreTests(){Preferences.Clear();ShoppingListStore.AccountId=()=>"alice";}
    [Fact]
    public void RecipeAppendOnlyChangesChosenListAndCombinesAmounts()
    {
        var main=new WidgetListTarget("alice",null,"Personal");
        var named=PersonalShoppingLists.Create("Party",false);var target=new WidgetListTarget("alice",null,named.Name,named.Id);
        var mainList=new ShoppingListDocument();mainList.Add(new ShoppingListItem{Name="Eggs"});ShoppingDestinationStore.Save(main,"alice",mainList);
        var party=new ShoppingListDocument();party.Add(new ShoppingListItem{Name="Minced beef",Quantity="500 g",Category="Meat"});ShoppingDestinationStore.Save(target,"alice",party);
        var selected=ShoppingDestinationStore.Read(target,"alice");selected.AppendIngredients(new[]{new Ingredient{Name="Minced beef",Quantity="250 g",Tag="Meat"}});ShoppingDestinationStore.Save(target,"alice",selected);
        Assert.Equal("750 g",Assert.Single(ShoppingDestinationStore.Read(target,"alice").Items).Quantity);
        Assert.Equal("Eggs",Assert.Single(ShoppingDestinationStore.Read(main,"alice").Items).Name);
        Assert.Contains(named.Id,target.OpenRoute("alice"));
    }
    [Fact]
    public void DeletedDestinationAndChangedAccountNeverFallBackToMain()
    {
        var named=PersonalShoppingLists.Create("Party",false);var target=new WidgetListTarget("alice",null,named.Name,named.Id);PersonalShoppingLists.Delete(named.Id);
        Assert.Throws<InvalidOperationException>(()=>ShoppingDestinationStore.Read(target,"alice"));
        ShoppingListStore.AccountId=()=>"bob";
        Assert.Throws<InvalidOperationException>(()=>ShoppingDestinationStore.Save(new("alice",null,"Personal"),"alice",new()));
        Assert.Equal("missing",Preferences.Get(ShoppingListStore.StateKey,"missing"));
    }
    [Fact]
    public void SharedDestinationKeepsPersonalDataAndPreservesLegacyMainItems()
    {
        Preferences.Set(ShoppingListStore.LegacyKey,"Milk");
        var main=new WidgetListTarget("alice",null,"Personal");Assert.Contains(ShoppingDestinationStore.Read(main,"alice").Items,i=>i.Name=="Milk");
        var shared=new WidgetListTarget("alice","home","Family");var list=new ShoppingListDocument();list.Add(new ShoppingListItem{Name="Bread"});ShoppingDestinationStore.Save(shared,"alice",list);
        Assert.Equal("Bread",Assert.Single(ShoppingDestinationStore.Read(shared,"alice").Items).Name);
        Assert.Contains(ShoppingDestinationStore.Read(main,"alice").Items,i=>i.Name=="Milk");
    }
}
