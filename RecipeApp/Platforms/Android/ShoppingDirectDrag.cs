using Android.Views;
using Window=Android.Views.Window;
using RecipeApp.Services;
using MauiView=Microsoft.Maui.Controls.View;
using NativeView=Android.Views.View;

namespace RecipeApp.Platforms.Android;

/// <summary>Immediate icon dragging with continuous edge scrolling and window-level pointer capture.</summary>
internal sealed class ShoppingDirectDrag
{
    private record Registered(WeakReference<MauiView> View,Func<MauiView,string> Key);
    private readonly List<Registered> rows=new();
    private readonly Func<bool> allowed;
    private readonly Func<string,string,bool> canDrop;
    private readonly Action<string,string,bool> drop;
    private readonly Func<string> context;
    private readonly Func<IReadOnlyList<string>> order;
    private string capturedContext,source,account,target,candidate;
    private ShoppingDragPreview preview;
    private NativeView handle,viewport;
    private Func<string> handleKey;
    private Window window;
    private Window.ICallback previousCallback;
    private DragWindowCallback capture;
    private Microsoft.Maui.Dispatching.IDispatcherTimer timer;
    private readonly System.Diagnostics.Stopwatch clock=new();
    private double lastTick,remainder;
    private float startX,startY,pointerX,pointerY,density,slop;
    private bool pressed,after,suppressCancel;
    public bool IsDragging=>source!=null;
    public ShoppingDirectDrag(Func<bool> allowed,Func<string,string,bool> canDrop,Action<string,string,bool> drop,Func<string> context=null,Func<IReadOnlyList<string>> order=null)
    {this.allowed=allowed;this.canDrop=canDrop;this.drop=drop;this.context=context??(()=>"");this.order=order;}
    public void Cancel()
    {
        pressed=false;timer?.Stop();clock.Stop();remainder=0;
        handle?.Parent?.RequestDisallowInterceptTouchEvent(false);viewport?.Parent?.RequestDisallowInterceptTouchEvent(false);
        var old=capture;capture=null;
        if(old!=null)
        {
            if(window?.Callback is Java.Lang.Object current&&current.Handle==old.Handle)window.Callback=previousCallback;
            window?.DecorView?.Post(()=>old.Dispose());
        }
        source=null;target=null;candidate=null;preview?.Dispose();preview=null;
        handle=null;viewport=null;window=null;previousCallback=null;handleKey=null;
    }
    public void Register(MauiView row,Func<MauiView,string> key)
    {rows.RemoveAll(r=>!r.View.TryGetTarget(out _));rows.Add(new(new(row),key));}
    private List<ShoppingDragPreview.Row> Measure()=>rows.Select(r=>r.View.TryGetTarget(out var view)?ShoppingDragPreview.Measure(r.Key(view),view):null)
        .Where(r=>r!=null&&!string.IsNullOrEmpty(r.Key)).DistinctBy(r=>r.Key).ToList();
    private bool Current()=>pressed&&allowed()&&account==AccountSession.UserId&&capturedContext==context()&&(capture!=null||handleKey?.Invoke()==candidate);
    private void RefreshPreview()
    {
        var measured=Measure();
        var valid=measured.Where(r=>r.Key==source||canDrop(source,r.Key)).OrderBy(r=>r.Top).ToList();
        using var bounds=new global::Android.Graphics.Rect();
        var inside=viewport?.GetGlobalVisibleRect(bounds)==true&&bounds.Contains((int)pointerX,(int)pointerY);
        var hit=inside?valid.FirstOrDefault(r=>r.Key!=source&&pointerX>=r.Left&&pointerX<=r.Left+r.Width&&pointerY>=r.Top&&pointerY<=r.Top+r.Height):null;
        if(hit==null&&inside&&valid.Count>0&&pointerX>=valid[0].Left&&pointerX<=valid[0].Left+valid[0].Width)
        {
            var outsideRows=pointerY<valid[0].Top||pointerY>valid[^1].Top+valid[^1].Height;
            hit=valid.Where(r=>r.Key!=source).OrderBy(r=>Math.Abs(pointerY-(r.Top+r.Height/2)))
                .FirstOrDefault(r=>outsideRows||Math.Abs(pointerY-(r.Top+r.Height/2))<=r.Height/2+12*density);
        }
        target=hit?.Key;after=hit!=null&&pointerY>hit.Top+hit.Height/2;
        preview.Update(pointerY,valid,hit,after,order?.Invoke()??valid.Select(r=>r.Key).ToList());
    }
    private void Tick(object sender,EventArgs args)
    {
        try
        {
            if(!Current()||!IsDragging||viewport?.IsAttachedToWindow!=true){Cancel();return;}
            var now=clock.Elapsed.TotalSeconds;var elapsed=Math.Clamp(now-lastTick,0,.06);lastTick=now;
            using var bounds=new global::Android.Graphics.Rect();
            if(viewport.GetGlobalVisibleRect(bounds)&&pointerX>=bounds.Left&&pointerX<=bounds.Right)
            {
                var speed=ShoppingDragScroll.Speed((pointerY-bounds.Top)/density,bounds.Height()/density);
                if(speed!=0&&viewport.CanScrollVertically(Math.Sign(speed)))
                {
                    remainder+=speed*density*elapsed;var pixels=(int)remainder;remainder-=pixels;
                    if(pixels!=0)viewport.ScrollBy(0,pixels);
                }
                else remainder=0;
            }
            else remainder=0;
            RefreshPreview();
        }
        catch(Exception error){Fail(error);}
    }
    private void StartCapture(MotionEvent motion)
    {
        window=Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
        previousCallback=window?.Callback;
        if(previousCallback==null)throw new InvalidOperationException("Reopen the shopping list before moving this item.");
        // Clear the original view's touch target before it can be recycled. Further
        // pointer events belong to the drag, not to that original visible row.
        using(var cancel=MotionEvent.Obtain(motion))
        {
            cancel.Action=MotionEventActions.Cancel;suppressCancel=true;
            try{previousCallback.DispatchTouchEvent(cancel);}finally{suppressCancel=false;}
        }
        capture=new DragWindowCallback(previousCallback,Process,Cancel);window.Callback=capture;
        timer??=Microsoft.Maui.Controls.Application.Current.Dispatcher.CreateTimer();
        timer.Interval=TimeSpan.FromMilliseconds(32);timer.Tick-=Tick;timer.Tick+=Tick;
        clock.Restart();lastTick=0;remainder=0;timer.Start();
    }
    private bool Process(MotionEvent motion)
    {
        if(motion==null)return false;
        if(suppressCancel&&motion.ActionMasked==MotionEventActions.Cancel)return true;
        try
        {
            switch(motion.ActionMasked)
            {
                case MotionEventActions.Move:
                    if(!pressed)return false;
                    pointerX=motion.RawX;pointerY=motion.RawY;
                    if(!Current()){Cancel();return true;}
                    if(!IsDragging&&Math.Abs(pointerY-startY)<slop&&Math.Abs(pointerX-startX)<slop)return true;
                    if(!IsDragging)
                    {
                        var row=Measure().FirstOrDefault(r=>r.Key==candidate);if(row==null){Cancel();return true;}
                        source=candidate;preview=new ShoppingDragPreview(row,startY);StartCapture(motion);
                    }
                    RefreshPreview();return true;
                case MotionEventActions.Up:
                    if(!pressed)return false;
                    pointerX=motion.RawX;pointerY=motion.RawY;
                    if(IsDragging&&Current())RefreshPreview();
                    var commit=IsDragging&&target!=null&&Current()&&canDrop(source,target);
                    var from=source;var to=target;var insertAfter=after;
                    Cancel();if(commit)drop(from,to,insertAfter);return true;
                case MotionEventActions.Cancel:
                case MotionEventActions.PointerDown:
                case MotionEventActions.Outside:
                    var handled=pressed;Cancel();return handled;
            }
            return pressed;
        }
        catch(Exception error){Fail(error);return true;}
    }
    private void Fail(Exception error)
    {Cancel();_=Shell.Current.DisplayAlert("Shopping order",error is InvalidOperationException?error.Message:"Couldn't finish moving. Please try again.",AppLocalization.Text("OK"));}
    public void Attach(MauiView view,Func<string> key)
    {
        NativeView native=null;
        void Touch(object sender,NativeView.TouchEventArgs e)
        {
            var motion=e.Event;if(motion==null)return;
            if(suppressCancel&&motion.ActionMasked==MotionEventActions.Cancel){e.Handled=true;return;}
            if(motion.ActionMasked==MotionEventActions.Down)
            {
                Cancel();if(!allowed()||string.IsNullOrEmpty(key())){e.Handled=false;return;}
                handle=native;handleKey=key;candidate=key();account=AccountSession.UserId;capturedContext=context();
                pressed=true;pointerX=startX=motion.RawX;pointerY=startY=motion.RawY;density=native.Resources.DisplayMetrics.Density;slop=ViewConfiguration.Get(native.Context).ScaledTouchSlop;
                var parent=native.Parent;
                while(parent!=null&&parent is not global::AndroidX.RecyclerView.Widget.RecyclerView&&parent is not global::Android.Widget.ScrollView&&parent is not global::AndroidX.Core.Widget.NestedScrollView)parent=parent.Parent;
                viewport=parent as NativeView;
                native.Parent?.RequestDisallowInterceptTouchEvent(true);e.Handled=true;return;
            }
            e.Handled=Process(motion);
        }
        void Connect(){if(native!=null)native.Touch-=Touch;native=view.Handler?.PlatformView as NativeView;if(native!=null)native.Touch+=Touch;}
        view.HandlerChanging+=(_,_)=>{if(native!=null)native.Touch-=Touch;if(handle==native){if(capture==null)Cancel();else handle=null;}native=null;};
        view.HandlerChanged+=(_,_)=>Connect();Connect();
    }
    private sealed class DragWindowCallback : global::AndroidX.AppCompat.View.WindowCallbackWrapper
    {
        private readonly Func<MotionEvent,bool> touch;
        private readonly Action cancel;
        public DragWindowCallback(Window.ICallback callback,Func<MotionEvent,bool> touch,Action cancel):base(callback){this.touch=touch;this.cancel=cancel;}
        public override bool DispatchTouchEvent(MotionEvent motion)=>touch(motion)||base.DispatchTouchEvent(motion);
        public override void OnWindowFocusChanged(bool focused){if(!focused)cancel();base.OnWindowFocusChanged(focused);}
    }
}
