using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using Android.Gms.Tasks;
using RecipeApp.Services;
namespace RecipeApp.Platforms.Android;
[Service(Exported=false)]
[IntentFilter(new[]{"com.google.firebase.MESSAGING_EVENT"})]
public class ActivityNotificationService:FirebaseMessagingService
{
    public override void OnNewToken(string token){base.OnNewToken(token);_=ActivityNotifications.Refresh();}
    public override void OnMessageReceived(RemoteMessage message){
        if(!ProductFeatures.SocialEnabled)return;
        if(!Preferences.Get("Mealsy.push.enabled",false)||!message.Data.TryGetValue("recipient",out var recipient)||Preferences.Get("Mealsy.push.account","")!=recipient)return;
        if(!message.Data.TryGetValue("eventId",out var eventId)||!System.Text.RegularExpressions.Regex.IsMatch(eventId,@"^[a-f0-9]{64}$"))return;
        var key="Mealsy.push.seen."+recipient;var seen=Preferences.Get(key,"").Split('|',StringSplitOptions.RemoveEmptyEntries).ToList();if(seen.Contains(eventId))return;
        var context=global::Android.App.Application.Context;var manager=(NotificationManager)context.GetSystemService(Context.NotificationService);
        if(OperatingSystem.IsAndroidVersionAtLeast(26))manager.CreateNotificationChannel(new NotificationChannel("mealsy_activity","Mealsy activity",NotificationImportance.Default));
        var intent=new Intent(context,typeof(MainActivity));intent.SetAction("com.mealsy.activity");intent.PutExtra("mealsy_activity",true);intent.AddFlags(ActivityFlags.ClearTop|ActivityFlags.SingleTop);
        var pending=PendingIntent.GetActivity(context,731,intent,PendingIntentFlags.UpdateCurrent|PendingIntentFlags.Immutable);
        var notification=new NotificationCompat.Builder(context,"mealsy_activity").SetSmallIcon(Resource.Drawable.ic_stat_mealsy).SetContentTitle("Mealsy").SetContentText("You have new activity. Open Mealsy to see your comments and follows.").SetContentIntent(pending).SetAutoCancel(true).Build();
        if(NotificationManagerCompat.From(context).AreNotificationsEnabled()){manager.Notify(731,notification);seen.Add(eventId);Preferences.Set(key,string.Join("|",seen.TakeLast(50)));}
    }
}
public static class ActivityNotifications
{
    private const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/notifications";
    private sealed class NotificationPermission:Permissions.BasePlatformPermission{public override (string androidPermission,bool isRuntime)[] RequiredPermissions=>Build.VERSION.SdkInt>=BuildVersionCodes.Tiramisu?new[]{("android.permission.POST_NOTIFICATIONS",true)}:Array.Empty<(string,bool)>();}
    private sealed class TokenListener:Java.Lang.Object,IOnCompleteListener{
        public readonly TaskCompletionSource<string> Completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnComplete(global::Android.Gms.Tasks.Task task){if(task.IsSuccessful)Completion.TrySetResult(task.Result?.ToString());else Completion.TrySetException(new InvalidOperationException("Notification registration is unavailable. Retry shortly."));}
    }
    public static async System.Threading.Tasks.Task Enable(){
        if(!ProductFeatures.SocialEnabled)return;
        if(HouseholdService.UserId==null)throw new InvalidOperationException("Sign in before enabling notifications.");
        if(await Permissions.RequestAsync<NotificationPermission>()!=PermissionStatus.Granted)throw new InvalidOperationException("Allow notifications in your phone settings to receive activity alerts.");
        Preferences.Set("Mealsy.push.enabled",true);FirebaseMessaging.Instance.AutoInitEnabled=true;await Register();
    }
    private static async System.Threading.Tasks.Task Register(){
        var account=HouseholdService.UserId;if(account==null)return;
        using var listener=new TokenListener();FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(listener);
        var token=await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(20));AccountSession.Check(account);
        await HouseholdService.CallEndpointAsync<object>(Endpoint,new{action="registerDevice",token});AccountSession.Check(account);
        Preferences.Set("Mealsy.push.token",token);Preferences.Set("Mealsy.push.account",account);
    }
    public static async System.Threading.Tasks.Task Refresh(){try{if(!ProductFeatures.SocialEnabled){FirebaseMessaging.Instance.AutoInitEnabled=false;NotificationManagerCompat.From(global::Android.App.Application.Context).Cancel(731);if(Preferences.Get("Mealsy.push.enabled",false))await Disable();return;}if(Preferences.Get("Mealsy.push.enabled",false))await Register();}catch{/* Next app open retries; the Activity inbox remains available. */}}
    public static async System.Threading.Tasks.Task Disable(){
        var token=Preferences.Get("Mealsy.push.token","");Preferences.Set("Mealsy.push.account","");Preferences.Set("Mealsy.push.enabled",false);
        NotificationManagerCompat.From(global::Android.App.Application.Context).Cancel(731);
        if(token.Length>0&&HouseholdService.UserId!=null)try{await HouseholdService.CallEndpointAsync<object>(Endpoint,new{action="unregisterDevice",token});}catch{/* Local recipient check blocks notifications even when offline. */}
    }
}
