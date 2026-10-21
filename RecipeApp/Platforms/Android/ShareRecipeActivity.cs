using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace RecipeApp.Platforms.Android;

[Activity(Exported=true,NoHistory=true,ExcludeFromRecents=true,Label="Save recipe to Mealsy",Theme="@android:style/Theme.NoDisplay")]
[IntentFilter(new[]{Intent.ActionSend},Categories=new[]{Intent.CategoryDefault},DataMimeTypes=new[]{"text/plain","text/html"})]
public class ShareRecipeActivity : Activity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            if(Intent?.Action!=global::Android.Content.Intent.ActionSend)return;
            var text=Intent.GetCharSequenceExtra(global::Android.Content.Intent.ExtraText)?.ToString()??Intent.GetStringExtra(global::Android.Content.Intent.ExtraHtmlText);
            if(string.IsNullOrWhiteSpace(text)&&Intent.ClipData?.ItemCount>0)
            {
                var item=Intent.ClipData.GetItemAt(0);text=item?.Text?.ToString()??item?.Uri?.ToString();
            }
            var link=Services.SharedRecipeLink.Extract(text);
            var open=new Intent(this,typeof(MainActivity));open.SetAction(global::Android.Content.Intent.ActionSend);
            open.PutExtra(global::Android.Content.Intent.ExtraText,link);open.AddFlags(ActivityFlags.ClearTop|ActivityFlags.SingleTop);
            StartActivity(open);
        }
        catch(Exception error)
        {
            Toast.MakeText(this,error is InvalidOperationException?error.Message:"Couldn't open this share. Share a recipe page link and try again.",ToastLength.Long).Show();
        }
        finally{Finish();}
    }
}
