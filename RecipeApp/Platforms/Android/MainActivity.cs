
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Firebase;
using Microsoft.Maui;
using System.Diagnostics;

namespace RecipeApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        try
        {
            base.OnCreate(savedInstanceState);
            System.Diagnostics.Debug.WriteLine("Initializing Firebase...");
            FirebaseApp.InitializeApp(this);
            System.Diagnostics.Debug.WriteLine("Firebase initialized successfully.");
            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing Firebase: {ex.Message}");
        }
    }


}
