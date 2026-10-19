using System.Runtime.CompilerServices;
namespace RecipeApp.Views;

/// <summary>Keep each page's existing actions, commands and labels behind one options control.</summary>
internal static class PageOptions
{
    private const string Caption="⚙ Options";
    private static readonly ConditionalWeakTable<ContentPage,OptionsState> states=new();
    public static void Configure(Page page)
    {
        if(page is not ContentPage content||content.ToolbarItems.Count==0||states.TryGetValue(content,out _))return;
        var state=new OptionsState(content);states.Add(content,state);
    }
    private sealed class OptionsState
    {
        private readonly ContentPage page;
        private readonly List<ToolbarItem> actions;
        private bool showing;
#if ANDROID
        private AndroidX.AppCompat.Widget.PopupMenu popup;
#endif
        public OptionsState(ContentPage page)
        {
            this.page=page;actions=page.ToolbarItems.ToList();
            // Removed items lose their inherited context. Keep command bindings
            // (e.g. shopping Paste/Clear) connected to the page's current model.
            foreach(var item in actions)
                if(item.BindingContext==null||ReferenceEquals(item.BindingContext,page.BindingContext))
                    item.SetBinding(BindableObject.BindingContextProperty,new Binding(nameof(Page.BindingContext),source:page));
            page.ToolbarItems.Clear();page.ToolbarItems.Add(new ToolbarItem(Caption,null,async()=>await Show()){AutomationId="PageOptions",Order=ToolbarItemOrder.Primary});
            page.Disappearing+=(_,_)=>{
#if ANDROID
                popup?.Dismiss();
#endif
            };
        }
        private static bool Enabled(ToolbarItem item)=>((IMenuItemController)item).IsEnabled&&(item.Command?.CanExecute(item.CommandParameter)??true);
        private static void Activate(ToolbarItem item){if(Enabled(item))((IMenuItemController)item).Activate();}
        private async Task Show()
        {
            if(showing)return;showing=true;
            try{
#if ANDROID
                var activity=Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;var anchor=FindAnchor(activity?.Window?.DecorView);
                if(activity!=null&&anchor!=null){
                    popup=new AndroidX.AppCompat.Widget.PopupMenu(activity,anchor);
                    for(var index=0;index<actions.Count;index++){using var title=new Java.Lang.String(actions[index].Text??"Action");popup.Menu.Add(0,index,index,title).SetEnabled(Enabled(actions[index]));}
                    popup.MenuItemClick+=(_,e)=>{e.Handled=true;var index=e.Item.ItemId;if(index>=0&&index<actions.Count)Activate(actions[index]);};
                    popup.DismissEvent+=(_,_)=>{showing=false;popup=null;};popup.Show();return;
                }
#endif
                var available=actions.Where(Enabled).ToList();if(available.Count==0)return;
                var labels=available.Select(item=>item.Text??"Action").ToArray();
                var choice=await page.DisplayActionSheet("Options","Cancel",null,labels);var selected=Array.IndexOf(labels,choice);
                if(selected>=0)Activate(available[selected]);
            }catch(Exception error){
#if ANDROID
                popup?.Dismiss();popup=null;
#endif
                await page.DisplayAlert("Options",error is InvalidOperationException?error.Message:"Couldn't open that option. Please try again.","OK");
            }
            finally{
#if ANDROID
                if(popup==null)showing=false;
#else
                showing=false;
#endif
            }
        }
#if ANDROID
        private static global::Android.Views.View FindAnchor(global::Android.Views.View root)
        {
            if(root is global::Android.Widget.TextView text&&string.Equals(text.Text,Caption,StringComparison.OrdinalIgnoreCase))return root;
            if(root is global::Android.Views.ViewGroup group)for(var index=0;index<group.ChildCount;index++){var found=FindAnchor(group.GetChildAt(index));if(found!=null)return found;}
            return null;
        }
#endif
    }
}
