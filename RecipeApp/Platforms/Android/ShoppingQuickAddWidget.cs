using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using RecipeApp.Services;
namespace RecipeApp.Platforms.Android;

[BroadcastReceiver(Name="com.companyname.recipeapp.ShoppingQuickAddWidget",Label="Mealsy quick add",Exported=true)]
[IntentFilter(new[]{"android.appwidget.action.APPWIDGET_UPDATE"})]
[MetaData("android.appwidget.provider",Resource="@xml/shopping_widget_info")]
public class ShoppingQuickAddWidget : AppWidgetProvider
{
    internal static bool SystemDark(Context context)=>(context.Resources.Configuration.UiMode&global::Android.Content.Res.UiMode.NightMask)==global::Android.Content.Res.UiMode.NightYes;
    public override void OnUpdate(Context context,AppWidgetManager manager,int[] ids){foreach(var id in ids)Update(context,id);}
    public override void OnRestored(Context context,int[] oldIds,int[] newIds)
    {
        for(var i=0;i<Math.Min(oldIds.Length,newIds.Length);i++){var target=WidgetShoppingStore.Target(oldIds[i]);if(target!=null)WidgetShoppingStore.SetTarget(newIds[i],target);if(oldIds[i]!=newIds[i])WidgetShoppingStore.Delete(oldIds[i]);Update(context,newIds[i]);}
        base.OnRestored(context,oldIds,newIds);
    }
    public override void OnDeleted(Context context,int[] ids){foreach(var id in ids)WidgetShoppingStore.Delete(id);base.OnDeleted(context,ids);}
    public static bool IsValid(int id)
    {
        var context=global::Android.App.Application.Context;var info=AppWidgetManager.GetInstance(context).GetAppWidgetInfo(id);
        return info?.Provider?.PackageName==context.PackageName&&info.Provider.ClassName=="com.companyname.recipeapp.ShoppingQuickAddWidget";
    }
    public static void UpdateAll(Context context)
    {
        var manager=AppWidgetManager.GetInstance(context);
        foreach(var id in manager.GetAppWidgetIds(new ComponentName(context.PackageName,"com.companyname.recipeapp.ShoppingQuickAddWidget")))Update(context,id);
    }
    public static void Update(Context context,int id)
    {
        var views=new RemoteViews(context.PackageName,Resource.Layout.shopping_quick_add_widget);
        var theme=ThemeColors.For(AppSettings.Current,SystemDark(context));
        int Color(string hex)=>global::Android.Graphics.Color.ParseColor(hex).ToArgb();
        views.SetInt(Resource.Id.widget_root,"setBackgroundResource",AppSettings.Current.IsDark(SystemDark(context))?Resource.Drawable.shopping_widget_background_dark:Resource.Drawable.shopping_widget_background);
        views.SetTextColor(Resource.Id.widget_title,global::Android.Graphics.Color.ParseColor(theme.Accent));
        views.SetTextColor(Resource.Id.widget_list,global::Android.Graphics.Color.ParseColor(theme.Text));
        foreach(var button in new[]{Resource.Id.widget_add,Resource.Id.widget_choose,Resource.Id.widget_open})views.SetInt(button,"setColorFilter",Color(theme.Accent));
        var target=WidgetShoppingStore.Target(id);views.SetTextViewText(Resource.Id.widget_list,target==null?"Choose a shopping list":target.Name);
        PendingIntent Open(string action)
        {
            var intent=new Intent(context,action=="add"?typeof(WidgetQuickAddActivity):typeof(MainActivity));intent.SetAction("com.mealsy.widget."+id+"."+action);
            intent.SetData(global::Android.Net.Uri.Parse($"mealsy://widget/{id}/{action}"));
            intent.PutExtra("mealsy_widget_id",id);intent.PutExtra("mealsy_widget_configure",action=="configure");intent.PutExtra("mealsy_widget_open",action=="open");intent.AddFlags(ActivityFlags.ClearTop|ActivityFlags.SingleTop);
            return PendingIntent.GetActivity(context,id,intent,PendingIntentFlags.UpdateCurrent|PendingIntentFlags.Immutable);
        }
        views.SetOnClickPendingIntent(Resource.Id.widget_add,Open("add"));views.SetOnClickPendingIntent(Resource.Id.widget_choose,Open("configure"));views.SetOnClickPendingIntent(Resource.Id.widget_open,Open("open"));
        AppWidgetManager.GetInstance(context).UpdateAppWidget(id,views);
    }
}

internal static class WidgetNavigation
{
    private static bool opening;
    public static async void FromIntent(Intent intent)
    {
        var id=intent?.GetIntExtra("mealsy_widget_id",-1)??-1;if(id<0||opening||!ShoppingQuickAddWidget.IsValid(id))return;
        opening=true;
        try
        {
            var configure=intent.GetBooleanExtra("mealsy_widget_configure",false);var open=intent.GetBooleanExtra("mealsy_widget_open",false);intent.RemoveExtra("mealsy_widget_id");
            for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(Shell.Current==null)return;
            if(!App.InitialAccountReady){await Shell.Current.DisplayAlert("Mealsy is still starting","Wait for sign-in to finish, then tap the widget again.","OK");return;}
            if(open) {
                var account=HouseholdService.UserId;var target=WidgetShoppingStore.Target(id);
                if(target==null||!target.BelongsTo(account)) {await MainThread.InvokeOnMainThreadAsync(async()=>await Shell.Current.Navigation.PushAsync(new RecipeApp.Views.WidgetQuickAddPage(id,true)));return;}
                if(target.HouseholdId!=null) {
                    var household=await HouseholdService.StatusAsync();
                    if(account!=HouseholdService.UserId||household.HouseholdId!=target.HouseholdId||!household.FamilyEnabled)throw new InvalidOperationException("Access to the chosen household list has ended. Tap Choose list to select another list.");
                }
                var route=target.OpenRoute(HouseholdService.UserId);
                await MainThread.InvokeOnMainThreadAsync(async()=>await AppNavigation.ShoppingList(route));return;
            }
            if(Shell.Current.CurrentPage is RecipeApp.Views.WidgetQuickAddPage existing&&existing.WidgetId==id&&existing.IsConfiguration==configure)return;
            await MainThread.InvokeOnMainThreadAsync(async()=>await Shell.Current.Navigation.PushAsync(new RecipeApp.Views.WidgetQuickAddPage(id,configure)));
        }
        catch(Exception error){if(Shell.Current!=null)await Shell.Current.DisplayAlert("Widget unavailable",error is HouseholdException or InvalidOperationException?error.Message:"Open Mealsy and try the widget again.\nReference: "+AppDiagnostics.Report(error,"ui.action"),"OK");}
        finally{opening=false;}
    }
}
