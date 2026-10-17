
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
    protected override void OnResume(){base.OnResume();_=Platforms.Android.ActivityNotifications.Refresh();}
    private static async void OpenActivity(Intent intent){if(!Services.ProductFeatures.SocialEnabled||intent?.GetBooleanExtra("mealsy_activity",false)!=true)return;intent.RemoveExtra("mealsy_activity");for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);if(App.InitialAccountReady&&Shell.Current!=null&&RecipeApp.Services.HouseholdService.UserId!=null)await RecipeApp.Services.AppNavigation.Open(new RecipeApp.Views.ActivityPage());}
    private static async void OpenSharedRecipe(Intent intent){
        if(intent?.Action!=Intent.ActionSend||openingShare)return;
        var text=intent.GetStringExtra(Intent.ExtraText);intent.RemoveExtra(Intent.ExtraText);if(string.IsNullOrWhiteSpace(text)||text.Length>10000)return;
        var match=System.Text.RegularExpressions.Regex.Match(text,@"https://[^\s<>""']+");if(!match.Success)return;
        try{
            var content=Services.ContentLinks.Parse(match.Value);
            if(!Services.ProductFeatures.SocialEnabled)return;
            openingShare=true;
            try{for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);if(App.InitialAccountReady&&Shell.Current!=null)await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>Services.ContentSharing.Open(content));}
            finally{openingShare=false;}return;
        }catch(InvalidOperationException){ }
        openingShare=true;
        try{for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(!App.InitialAccountReady||Shell.Current==null)return;
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(()=>RecipeApp.Services.AppNavigation.Open(new RecipeApp.Views.RecipeWebsitePage(match.Value)));
        }finally{openingShare=false;}
    }
}
