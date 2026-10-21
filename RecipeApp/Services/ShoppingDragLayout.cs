namespace RecipeApp.Services;

/// <summary>Geometry shared by item and section previews, matching insertion on release.</summary>
public static class ShoppingDragLayout
{
    public record Preview(int Position,IReadOnlyList<double> Offsets,double GapOffset);
    public static Preview Calculate(int count,int source,int target,bool after,double sourceHeight,double targetHeight,double spacing)
    {
        if(source<0||source>=count||target<0||target>=count||sourceHeight<=0||targetHeight<=0||spacing<0)
            throw new ArgumentOutOfRangeException(nameof(source));
        var position=target+(after?1:0);if(position>source)position--;
        var extent=sourceHeight+spacing;
        var offsets=Enumerable.Range(0,count).Select(index=>
            position>source&&index>source&&index<=position?-extent:
            position<source&&index>=position&&index<source?extent:0).ToArray();
        return new(position,offsets,(after?targetHeight+spacing:0)-(target>source?extent:0));
    }
}
