using RecipeApp.Services;
namespace RecipeApp.Views;

/// <summary>Only accepts drags started in this list, never external drag payloads.</summary>
internal sealed class ShoppingSectionDrag
{
    private readonly Func<List<string>> order;
    private readonly Action<List<string>> save;
    private readonly Func<bool> allowed;
    private readonly Func<string> context;
    private readonly Action started,finished;
#if !ANDROID
    private string source,sourceAccount,sourceContext;
    private bool Current()=>source!=null&&allowed()&&sourceAccount==AccountSession.UserId&&sourceContext==context?.Invoke();
    private readonly List<VisualElement> highlights=new();

#endif
    public ShoppingSectionDrag(Func<List<string>> order,Action<List<string>> save,Func<bool> allowed,Func<string> context=null,Action started=null,Action finished=null){this.order=order;this.save=save;this.allowed=allowed;this.context=context;this.started=started;this.finished=finished;}
#if ANDROID
    private RecipeApp.Platforms.Android.ShoppingDirectDrag direct;
    private RecipeApp.Platforms.Android.ShoppingDirectDrag Direct=>direct??=new(allowed,
        (from,to)=>from!=to&&order().Contains(from)&&order().Contains(to),
        (from,to,after)=>save(ShoppingSectionOrder.Drop(order(),from,to,after)),context,()=>order(),started,finished);
    public bool IsDragging=>direct?.IsDragging==true;
#else
    public bool IsDragging=>source!=null;
#endif
    public void Cancel(){
#if ANDROID
        direct?.Cancel();
#else
        var wasDragging=source!=null;
        source=null;sourceAccount=null;sourceContext=null;
        foreach(var view in highlights)view.Opacity=1;highlights.Clear();
        if(wasDragging)finished?.Invoke();
#endif
    }
    public void Attach(View row,View handle,Func<string> section)
    {
#if ANDROID
        Direct.Register(row,_=>section());Direct.Attach(handle,section);
#else
        var drag=new DragGestureRecognizer{CanDrag=true};
        drag.DragStarting+=(_,e)=>{Cancel();if(!allowed()||!order().Contains(section())){e.Cancel=true;return;}source=section();sourceAccount=AccountSession.UserId;sourceContext=context?.Invoke();e.Data.Text=source;row.Opacity=.55;highlights.Add(row);started?.Invoke();};
        drag.DropCompleted+=(_,_)=>Cancel();
        var drop=new DropGestureRecognizer{AllowDrop=true};
        drop.DragOver+=(_,e)=>{if(!Current()){e.AcceptedOperation=DataPackageOperation.None;return;}e.AcceptedOperation=DataPackageOperation.Copy;if(section()!=source){row.Opacity=.65;if(!highlights.Contains(row))highlights.Add(row);}};
        drop.DragLeave+=(_,_)=>{if(section()!=source)row.Opacity=1;};
        drop.Drop+=(_,e)=>{
            try{e.Handled=true;if(!Current())return;var target=section();var after=(e.GetPosition(row)?.Y??0)>row.Height/2;var moved=ShoppingSectionOrder.Drop(order(),source,target,after);Cancel();save(moved);}
            catch(Exception error){Cancel();_=Shell.Current.DisplayAlert("Section order",error is InvalidOperationException?error.Message:"Couldn’t save the section order. Please try again.",AppLocalization.Text("OK"));}
            finally{Cancel();}
        };
        handle.GestureRecognizers.Add(drag);row.GestureRecognizers.Add(drop);
#endif
    }
}
