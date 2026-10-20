using Android.Graphics;
using Color=Android.Graphics.Color;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using RecipeApp.Services;
using NativeView=Android.Views.View;
using MauiView=Microsoft.Maui.Controls.View;

namespace RecipeApp.Platforms.Android;

/// <summary>A floating snapshot and a full-height insertion gap; no data changes until release.</summary>
internal sealed class ShoppingDragPreview : IDisposable
{
    internal record Row(string Key,MauiView View,NativeView Native,float Left,float Top,float Width,float Height);
    private readonly ViewGroup root;
    private readonly ViewGroupOverlay overlay;
    private readonly ImageView ghost;
    private readonly TextView gap;
    private readonly Bitmap bitmap;
    private readonly Row source;
    private readonly Dictionary<NativeView,(float Alpha,float Translation)> originals=new();
    private readonly Dictionary<NativeView,float> destinations=new();
    private readonly float fingerOffset;
    private readonly int[] rootPosition=new int[2];
    private string destination;
    public ShoppingDragPreview(Row source,float fingerY)
    {
        this.source=source;
        root=(ViewGroup)source.Native.RootView;
        overlay=(ViewGroupOverlay)root.Overlay;
        root.GetLocationOnScreen(rootPosition);
        fingerOffset=fingerY-source.Top;
        bitmap=Bitmap.CreateBitmap(Math.Max(1,source.Native.Width),Math.Max(1,source.Native.Height),Bitmap.Config.Argb8888);
        using(var canvas=new Canvas(bitmap))source.Native.Draw(canvas);
        ghost=new ImageView(source.Native.Context){Alpha=.96f};ghost.SetImageBitmap(bitmap);
        ghost.SetScaleType(ImageView.ScaleType.FitXy);
        ghost.Layout((int)source.Left-rootPosition[0],(int)source.Top-rootPosition[1],(int)(source.Left+source.Width)-rootPosition[0],(int)(source.Top+source.Height)-rootPosition[1]);
        ghost.ScaleX=ghost.ScaleY=1.025f;
        var background=new GradientDrawable();background.SetColor(Color.Argb(210,42,81,64));background.SetCornerRadius(12*source.Native.Resources.DisplayMetrics.Density);background.SetStroke(3,Color.Rgb(223,184,88));
        gap=new TextView(source.Native.Context){Text="Drop here",Gravity=GravityFlags.Center,TextSize=16,Visibility=ViewStates.Invisible};gap.SetTextColor(Color.White);gap.Background=background;
        overlay.Add(gap);overlay.Add(ghost);
        Remember(source.Native);source.Native.Alpha=0;
        source.Native.PerformHapticFeedback(FeedbackConstants.LongPress);
    }
    public static Row Measure(string key,MauiView view)
    {
        if(view.Handler?.PlatformView is not NativeView native||!native.IsShown||native.Width==0||native.Height==0)return null;
        // Animate the entire recycled cell, including its SwipeView wrapper.
        // Translating only the inner Border can clip it inside the original cell.
        var ancestor=native;
        while(ancestor.Parent is NativeView parent)
        {
            if(parent is global::AndroidX.RecyclerView.Widget.RecyclerView){native=ancestor;break;}
            ancestor=parent;
        }
        var location=new int[2];native.GetLocationOnScreen(location);
        // Ignore the preview animation when testing insertion positions; otherwise
        // sliding rows can repeatedly switch the destination under a still finger.
        return new(key,view,native,location[0],location[1]-native.TranslationY,native.Width,native.Height);
    }
    private void Remember(NativeView view){if(!originals.ContainsKey(view))originals[view]=(view.Alpha,view.TranslationY);}
    private void Slide(NativeView view,float offset)
    {
        Remember(view);
        if(destinations.TryGetValue(view,out var previous)&&Math.Abs(previous-offset)<.5f)return;
        destinations[view]=offset;
        view.Animate().Cancel();view.Animate().TranslationY(originals[view].Translation+offset).SetDuration(160).SetInterpolator(new global::Android.Views.Animations.DecelerateInterpolator()).Start();
    }
    public void Update(float fingerY,IReadOnlyList<Row> rows,Row target,bool after)
    {
        root.GetLocationOnScreen(rootPosition);
        ghost.TranslationY=fingerY-fingerOffset-source.Top;
        var ordered=rows.OrderBy(r=>r.Top).ToList();
        var from=ordered.FindIndex(r=>r.Key==source.Key);
        var to=target==null?-1:ordered.FindIndex(r=>r.Key==target.Key);
        if(from<0||to<0)
        {
            foreach(var row in ordered)if(row.Key!=source.Key)Slide(row.Native,0);
            gap.Visibility=ViewStates.Invisible;destination=null;return;
        }
        var density=source.Native.Resources.DisplayMetrics.Density;
        var spacing=from+1<ordered.Count?ordered[from+1].Top-ordered[from].Top-ordered[from].Height:
            from>0?ordered[from].Top-ordered[from-1].Top-ordered[from-1].Height:7*density;
        spacing=Math.Clamp(spacing,0,24*density);
        var geometry=ShoppingDragLayout.Calculate(ordered.Count,from,to,after,source.Height,target.Height,spacing);
        for(var index=0;index<ordered.Count;index++)
            if(ordered[index].Key!=source.Key)Slide(ordered[index].Native,(float)geometry.Offsets[index]);
        var top=target.Top+(float)geometry.GapOffset;
        gap.Measure(NativeView.MeasureSpec.MakeMeasureSpec((int)target.Width,MeasureSpecMode.Exactly),NativeView.MeasureSpec.MakeMeasureSpec((int)source.Height,MeasureSpecMode.Exactly));
        var outline=6*density;
        gap.Layout((int)(target.Left-outline)-rootPosition[0],(int)(top-outline)-rootPosition[1],(int)(target.Left+target.Width+outline)-rootPosition[0],(int)(top+source.Height+outline)-rootPosition[1]);
        gap.Visibility=ViewStates.Visible;
        var next=target.Key+":"+after;
        if(destination!=null&&destination!=next)source.Native.PerformHapticFeedback(FeedbackConstants.ClockTick);
        destination=next;
    }
    public void Dispose()
    {
        foreach(var pair in originals){pair.Key.Animate().Cancel();pair.Key.Alpha=pair.Value.Alpha;pair.Key.TranslationY=pair.Value.Translation;}
        overlay.Remove(ghost);overlay.Remove(gap);ghost.SetImageDrawable(null);ghost.Dispose();gap.Dispose();bitmap.Dispose();
    }
}
