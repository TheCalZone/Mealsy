using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using UraniumUI;
using Plugin.Firebase.Auth;
using Firebase.Auth;
using Plugin.Firebase.Core;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.Crashlytics;
using Microsoft.Maui.Handlers;

using RecipeApp.CustomViews;
using RecipeApp.Custom_Handlers;
using RecipeApp.Services;




#if IOS
using Plugin.Firebase.Core.Platforms.iOS;
#elif ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

namespace RecipeApp;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppDiagnostics.RegisterManagedHandlers();
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            fonts.AddFont("Caveat-VariableFont_wght.ttf", "Caveat");
        })
        .UseMauiCommunityToolkit()
        .UseUraniumUI()
        .UseUraniumUIMaterial()
        .RegisterFirebaseServices()
        .ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(CustomEditor), typeof(CustomEditorHandler));
#if ANDROID
            handlers.AddHandler(typeof(Shell),typeof(RecipeApp.Platforms.Android.MealsyShellRenderer));
            handlers.AddHandler(typeof(RecipeApp.Views.RecipePressLayout), typeof(RecipeApp.Platforms.Android.RecipePressHandler));
            handlers.AddHandler(typeof(RecipeApp.Controls.AdBanner), typeof(RecipeApp.Platforms.Android.AdBannerHandler));
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events => {
#if IOS
            events.AddiOS(iOS => iOS.FinishedLaunching((_, __) => {
                try
                {
                    CrossFirebase.Initialize();
                    AppDiagnostics.ApplyCollection();
                }
                catch(Exception error)
                {
                    AppDiagnostics.Report(error,"sdk.startup","SDK_STARTUP");
                    System.Diagnostics.Debug.WriteLine("Firebase initialization unavailable; local tools remain available.");
                }
            return true;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) => 
            {
                try
                {
                    CrossFirebase.Initialize(activity);
                    AppDiagnostics.ApplyCollection();
                }
                catch (Exception error)
                {
                    AppDiagnostics.Report(error,"sdk.startup","SDK_STARTUP");
                    System.Diagnostics.Debug.WriteLine("Firebase initialization unavailable; local tools remain available.");
                }
            }));
#endif
        });


        builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
        return builder;
    }


}
