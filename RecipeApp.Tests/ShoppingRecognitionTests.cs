using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
public class ShoppingRecognitionTests
{
    [Theory]
    [InlineData("500g minced beef","Meat")][InlineData("2 chicken breasts","Meat")]
    [InlineData("fresh salmon fillets","Meat")][InlineData("aubergine","Produce")]
    [InlineData("cherry tomatoes","Produce")][InlineData("eggplant","Produce")]
    [InlineData("chicken stock","Pantry")][InlineData("peanut butter","Pantry")]
    [InlineData("coconut milk","Pantry")][InlineData("oat milk","Dairy")]
    [InlineData("frozen peas","Frozen")][InlineData("vegan beef mince","Chilled alternatives")]
    [InlineData("shampoo","Household")][InlineData("cat food","Pet food")]
    [InlineData("fresh basil","Produce")][InlineData("dried basil","Pantry")]
    [InlineData("butternut squash","Produce")][InlineData("fish sauce","Pantry")]
    [InlineData("fresh thyme","Produce")][InlineData("orange juice","Drinks")][InlineData("mystery product","Other")]
    public void WholeWordsAndCompoundFoodsChoosePracticalSections(string name,string category)=>Assert.Equal(category,ShoppingCategories.Resolve(name,"Other"));
    [Fact]
    public void ExplicitCategoriesAndSavedRowsAreNotReclassified()
    {
        Assert.Equal("My aisle",ShoppingCategories.Resolve("beef","My aisle"));
        var document=ShoppingListDocument.Deserialize("[{\"Name\":\"beef\",\"Category\":\"Other\"}]");Assert.Equal("Other",Assert.Single(document.Items).Category);
        var item=new ShoppingListItem{Name="500g minced beef"};document.Add(item);Assert.Equal("Meat",item.Category);Assert.Equal("minced beef",item.Name);Assert.Equal("500 g",item.Quantity);
    }
    [Fact]
    public void MovingItemsKeepsHiddenItemsAndOtherSectionsInPlace()
    {
        var a=new ShoppingListItem{Name="A",Category="Produce"};var hidden=new ShoppingListItem{Name="Hidden",Category="Produce",InBasket=true};var b=new ShoppingListItem{Name="B",Category="Produce"};var milk=new ShoppingListItem{Name="Milk",Category="Dairy"};var items=new[]{a,hidden,b,milk};
        var order=ShoppingItemOrder.Move(items,Array.Empty<Guid>(),b.Id,-1,new[]{a.Id,b.Id,milk.Id});
        Assert.Equal(new[]{b.Id,hidden.Id,a.Id,milk.Id},order);Assert.Equal(new[]{b,hidden,a,milk},ShoppingItemOrder.Sort(items,order));Assert.True(hidden.InBasket);
        var document=new ShoppingListDocument();document.Replace(items);var text=document.PlainText(itemOrder:order);Assert.True(text.IndexOf("B")<text.IndexOf("A"));
        Assert.Equal(items.Select(i=>i.Id),ShoppingItemOrder.Move(items,Array.Empty<Guid>(),milk.Id,-1,items.Select(i=>i.Id)));
        Assert.Equal(items.Select(i=>i.Id),ShoppingItemOrder.Move(items,Array.Empty<Guid>(),a.Id,-1,items.Select(i=>i.Id)));
        Assert.Empty(ShoppingItemOrder.Read("invalid"));
    }
}

public class ShoppingItemDragTests
{
    [Fact]
    public void HandleDropsReinsertVisibleItemsWithoutMovingHiddenOrOtherSectionItems()
    {
        var a=new ShoppingListItem{Name="A",Category="Produce"};var hidden=new ShoppingListItem{Name="Hidden",Category="Produce",InBasket=true};var b=new ShoppingListItem{Name="B",Category="Produce"};var c=new ShoppingListItem{Name="C",Category="Produce"};var milk=new ShoppingListItem{Name="Milk",Category="Dairy"};var items=new[]{a,hidden,b,c,milk};var shown=new[]{a.Id,b.Id,c.Id,milk.Id};
        Assert.Equal(new[]{b.Id,hidden.Id,c.Id,a.Id,milk.Id},ShoppingItemOrder.Drop(items,null,a.Id,c.Id,true,shown));
        Assert.Equal(new[]{c.Id,hidden.Id,a.Id,b.Id,milk.Id},ShoppingItemOrder.Drop(items,null,c.Id,a.Id,false,shown));
        Assert.Equal(items.Select(item=>item.Id),ShoppingItemOrder.Drop(items,null,a.Id,milk.Id,true,shown));
        Assert.Equal(items.Select(item=>item.Id),ShoppingItemOrder.Drop(items,null,hidden.Id,a.Id,true,shown));
        Assert.True(hidden.InBasket);Assert.Equal("A",a.Name);
    }
}
