using Foundation;
using RecipeApp.Services;
using RecipeApp.Views;

namespace RecipeApp.Platforms.iOS;

/// <summary>Extensions never receive account credentials or directly edit cloud lists.</summary>
internal static class ExtensionNavigation
{
    public const int WidgetId=-100;
    private const string Group="group.com.companyname.recipeapp";
    private static bool opening;
    private static string pendingWidget;

    public static bool Handle(string raw)
    {
        if(raw is not ("mealsy://widget/add" or "mealsy://widget/open" or "mealsy://widget/configure"))return false;
        pendingWidget=raw;_=OpenWidget();return true;
    }
    private static async Task<bool> Ready()
    {
        for(var i=0;i<300&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
        return App.InitialAccountReady&&Shell.Current!=null&&App.IsForeground;
    }
    private static async Task OpenWidget()
    {
        if(opening||pendingWidget==null)return;
        var route=pendingWidget;pendingWidget=null;opening=true;
        try
        {
            if(!await Ready())return;
            await MainThread.InvokeOnMainThreadAsync(async()=>{
                var account=HouseholdService.UserId;
                var target=WidgetShoppingStore.Target(WidgetId);
                if(route=="mealsy://widget/open"&&target?.BelongsTo(account)==true)
                {
                    if(target.HouseholdId!=null)
                    {
                        var status=await HouseholdService.StatusAsync(true);
                        AccountSession.Check(account);
                        if(status.HouseholdId!=target.HouseholdId||!status.FamilyEnabled)
                            throw new InvalidOperationException("Access to this household list has ended. Choose another list in quick add.");
                    }
                    await AppNavigation.ShoppingList(target.OpenRoute(account));
                }
                else await AppNavigation.Open(new WidgetQuickAddPage(WidgetId,route!="mealsy://widget/add"));
            });
        }
        catch(Exception error)
        {
            AppDiagnostics.Report(error,"navigation.widget","OPEN_FAILURE");
            await MainThread.InvokeOnMainThreadAsync(()=>Shell.Current.DisplayAlert("Quick shopping",error is InvalidOperationException or HouseholdException?error.Message:"Couldn't open quick add. Open Shopping and try again.","OK"));
        }
        finally{opening=false;if(pendingWidget!=null)_=OpenWidget();}
    }

    public static async Task ReviewInbox(bool showEmpty=false)
    {
        if(opening)return;opening=true;
        try
        {
            if(!await Ready())return;
            var container=NSFileManager.DefaultManager.GetContainerUrl(Group);
            if(container?.Path==null)return;
            var directory=Path.Combine(container.Path,"RecipeInbox");
            if(!Directory.Exists(directory))return;
            // One explicit review at a time; no recipe network request until Import.
            var file=Directory.EnumerateFiles(directory,"*.txt").OrderBy(File.GetLastWriteTimeUtc).FirstOrDefault();
            if(file==null)
            {
                if(showEmpty)await MainThread.InvokeOnMainThreadAsync(()=>Shell.Current.DisplayAlert("Shared recipe links","No saved links yet. Share a recipe page from your browser to Mealsy, then return here to review it.","OK"));
                return;
            }
            var info=new FileInfo(file);
            if(info.Length>10000){File.Delete(file);return;}
            string link;
            try{link=SharedRecipeLink.Extract(await File.ReadAllTextAsync(file));}
            catch(InvalidOperationException){File.Delete(file);return;}
            await MainThread.InvokeOnMainThreadAsync(async()=>{
                if(Shell.Current.CurrentPage is RecipeWebsitePage)return;
                var action=await Shell.Current.DisplayActionSheet("Shared recipe · "+new Uri(link).Host,"Later","Discard link","Review in importer");
                if(action=="Discard link"){File.Delete(file);return;}
                if(action!="Review in importer")return;
                await AppNavigation.Open(new RecipeWebsitePage(link));
                File.Delete(file);
            });
        }
        catch(Exception error){AppDiagnostics.Report(error,"navigation.recipe-inbox","OPEN_FAILURE");}
        finally{opening=false;if(pendingWidget!=null)_=OpenWidget();}
    }
}
