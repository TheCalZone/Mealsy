using Android.Gms.Tasks;
using Android.Runtime;
using Firebase.AppCheck;
using Firebase.AppCheck.PlayIntegrity;
namespace RecipeApp.Platforms.Android;
public static class PromotionAppVerification
{
    private static bool initialized;
    private sealed class Listener:Java.Lang.Object,IOnCompleteListener
    {
        public readonly TaskCompletionSource<string> Completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnComplete(global::Android.Gms.Tasks.Task task){
            if(task.IsSuccessful){using var token=task.Result.JavaCast<AppCheckToken>();Completion.TrySetResult(token.Token);}
            else Completion.TrySetException(new InvalidOperationException("Device verification isn't available for this installation. Paid impressions won't be charged."));
        }
    }
    public static async System.Threading.Tasks.Task<string> Token(){
        if(!initialized){FirebaseAppCheck.Instance.InstallAppCheckProviderFactory(PlayIntegrityAppCheckProviderFactory.Instance);initialized=true;}
        using var listener=new Listener();FirebaseAppCheck.Instance.GetAppCheckToken(false).AddOnCompleteListener(listener);
        return await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
