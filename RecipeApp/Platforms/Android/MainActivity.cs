
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Firebase;
using Microsoft.Maui;
using System.Diagnostics;

namespace RecipeApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[]{Intent.ActionSend},Categories=new[]{Intent.CategoryDefault},DataMimeType="text/plain")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
        {
        base.OnCreate(savedInstanceState);
        Platforms.Android.WidgetNavigation.FromIntent(Intent);
        OpenSharedRecipe(Intent);
        OpenActivity(Intent);
        var manager=global::Android.Appwidget.AppWidgetManager.GetInstance(this);
        var widgetIds=manager.GetAppWidgetIds(new ComponentName(this,Java.Lang.Class.FromType(typeof(Platforms.Android.ShoppingQuickAddWidget))));
        foreach(var widgetId in widgetIds)Platforms.Android.ShoppingQuickAddWidget.Update(this,widgetId);
        }


    protected override void OnNewIntent(global::Android.Content.Intent intent)
    {
        base.OnNewIntent(intent); Intent=intent; Platforms.Android.WidgetNavigation.FromIntent(intent);OpenSharedRecipe(intent);OpenActivity(intent);
    }

    private static bool openingShare;
    protected override void OnResume(){base.OnResume();_=Platforms.Android.ActivityNotifications.Refresh();}
    private static async void OpenActivity(Intent intent){if(intent?.GetBooleanExtra("mealsy_activity",false)!=true)return;intent.RemoveExtra("mealsy_activity");for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);if(App.InitialAccountReady&&Shell.Current!=null&&RecipeApp.Services.HouseholdService.UserId!=null)await RecipeApp.Services.AppNavigation.Open(new RecipeApp.Views.ActivityPage());}
    private static async void OpenSharedRecipe(Intent intent){
        if(intent?.Action!=Intent.ActionSend||openingShare)return;
        var text=intent.GetStringExtra(Intent.ExtraText);intent.RemoveExtra(Intent.ExtraText);if(string.IsNullOrWhiteSpace(text)||text.Length>10000)return;
        var match=System.Text.RegularExpressions.Regex.Match(text,@"https://[^\s<>""']+");if(!match.Success)return;
        openingShare=true;
        try{for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(!App.InitialAccountReady||Shell.Current==null)return;
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>RecipeApp.Services.AppNavigation.Open(new RecipeApp.Views.RecipeWebsitePage(match.Value)));
        }finally{openingShare=false;}
    }
}
