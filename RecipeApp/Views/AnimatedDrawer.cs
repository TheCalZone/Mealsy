namespace RecipeApp.Views;

/// <summary>A clipped, height-animated drawer with a bounded scrollable interior.</summary>
[ContentProperty(nameof(DrawerContent))]
public class AnimatedDrawer:ContentView
{
    private readonly ScrollView scroll=new(){VerticalOptions=LayoutOptions.Start};
    private View drawerContent;
    private int revision;
    private double limit=220;
    public bool IsOpen {get;private set;}
    public event EventHandler OpenChanged;
    public View DrawerContent
    {
        get=>drawerContent;
        set{drawerContent=value;scroll.Content=value;}
    }
    public double MaximumDrawerHeight
    {
        get=>limit;
        set{limit=Math.Max(48,value);scroll.MaximumHeightRequest=Math.Max(1,limit-Padding.VerticalThickness);}
    }
    public AnimatedDrawer()
    {
        Padding=new Thickness(0,8,0,0);IsClippedToBounds=true;IsVisible=false;InputTransparent=true;
        scroll.MaximumHeightRequest=limit-Padding.VerticalThickness;Content=scroll;
        Unloaded+=(_,_)=>this.AbortAnimation("DrawerHeight");
    }
    private void Settle(){IsVisible=IsOpen;HeightRequest=-1;InputTransparent=!IsOpen;}
    public async Task SetOpenAsync(bool open,bool animate=true)
    {
        if(open==IsOpen&&!this.AnimationIsRunning("DrawerHeight"))return;
        var version=++revision;
        this.AbortAnimation("DrawerHeight");
        var from=IsVisible?Height:0;
        IsOpen=open;OpenChanged?.Invoke(this,EventArgs.Empty);
        try
        {
            var width=Width>0?Width:(Parent as VisualElement)?.Width??0;
            if(!animate||!MotionEnabled()||width<=0||drawerContent==null){Settle();return;}
            var measured=drawerContent.Measure(Math.Max(1,width-Padding.HorizontalThickness),double.PositiveInfinity,MeasureFlags.IncludeMargins).Request.Height+Padding.VerticalThickness;
            if(!double.IsFinite(measured)||measured<=0){Settle();return;}
            var to=open?Math.Min(limit,measured):0;
            if(!double.IsFinite(from)||from<0)from=0;
            HeightRequest=from;IsVisible=true;InputTransparent=true;
            var finished=new TaskCompletionSource<bool>();
            new Animation(height=>HeightRequest=height,from,to).Commit(this,"DrawerHeight",16,180,Easing.CubicOut,(_,cancelled)=>finished.TrySetResult(!cancelled));
            await finished.Task;
        }
        catch{ }
        finally{if(version==revision)Settle();}
    }
    private static bool MotionEnabled()
    {
        try
        {
#if ANDROID
            return global::Android.Provider.Settings.Global.GetFloat(global::Android.App.Application.Context.ContentResolver,"animator_duration_scale",1f)>0;
#elif IOS
            return !UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
            return true;
#endif
        }
        catch{return false;}
    }
}
