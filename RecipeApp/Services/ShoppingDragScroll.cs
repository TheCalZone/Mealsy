namespace RecipeApp.Services;

public static class ShoppingDragScroll
{
    // Coordinates and speed are in device-independent pixels.
    public static double Speed(double pointerY,double height)
    {
        if(!double.IsFinite(pointerY)||!double.IsFinite(height)||height<=0||pointerY<0||pointerY>height)return 0;
        var edge=Math.Min(64,height/3);
        if(pointerY<edge)return -(45+135*(edge-pointerY)/edge);
        if(pointerY>height-edge)return 45+135*(pointerY-height+edge)/edge;
        return 0;
    }
    public static int SourcePosition(IReadOnlyList<string> visible,IReadOnlyList<string> order,string source)
    {
        var index=order.ToList().IndexOf(source);
        if(index<0)return -1;
        var ranks=order.Select((key,rank)=>(key,rank)).ToDictionary(x=>x.key,x=>x.rank);
        return visible.Count(key=>ranks.TryGetValue(key,out var rank)&&rank<index);
    }
}
