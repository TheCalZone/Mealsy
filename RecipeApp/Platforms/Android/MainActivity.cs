
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Firebase;
using Microsoft.Maui;
using System.Diagnostics;

namespace RecipeApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
        {
        base.OnCreate(savedInstanceState);
        Platforms.Android.WidgetNavigation.FromIntent(Intent);
        }


    protected override void OnNewIntent(global::Android.Content.Intent intent)
    {
        base.OnNewIntent(intent); Intent=intent; Platforms.Android.WidgetNavigation.FromIntent(intent);
    }

}
