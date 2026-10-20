using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;
namespace RecipeApp.Views;

/// <summary>Only the handle starts a drag; checkboxes, item taps and swipes keep their actions.</summary>
internal sealed class ShoppingItemDrag
{
    private readonly ShoppingListViewModel model;
    private readonly Func<bool> allowed;
    private Guid? source;
    private string account,context;
    private readonly List<View> highlights=new();
    private readonly List<WeakReference<View>> rows=new();
    public bool IsDragging=>source.HasValue;
    public ShoppingItemDrag(ShoppingListViewModel model,Func<bool> allowed){this.model=model;this.allowed=allowed;}
    private bool Current()=>source.HasValue&&allowed()&&account==AccountSession.UserId&&context==model.SectionOrderContext;
    public void Cancel(){source=null;foreach(var view in highlights){view.Opacity=1;view.TranslationY=0;}highlights.Clear();}
    public void AttachHandle(View handle)
    {
#if ANDROID
        AttachDirectTouch(handle);
#else
        var drag=new DragGestureRecognizer{CanDrag=true};
        drag.DragStarting+=(_,e)=>{
            Cancel();if(!allowed()||handle.BindingContext is not ShoppingListItem item){e.Cancel=true;return;}
            source=item.Id;account=AccountSession.UserId;context=model.SectionOrderContext;e.Data.Text=item.Id.ToString();handle.Opacity=.45;highlights.Add(handle);
        };
        drag.DropCompleted+=(_,_)=>Cancel();handle.GestureRecognizers.Add(drag);
#endif
    }
#if ANDROID
    private void AttachDirectTouch(View handle)
    {
        global::Android.Views.View native=null;
        float startX=0,startY=0;
        bool pressed=false;
        Guid? target=null;
        bool after=false;
        Guid? candidate=null;
        void Touch(object sender,global::Android.Views.View.TouchEventArgs e)
        {
            var motion=e.Event;
            if(motion==null)return;
            try
            {
                switch(motion.ActionMasked)
                {
                    case global::Android.Views.MotionEventActions.Down:
                        Cancel();target=null;candidate=null;
                        if(!allowed()||handle.BindingContext is not ShoppingListItem item){e.Handled=false;return;}
                        pressed=true;candidate=item.Id;startX=motion.RawX;startY=motion.RawY;
                        account=AccountSession.UserId;context=model.SectionOrderContext;
                        native.Parent?.RequestDisallowInterceptTouchEvent(true);
                        e.Handled=true;return;
                    case global::Android.Views.MotionEventActions.Move:
                        if(!pressed){e.Handled=false;return;}
                        e.Handled=true;
                        if(!allowed()||account!=AccountSession.UserId||context!=model.SectionOrderContext||handle.BindingContext is not ShoppingListItem current||current.Id!=candidate){pressed=false;Cancel();return;}
                        var slop=global::Android.Views.ViewConfiguration.Get(native.Context).ScaledTouchSlop;
                        if(!source.HasValue&&Math.Abs(motion.RawY-startY)<slop&&Math.Abs(motion.RawX-startX)<slop)return;
                        source??=candidate;
                        foreach(var highlighted in highlights){highlighted.Opacity=1;highlighted.TranslationY=0;}
                        highlights.Clear();handle.Opacity=.45;highlights.Add(handle);target=null;
                        // Only visible row bounds participate; recycled rows use their current binding.
                        foreach(var reference in rows)
                        {
                            if(!reference.TryGetTarget(out var row)||row.BindingContext is not ShoppingListItem other||!model.CanDropItem(source.Value,other.Id)||other.Id==source)continue;
                            if(row.Handler?.PlatformView is not global::Android.Views.View platform||!platform.IsShown)continue;
                            var position=new int[2];platform.GetLocationOnScreen(position);
                            if(motion.RawX<position[0]||motion.RawX>position[0]+platform.Width||motion.RawY<position[1]||motion.RawY>position[1]+platform.Height)continue;
                            target=other.Id;after=motion.RawY>position[1]+platform.Height/2f;
                            row.Opacity=.65;row.TranslationY=after?-4:4;highlights.Add(row);break;
                        }
                        // Move the viewport while dragging near its edge, without requiring release.
                        var parent=native.Parent;
                        while(parent!=null&&parent is not global::AndroidX.RecyclerView.Widget.RecyclerView)parent=parent.Parent;
                        if(parent is global::AndroidX.RecyclerView.Widget.RecyclerView recycler)
                        {
                            var location=new int[2];recycler.GetLocationOnScreen(location);
                            var edge=48*native.Resources.DisplayMetrics.Density;
                            if(motion.RawY<location[1]+edge)recycler.ScrollBy(0,-24);
                            else if(motion.RawY>location[1]+recycler.Height-edge)recycler.ScrollBy(0,24);
                        }
                        return;
                    case global::Android.Views.MotionEventActions.Up:
                        e.Handled=pressed;
                        var move=pressed&&Current()&&source.HasValue&&target.HasValue;
                        var from=source;var to=target;var insertAfter=after;
                        pressed=false;native.Parent?.RequestDisallowInterceptTouchEvent(false);Cancel();target=null;
                        if(move)model.DropItem(from.Value,to.Value,insertAfter);
                        return;
                    case global::Android.Views.MotionEventActions.Cancel:
                        e.Handled=pressed;pressed=false;native.Parent?.RequestDisallowInterceptTouchEvent(false);Cancel();target=null;return;
                }
                e.Handled=pressed;
            }
            catch(Exception error)
            {
                pressed=false;native.Parent?.RequestDisallowInterceptTouchEvent(false);Cancel();e.Handled=true;
                _=Shell.Current.DisplayAlert("Item order",error is InvalidOperationException?error.Message:"Couldn't move this item. Please try again.",AppLocalization.Text("OK"));
            }
        }
        void Connect()
        {
            if(native!=null)native.Touch-=Touch;
            pressed=false;native=handle.Handler?.PlatformView as global::Android.Views.View;
            if(native!=null)native.Touch+=Touch;
        }
        handle.HandlerChanging+=(_,_)=>{if(native!=null)native.Touch-=Touch;pressed=false;if(candidate.HasValue&&source==candidate)Cancel();native=null;};
        handle.HandlerChanged+=(_,_)=>Connect();Connect();
    }
#endif
    public void AttachRow(View row)
    {
        rows.RemoveAll(reference=>!reference.TryGetTarget(out _));
        rows.Add(new(row));
#if !ANDROID
        bool Valid()=>Current()&&row.BindingContext is ShoppingListItem item&&model.CanDropItem(source.Value,item.Id);
        var drop=new DropGestureRecognizer{AllowDrop=true};
        drop.DragOver+=(_,e)=>{if(!Valid()){e.AcceptedOperation=DataPackageOperation.None;return;}e.AcceptedOperation=DataPackageOperation.Copy;row.Opacity=.65;if(!highlights.Contains(row))highlights.Add(row);};
        drop.DragLeave+=(_,_)=>row.Opacity=1;
        drop.Drop+=(_,e)=>{
            try{
                e.Handled=true;if(!Valid())return;var id=source.Value;var target=((ShoppingListItem)row.BindingContext).Id;var after=(e.GetPosition(row)?.Y??0)>row.Height/2;
                Cancel();model.DropItem(id,target,after);
            }catch(Exception error){_=Shell.Current.DisplayAlert("Item order",error is InvalidOperationException?error.Message:"Couldn't save the item order. Please try again.",AppLocalization.Text("OK"));}
            finally{Cancel();}
        };row.GestureRecognizers.Add(drop);
#endif
    }
}
