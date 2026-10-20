using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;
namespace RecipeApp.Views;

/// <summary>Only the handle starts a drag; checkboxes, item taps and swipes keep their actions.</summary>
internal sealed class ShoppingItemDrag
{
    private readonly ShoppingListViewModel model;
    private readonly Func<bool> allowed;
#if !ANDROID
    private Guid? source;
    private string account,context;
    private readonly List<View> highlights=new();
    private readonly List<WeakReference<View>> rows=new();
#endif
#if ANDROID
    private RecipeApp.Platforms.Android.ShoppingDirectDrag direct;
    private RecipeApp.Platforms.Android.ShoppingDirectDrag Direct=>direct??=new(allowed,
        (from,to)=>Guid.TryParse(from,out var a)&&Guid.TryParse(to,out var b)&&model.CanDropItem(a,b),
        (from,to,after)=>model.DropItem(Guid.Parse(from),Guid.Parse(to),after),()=>model.SectionOrderContext);
    public bool IsDragging=>direct?.IsDragging==true;
#else
    public bool IsDragging=>source.HasValue;
#endif
    public ShoppingItemDrag(ShoppingListViewModel model,Func<bool> allowed){this.model=model;this.allowed=allowed;}

#if !ANDROID
    private bool Current()=>source.HasValue&&allowed()&&account==AccountSession.UserId&&context==model.SectionOrderContext;
#endif
    public void Cancel(){
#if ANDROID
        direct?.Cancel();
#else
        source=null;foreach(var view in highlights){view.Opacity=1;view.TranslationY=0;}highlights.Clear();
#endif
    }
    public void AttachHandle(View handle)
    {
#if ANDROID
        Direct.Attach(handle,()=> (handle.BindingContext as ShoppingListItem)?.Id.ToString());
#else
        var drag=new DragGestureRecognizer{CanDrag=true};
        drag.DragStarting+=(_,e)=>{
            Cancel();if(!allowed()||handle.BindingContext is not ShoppingListItem item){e.Cancel=true;return;}
            source=item.Id;account=AccountSession.UserId;context=model.SectionOrderContext;e.Data.Text=item.Id.ToString();handle.Opacity=.45;highlights.Add(handle);
        };
        drag.DropCompleted+=(_,_)=>Cancel();handle.GestureRecognizers.Add(drag);
#endif
    }
    public void AttachRow(View row)
    {
#if ANDROID
        Direct.Register(row,view=> (view.BindingContext as ShoppingListItem)?.Id.ToString());
#else
        rows.RemoveAll(reference=>!reference.TryGetTarget(out _));
        rows.Add(new(row));
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
