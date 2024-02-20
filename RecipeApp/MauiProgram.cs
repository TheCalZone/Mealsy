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
                        // Assuming CustomEditor is your Editor control that should handle the Enter key press
                        EditorHandler.Mapper.AppendToMapping("HandleEnterPress", (handler, view) =>
                        {
                            if (view is CustomEditor customEditor)
                            {
#if ANDROID
                                // Example for Android: listen for the EditorAction event
                                handler.PlatformView.EditorAction += (sender, args) =>
                                {
                                    if (args.ActionId == Android.Views.InputMethods.ImeAction.Done)
                                    {
                                        // Execute the command bound to the CustomEditor
                                        customEditor.EnterPressedCommand?.Execute(null);
                                        args.Handled = true;
                                    }
                                };
#elif IOS || MACCATALYST
                        // For iOS, you might need to subclass the EditorHandler to intercept the return key
#elif WINDOWS
                        // Windows platform-specific implementation
#endif
                            }
                        });
                    }).
                    ConfigureMauiHandlers(handlers =>
                    {
                        handlers.AddHandler(typeof(CustomEditor), typeof(CustomEditorHandler));
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
             CrossFirebase.Initialize();
                //"Mealsy" ,new Firebase.Core.Options("1:1067194949884:ios:42663f840336cbe6aab779", "1067194949884")
                //{
                //    ApiKey = "AIzaSyD-9tSrZ0wZ2j8Q9x5hU4JGzr3f3cQb3d8",
                //    ProjectId = "mealsy-b732b",
                //    BundleId = "com.companyname.recipeapp",
                //    ClientId = "1067194949884-pbr5bbd66ka2hqros2khfpa5ccvdp825.apps.googleusercontent.com",
                //    GoogleAppId = "1:1067194949884:ios:42663f840336cbe6aab779"
                //}
                CrossFirebaseCrashlytics.Current.SetCrashlyticsCollectionEnabled(true);
            return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) => 
            {
                CrossFirebase.Initialize(activity);
                CrossFirebaseCrashlytics.Current.SetCrashlyticsCollectionEnabled(true);
            }));
#endif
        });


        builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
        return builder;
    }


}