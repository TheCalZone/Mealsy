using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
namespace RecipeApp.Tests;

public class ShoppingDragLayoutTests
{
    [Theory]
    [InlineData(0,2,true)]
    [InlineData(0,2,false)]
    [InlineData(2,0,true)]
    [InlineData(2,0,false)]
    [InlineData(1,2,false)]
    [InlineData(1,0,true)]
    public void AnimatedGapMatchesFinalOrderForUnequalRowHeights(int source,int target,bool after)
    {
        double[] heights={70,130,90};const double spacing=7;
        var items=Enumerable.Range(0,3).Select(i=>new ShoppingListItem{Name="Item "+i,Category="Other"}).ToList();
        var ordered=ShoppingItemOrder.Drop(items,null,items[source].Id,items[target].Id,after,items.Select(i=>i.Id));
        var geometry=ShoppingDragLayout.Calculate(3,source,target,after,heights[source],heights[target],spacing);
        Assert.Equal(ordered.IndexOf(items[source].Id),geometry.Position);
        double Top(int index)=>heights.Take(index).Sum()+spacing*index;
        var expectedTop=ordered.Take(geometry.Position).Sum(id=>heights[items.FindIndex(item=>item.Id==id)]+spacing);
        Assert.Equal(expectedTop,Top(target)+geometry.GapOffset);
        for(var i=0;i<items.Count;i++)
        {
            if(i==source)continue;
            var expected=ordered.Take(ordered.IndexOf(items[i].Id)).Sum(id=>heights[items.FindIndex(item=>item.Id==id)]+spacing);
            Assert.Equal(expected,Top(i)+geometry.Offsets[i]);
        }
    }
    [Fact]
    public void InvalidPreviewBoundsCannotCreateAnInvalidGap()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>ShoppingDragLayout.Calculate(0,0,0,true,60,60,7));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ShoppingDragLayout.Calculate(3,0,4,true,60,60,7));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ShoppingDragLayout.Calculate(3,0,2,true,0,60,7));
    }
}
