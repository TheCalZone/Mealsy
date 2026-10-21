using RecipeApp.Services;
using RecipeApp.Models;
using Xunit;
public class ShoppingSectionOrderTests
{
    [Fact]
    public void DefaultOrderIsAlphabeticalWithOtherLast()=>Assert.Equal(new[]{"Bakery","Dairy","Pet food","Produce","Other"},ShoppingSectionOrder.Sort(new[]{"Other","Produce","Dairy","Pet food","Bakery"},null));
    [Fact]
    public void SavedOrderSurvivesMissingSectionsAndNewSectionsFollowIt()
    {
        var saved=new[]{"Produce","Dairy","Bakery","Other"};
        Assert.Equal(new[]{"Produce","Bakery","Cleaning"},ShoppingSectionOrder.Sort(new[]{"Cleaning","Bakery","Produce"},saved));
        Assert.Equal(new[]{"Produce","Dairy","Bakery"},ShoppingSectionOrder.Sort(new[]{"Bakery","Dairy","Produce"},saved));
    }
    [Fact]
    public void PreferencesRecoverFromInvalidJsonAndNormalizeDuplicates()
    {
        Assert.Empty(ShoppingSectionOrder.Read("{broken"));Assert.Empty(ShoppingSectionOrder.Read("null"));
        Assert.Equal(new[]{"Produce","Dairy"},ShoppingSectionOrder.Read("[\"veg\",\"Produce\",null,\"\",\"Dairy\"]"));
    }
    [Fact]
    public void MovesKeepAllSectionsAndRespectBoundaries()
    {
        var original=new[]{"Bakery","Dairy","Other"};
        Assert.Equal(new[]{"Dairy","Bakery","Other"},ShoppingSectionOrder.Move(original,"Dairy",-1));
        Assert.Equal(original,ShoppingSectionOrder.Move(original,"Bakery",-1));Assert.Equal(original,ShoppingSectionOrder.Move(original,"Other",1));
        Assert.Equal(original,ShoppingSectionOrder.Move(original,"missing",1));Assert.Equal(original,ShoppingSectionOrder.Move(original,"Dairy",5));
    }
    [Fact]
    public void ExportUsesSectionOrderWithoutChangingItemsOrCheckmarks()
    {
        var document=new ShoppingListDocument();document.Add(new ShoppingListItem{Name="Milk",Category="Dairy",InBasket=true});document.Add(new ShoppingListItem{Name="Apple",Category="Produce"});var before=document.Serialize();
        var text=document.PlainText(sectionOrder:new[]{"Produce","Dairy"});Assert.True(text.IndexOf("Apple")<text.IndexOf("Milk"));
        Assert.DoesNotContain("Milk",document.PlainText(remainingOnly:true,sectionOrder:new[]{"Produce","Dairy"}));Assert.Equal(before,document.Serialize());
    }
}

public class ShoppingSectionDropTests
{
    [Fact]
    public void DragCanMoveAcrossSeveralSectionsInEitherDirection()
    {
        var names=new[]{"Bakery","Dairy","Meat","Produce","Other"};
        Assert.Equal(new[]{"Dairy","Meat","Produce","Bakery","Other"},ShoppingSectionOrder.Drop(names,"Bakery","Produce",true));
        Assert.Equal(new[]{"Produce","Bakery","Dairy","Meat","Other"},ShoppingSectionOrder.Drop(names,"Produce","Bakery",false));
        Assert.Equal(names,ShoppingSectionOrder.Drop(names,"Dairy","Dairy",true));Assert.Equal(names,ShoppingSectionOrder.Drop(names,"unknown","Dairy",false));
        Assert.Equal(new[]{"Bakery","Dairy","Meat","Produce","Other"},names);
    }
}
