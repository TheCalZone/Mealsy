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
    public bool IsDragging=>source.HasValue;
    public ShoppingItemDrag(ShoppingListViewModel model,Func<bool> allowed){this.model=model;this.allowed=allowed;}
    private bool Current()=>source.HasValue&&allowed()&&account==AccountSession.UserId&&context==model.SectionOrderContext;
    public void Cancel(){source=null;foreach(var view in highlights)view.Opacity=1;highlights.Clear();}
    public void AttachHandle(View handle)
    {
        var drag=new DragGestureRecognizer{CanDrag=true};
        drag.DragStarting+=(_,e)=>{
            Cancel();if(!allowed()||handle.BindingContext is not ShoppingListItem item){e.Cancel=true;return;}
            source=item.Id;account=AccountSession.UserId;context=model.SectionOrderContext;e.Data.Text=item.Id.ToString();handle.Opacity=.45;highlights.Add(handle);
        };
        drag.DropCompleted+=(_,_)=>Cancel();handle.GestureRecognizers.Add(drag);
    }
    public void AttachRow(View row)
    {
        bool Valid()=>Current()&&row.BindingContext is ShoppingListItem item&&model.CanDropItem(source.Value,item.Id);
        var drop=new DropGestureRecognizer{AllowDrop=true};
        drop.DragOver+=(_,e)=>{if(!Valid()){e.AcceptedOperation=DataPackageOperation.None;return;}e.AcceptedOperation=DataPackageOperation.Copy;row.Opacity=.65;if(!highlights.Contains(row))highlights.Add(row);};
        drop.DragLeave+=(_,_)=>row.Opacity=1;
        drop.Drop+=(_,e)=>{
            try{
                e.Handled=true;if(!Valid())return;var id=source.Value;var target=((ShoppingListItem)row.BindingContext).Id;var after=(e.GetPosition(row)?.Y??0)>row.Height/2;
                Cancel();model.DropItem(id,target,after);
            }catch(Exception error){_=Shell.Current.DisplayAlert("Item order",error is InvalidOperationException?error.Message:"Couldn't save the item order. Please try again.","OK");}
            finally{Cancel();}
        };row.GestureRecognizers.Add(drop);
    }
}
