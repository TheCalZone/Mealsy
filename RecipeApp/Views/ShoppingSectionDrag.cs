using RecipeApp.Services;
namespace RecipeApp.Views;

/// <summary>Only accepts drags started in this list, never external drag payloads.</summary>
internal sealed class ShoppingSectionDrag
{
    private readonly Func<List<string>> order;
    private readonly Action<List<string>> save;
    private readonly Func<bool> allowed;
    private readonly Func<string> context;
#if !ANDROID
    private string source;
    private readonly List<VisualElement> highlights=new();

#endif
    public ShoppingSectionDrag(Func<List<string>> order,Action<List<string>> save,Func<bool> allowed,Func<string> context=null){this.order=order;this.save=save;this.allowed=allowed;this.context=context;}
#if ANDROID
    private RecipeApp.Platforms.Android.ShoppingDirectDrag direct;
    private RecipeApp.Platforms.Android.ShoppingDirectDrag Direct=>direct??=new(allowed,
        (from,to)=>from!=to&&order().Contains(from)&&order().Contains(to),
        (from,to,after)=>save(ShoppingSectionOrder.Drop(order(),from,to,after)),context,()=>order());
    public bool IsDragging=>direct?.IsDragging==true;
#else
    public bool IsDragging=>source!=null;
#endif
    public void Cancel(){
#if ANDROID
        direct?.Cancel();
#else
        source=null;foreach(var view in highlights)view.Opacity=1;highlights.Clear();
#endif
    }
    public void Attach(View row,View handle,Func<string> section)
    {
#if ANDROID
        Direct.Register(row,_=>section());Direct.Attach(handle,section);
#else
        var drag=new DragGestureRecognizer{CanDrag=true};
        drag.DragStarting+=(_,e)=>{Cancel();if(!allowed()){e.Cancel=true;return;}source=section();e.Data.Text=source;row.Opacity=.55;highlights.Add(row);};
        drag.DropCompleted+=(_,_)=>Cancel();
        var drop=new DropGestureRecognizer{AllowDrop=true};
        drop.DragOver+=(_,e)=>{if(source==null||!allowed()){e.AcceptedOperation=DataPackageOperation.None;return;}e.AcceptedOperation=DataPackageOperation.Copy;if(section()!=source){row.Opacity=.65;if(!highlights.Contains(row))highlights.Add(row);}};
        drop.DragLeave+=(_,_)=>{if(section()!=source)row.Opacity=1;};
        drop.Drop+=(_,e)=>{
            try{e.Handled=true;if(source==null||!allowed())return;var target=section();var after=(e.GetPosition(row)?.Y??0)>row.Height/2;var moved=ShoppingSectionOrder.Drop(order(),source,target,after);Cancel();save(moved);}
            catch(Exception error){Cancel();_=Shell.Current.DisplayAlert("Section order",error is InvalidOperationException?error.Message:"Couldn’t save the section order. Please try again.",AppLocalization.Text("OK"));}
            finally{Cancel();}
        };
        handle.GestureRecognizers.Add(drag);row.GestureRecognizers.Add(drop);
#endif
    }
}
