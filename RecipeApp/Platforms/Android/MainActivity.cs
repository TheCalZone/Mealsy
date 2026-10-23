
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using Firebase;
using Microsoft.Maui;
using System.Diagnostics;

namespace RecipeApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustResize, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable},DataScheme="mealsy",DataHost="join")]
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable},DataScheme="mealsy",DataHost="welcome")]
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable},DataScheme="mealsy",DataHost="content")]
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable},DataScheme="https",DataHost="mealsy-b732b.web.app",DataPathPrefix="/share/",AutoVerify=true)]
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable},DataScheme="https",DataHost="mealsy-b732b.firebaseapp.com",DataPathPrefix="/share/",AutoVerify=true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
        {
        base.OnCreate(savedInstanceState);
        Window?.SetSoftInputMode(SoftInput.AdjustResize);
        Platforms.Android.WidgetNavigation.FromIntent(Intent);
        OpenContent(Intent);
        OpenInvitation(Intent);
        OpenSharedRecipe(Intent);
        OpenActivity(Intent);
        var manager=global::Android.Appwidget.AppWidgetManager.GetInstance(this);
        var widgetIds=manager.GetAppWidgetIds(new ComponentName(this,Java.Lang.Class.FromType(typeof(Platforms.Android.ShoppingQuickAddWidget))));
        foreach(var widgetId in widgetIds)Platforms.Android.ShoppingQuickAddWidget.Update(this,widgetId);
        }


    protected override void OnNewIntent(global::Android.Content.Intent intent)
    {
        base.OnNewIntent(intent); Intent=intent; Platforms.Android.WidgetNavigation.FromIntent(intent);OpenContent(intent);OpenInvitation(intent);OpenSharedRecipe(intent);OpenActivity(intent);
    }

    private static bool openingContent;
    private static async void OpenContent(Intent intent)
    {
        if(intent?.Action!=Intent.ActionView||openingContent)return;
        Services.ContentTarget target;try{target=Services.ContentLinks.Parse(intent.DataString);}catch(InvalidOperationException){return;}
        intent.SetData(null);if(!Services.ProductFeatures.SocialEnabled)return;openingContent=true;
        try{
            for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(App.InitialAccountReady&&Shell.Current!=null)await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>Services.ContentSharing.Open(target));
        }catch(Exception error){Services.AppDiagnostics.Report(error,"ui.action");}finally{openingContent=false;}
    }
    private static bool openingInvitation;
    private static async void OpenInvitation(Intent intent)
    {
        if(intent?.Action!=Intent.ActionView||openingInvitation)return;
        var raw=intent.DataString;if(string.IsNullOrWhiteSpace(raw)||raw.Length>2000)return;
        string code=null;
        try{if(raw.StartsWith("mealsy://join",StringComparison.OrdinalIgnoreCase)){code=Services.InvitationLinks.Code(raw);Services.OnboardingState.Remember(code);}else if(raw!="mealsy://welcome")return;}catch(InvalidOperationException){return;}
        intent.SetData(null);openingInvitation=true;
        try{for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(!App.InitialAccountReady||Shell.Current==null)return;
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>Services.AppNavigation.Open(new Views.OnboardingPage(code,startAt:code==null?0:2)));
        }catch(Exception error){Services.AppDiagnostics.Report(error,"navigation.invitation","OPEN_FAILURE");}finally{openingInvitation=false;}
    }
    private static bool openingShare;
    protected override void OnResume(){base.OnResume();Services.ThemeService.ApplySystemBars();_=Platforms.Android.ActivityNotifications.Refresh();}
    private static async void OpenActivity(Intent intent){
        var household=intent?.GetBooleanExtra("mealsy_household_activity",false)==true;
        if(!household&&(!Services.ProductFeatures.SocialEnabled||intent?.GetBooleanExtra("mealsy_activity",false)!=true))return;
        intent.RemoveExtra("mealsy_household_activity");intent.RemoveExtra("mealsy_activity");
        for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
        if(App.InitialAccountReady&&Shell.Current!=null&&RecipeApp.Services.HouseholdService.UserId!=null)
            await RecipeApp.Services.AppNavigation.Open(household?new RecipeApp.Views.HouseholdShoppingInboxPage():new RecipeApp.Views.ActivityPage());
    }

    private static readonly Queue<string> sharedRecipes=new();
    private static async void OpenSharedRecipe(Intent intent)
    {
        if(intent?.Action!=Intent.ActionSend)return;
        var text=intent.GetCharSequenceExtra(Intent.ExtraText)?.ToString();intent.RemoveExtra(Intent.ExtraText);
        if(string.IsNullOrWhiteSpace(text))return;
        try
        {
            var link=Services.SharedRecipeLink.Extract(text);
            if(sharedRecipes.Count>=5)throw new InvalidOperationException("Finish opening the shared recipes, then share this link again.");
            sharedRecipes.Enqueue(link);
        }
        catch(InvalidOperationException error){global::Android.Widget.Toast.MakeText(global::Android.App.Application.Context,error.Message,global::Android.Widget.ToastLength.Long).Show();return;}
        if(openingShare)return;
        openingShare=true;
        try
        {
            while(sharedRecipes.Count>0)
            {
                var link=sharedRecipes.Dequeue();
                for(var i=0;i<300&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
                if(!App.InitialAccountReady||Shell.Current==null)throw new InvalidOperationException("Mealsy is still starting. Once it is ready, share your recipe link again.");
                Services.ContentTarget content=null;
                try{content=Services.ContentLinks.Parse(link);}catch(InvalidOperationException){ }
                if(content!=null)
                {
                    if(Services.ProductFeatures.SocialEnabled)await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>Services.ContentSharing.Open(content));
                    else global::Android.Widget.Toast.MakeText(global::Android.App.Application.Context,"Community sharing is currently unavailable.",global::Android.Widget.ToastLength.Long).Show();
                }
                else await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>Services.AppNavigation.Open(new Views.RecipeWebsitePage(link)));
            }
        }
        catch(Exception error)
        {
            Services.AppDiagnostics.Report(error,"navigation.recipe-share","OPEN_FAILURE");
            global::Android.Widget.Toast.MakeText(global::Android.App.Application.Context,error is InvalidOperationException?error.Message:"Couldn't open the recipe importer. Open Mealsy and share the page again.",global::Android.Widget.ToastLength.Long).Show();
        }
        finally{openingShare=false;}
    }
}
