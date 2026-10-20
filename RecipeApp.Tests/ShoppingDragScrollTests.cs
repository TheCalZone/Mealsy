using RecipeApp.Services;
using Xunit;
namespace RecipeApp.Tests;

public class ShoppingDragScrollTests
{
    [Theory]
    [InlineData(0,-180)]
    [InlineData(32,-112.5)]
    [InlineData(300,0)]
    [InlineData(568,112.5)]
    [InlineData(600,180)]
    [InlineData(-1,0)]
    [InlineData(601,0)]
    public void SpeedIsGentleDirectionalAndStopsAwayFromEdges(double y,double speed)
        =>Assert.Equal(speed,ShoppingDragScroll.Speed(y,600));
    [Fact]
    public void SmallViewportsHaveSeparateEdgeZonesAndInvalidSizesDoNotScroll()
    {
        Assert.Equal(0,ShoppingDragScroll.Speed(45,90));
        Assert.Equal(-180,ShoppingDragScroll.Speed(0,90));
        Assert.Equal(180,ShoppingDragScroll.Speed(90,90));
        Assert.Equal(0,ShoppingDragScroll.Speed(0,0));
        Assert.Equal(0,ShoppingDragScroll.Speed(double.NaN,600));
        Assert.Equal(0,ShoppingDragScroll.Speed(50,double.PositiveInfinity));
    }
    [Theory]
    [InlineData("a",0)]
    [InlineData("c",1)]
    [InlineData("e",2)]
    [InlineData("missing",-1)]
    public void RecycledSourcesRetainTheirLogicalInsertionSlot(string source,int expected)
        =>Assert.Equal(expected,ShoppingDragScroll.SourcePosition(new[]{"b","d"},new[]{"a","b","c","d","e"},source));
    [Fact]
    public void OffscreenSourcePreviewStillMatchesActualDropPosition()
    {
        var keys=new[]{"a","b","c","d","e"};
        var visible=new[]{"c","d","e"};
        var position=ShoppingDragScroll.SourcePosition(visible,keys,"a");
        var previewKeys=visible.ToList();previewKeys.Insert(position,"a");
        var geometry=ShoppingDragLayout.Calculate(previewKeys.Count,position,previewKeys.IndexOf("e"),true,80,80,7);
        Assert.Equal(3,geometry.Position);
        Assert.All(geometry.Offsets.Skip(1),offset=>Assert.Equal(-87d,offset));
    }
}
