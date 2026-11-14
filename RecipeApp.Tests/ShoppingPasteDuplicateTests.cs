using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;

public class ShoppingPasteDuplicateTests
{
    [Fact]
    public void PreviewFindsExistingAndWithinPasteMatchesWithoutChangingList()
    {
        var list=new ShoppingListDocument();
        list.Add(new ShoppingListItem{Name="Minced beef",Quantity="500 g",Category="Meat",Note="Fresh",InBasket=true});
        var before=list.Serialize();
        Assert.Equal(new[]{"MINCED BEEF","eggs"},list.PasteDuplicates("☐ 250g MINCED BEEF\nEggs\neggs"));
        Assert.Equal(before,list.Serialize());
    }
    [Fact]
    public void KeepBothPreservesSeparateAmountsCheckmarksAndOneUndo()
    {
        var list=new ShoppingListDocument();list.Add(new ShoppingListItem{Name="Milk",Quantity="1 l",Category="Dairy",InBasket=true});
        var before=list.Serialize();
        list.AppendText("Milk — 500 ml\nMilk — 250 ml\nBread",true);
        Assert.Equal(4,list.Items.Count);Assert.True(list.Items[0].InBasket);
        Assert.Equal(new[]{"1 l","500 ml","250 ml"},list.Items.Take(3).Select(item=>item.Quantity));
        Assert.Equal(4,list.Items.Select(item=>item.Id).Distinct().Count());
        list.Undo();Assert.Equal(before,list.Serialize());list.Redo();Assert.Equal(4,list.Items.Count);
    }
    [Fact]
    public void SkipDuplicatesKeepsFirstNewRowWithoutIncreasingExistingQuantities()
    {
        var list=new ShoppingListDocument();list.Add(new ShoppingListItem{Name="Milk",Quantity="1 l",Category="Dairy"});
        list.AppendText("milk — 500 ml\nEggs — 6\neggs — 12\nBread",false);
        Assert.Equal(new[]{"Milk","Eggs","Bread"},list.Items.Select(item=>item.Name));
        Assert.Equal("1 l",list.Items[0].Quantity);Assert.Equal("6",list.Items[1].Quantity);
    }
    [Fact]
    public void DifferentIngredientsAreNotSilentlyTreatedAsTheSame()
    {
        var list=new ShoppingListDocument();list.AppendText("Minced beef\nMinced lamb",true);
        Assert.Empty(list.PasteDuplicates("Beef stock\nGarlic"));
    }
}
