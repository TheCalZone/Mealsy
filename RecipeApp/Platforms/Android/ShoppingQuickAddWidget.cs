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
    public static void Update(Context context,int id)
    {
        var views=new RemoteViews(context.PackageName,Resource.Layout.shopping_quick_add_widget);
        var target=WidgetShoppingStore.Target(id);views.SetTextViewText(Resource.Id.widget_list,target?.HouseholdId==null?"Personal shopping list":"Household shopping list");
        PendingIntent Open(bool configure)
        {
            var intent=new Intent(context,typeof(MainActivity));intent.SetAction("com.mealsy.widget."+id+(configure?".configure":".add"));
            intent.SetData(global::Android.Net.Uri.Parse($"mealsy://widget/{id}/{(configure?"configure":"add")}"));
            intent.PutExtra("mealsy_widget_id",id);intent.PutExtra("mealsy_widget_configure",configure);intent.AddFlags(ActivityFlags.ClearTop|ActivityFlags.SingleTop);
            return PendingIntent.GetActivity(context,id,intent,PendingIntentFlags.UpdateCurrent|PendingIntentFlags.Immutable);
        }
        views.SetOnClickPendingIntent(Resource.Id.widget_add,Open(false));views.SetOnClickPendingIntent(Resource.Id.widget_choose,Open(true));
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
            var configure=intent.GetBooleanExtra("mealsy_widget_configure",false);intent.RemoveExtra("mealsy_widget_id");
            for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(Shell.Current==null)return;
            if(!App.InitialAccountReady){await Shell.Current.DisplayAlert("Mealsy is still starting","Wait for sign-in to finish, then tap the widget again.","OK");return;}
            if(Shell.Current.CurrentPage is RecipeApp.Views.WidgetQuickAddPage existing&&existing.WidgetId==id)return;
            await MainThread.InvokeOnMainThreadAsync(async()=>await Shell.Current.Navigation.PushAsync(new RecipeApp.Views.WidgetQuickAddPage(id,configure)));
        }
        catch(Exception){if(Shell.Current!=null)await Shell.Current.DisplayAlert("Quick add unavailable","Open Mealsy and try the widget again.","OK");}
        finally{opening=false;}
    }
}
