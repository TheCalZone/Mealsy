using Android.Views;
using RecipeApp.Services;
using MauiView=Microsoft.Maui.Controls.View;
using NativeView=Android.Views.View;

namespace RecipeApp.Platforms.Android;

/// <summary>Shared immediate-handle dragging for shopping items and section cards.</summary>
internal sealed class ShoppingDirectDrag
{
    private record Registered(WeakReference<MauiView> View,Func<MauiView,string> Key);
    private readonly List<Registered> rows=new();
    private readonly Func<bool> allowed;
    private readonly Func<string,string,bool> canDrop;
    private readonly Action<string,string,bool> drop;
    private readonly Func<string> context;
    private string capturedContext;
    private ShoppingDragPreview preview;
    private string source,account,target;
    private bool after;
    public bool IsDragging=>source!=null;
    public ShoppingDirectDrag(Func<bool> allowed,Func<string,string,bool> canDrop,Action<string,string,bool> drop,Func<string> context=null){this.allowed=allowed;this.canDrop=canDrop;this.drop=drop;this.context=context??(()=>"");}
    public void Cancel(){source=null;target=null;preview?.Dispose();preview=null;}
    public void Register(MauiView row,Func<MauiView,string> key)
    {
        rows.RemoveAll(r=>!r.View.TryGetTarget(out _));rows.Add(new(new(row),key));
    }
    private List<ShoppingDragPreview.Row> Measure()=>rows.Select(r=>r.View.TryGetTarget(out var view)?ShoppingDragPreview.Measure(r.Key(view),view):null).Where(r=>r!=null).ToList();
    public void Attach(MauiView handle,Func<string> key)
    {
        NativeView native=null;
        var pressed=false;float startX=0,startY=0;string candidate=null;
        void Release(){pressed=false;native?.Parent?.RequestDisallowInterceptTouchEvent(false);}
        void Touch(object sender,NativeView.TouchEventArgs e)
        {
            var motion=e.Event;if(motion==null)return;
            try
            {
                switch(motion.ActionMasked)
                {
                    case MotionEventActions.Down:
                        Cancel();candidate=null;
                        if(!allowed()||string.IsNullOrEmpty(key())){e.Handled=false;return;}
                        pressed=true;candidate=key();account=AccountSession.UserId;capturedContext=context();startX=motion.RawX;startY=motion.RawY;
                        native.Parent?.RequestDisallowInterceptTouchEvent(true);e.Handled=true;return;
                    case MotionEventActions.Move:
                        e.Handled=pressed;if(!pressed)return;
                        if(!allowed()||account!=AccountSession.UserId||capturedContext!=context()||key()!=candidate){Release();Cancel();return;}
                        var slop=ViewConfiguration.Get(native.Context).ScaledTouchSlop;
                        if(!IsDragging&&Math.Abs(motion.RawY-startY)<slop&&Math.Abs(motion.RawX-startX)<slop)return;
                        var measured=Measure();
                        if(!IsDragging)
                        {
                            var row=measured.FirstOrDefault(r=>r.Key==candidate);if(row==null){Release();Cancel();return;}
                            source=candidate;preview=new ShoppingDragPreview(row,startY);
                        }
                        var valid=measured.Where(r=>r.Key==source||canDrop(source,r.Key)).OrderBy(r=>r.Top).ToList();
                        var hit=valid.FirstOrDefault(r=>r.Key!=source&&motion.RawX>=r.Left&&motion.RawX<=r.Left+r.Width&&motion.RawY>=r.Top&&motion.RawY<=r.Top+r.Height);
                        // Margins between cards still select the closest insertion boundary.
                        if(hit==null&&valid.Count>0&&motion.RawX>=valid[0].Left&&motion.RawX<=valid[0].Left+valid[0].Width)
                            hit=valid.Where(r=>r.Key!=source).OrderBy(r=>Math.Abs(motion.RawY-(r.Top+r.Height/2))).FirstOrDefault(r=>Math.Abs(motion.RawY-(r.Top+r.Height/2))<=r.Height/2+12*native.Resources.DisplayMetrics.Density);
                        target=hit?.Key;after=hit!=null&&motion.RawY>hit.Top+hit.Height/2;
                        preview.Update(motion.RawY,valid,hit,after);
                        var parent=native.Parent;
                        while(parent!=null&&parent is not global::AndroidX.RecyclerView.Widget.RecyclerView&&parent is not global::Android.Widget.ScrollView)parent=parent.Parent;
                        if(parent is NativeView viewport)
                        {
                            var position=new int[2];viewport.GetLocationOnScreen(position);var edge=48*native.Resources.DisplayMetrics.Density;
                            var delta=motion.RawY<position[1]+edge?-24:motion.RawY>position[1]+viewport.Height-edge?24:0;
                            if(delta!=0)viewport.ScrollBy(0,delta);
                        }
                        return;
                    case MotionEventActions.Up:
                        e.Handled=pressed;
                        var commit=pressed&&IsDragging&&target!=null&&allowed()&&account==AccountSession.UserId&&capturedContext==context()&&key()==candidate&&canDrop(source,target);
                        var from=source;var to=target;var insertAfter=after;
                        Release();Cancel();if(commit)drop(from,to,insertAfter);return;
                    case MotionEventActions.Cancel: e.Handled=pressed;Release();Cancel();return;
                }
                e.Handled=pressed;
            }
            catch(Exception error){Release();Cancel();e.Handled=true;_=Shell.Current.DisplayAlert("Shopping order",error is InvalidOperationException?error.Message:"Couldn't finish moving. Please try again.",AppLocalization.Text("OK"));}
        }
        void Connect(){if(native!=null)native.Touch-=Touch;pressed=false;native=handle.Handler?.PlatformView as NativeView;if(native!=null)native.Touch+=Touch;}
        handle.HandlerChanging+=(_,_)=>{if(native!=null)native.Touch-=Touch;Release();if(candidate!=null&&source==candidate)Cancel();native=null;};
        handle.HandlerChanged+=(_,_)=>Connect();Connect();
    }
}
