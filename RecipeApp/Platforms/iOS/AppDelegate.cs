using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Firebase.Core;
using UIKit;

namespace RecipeApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    //public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    //{
    //    Firebase.Core.App.Configure(new Options("1:1067194949884:ios:42663f840336cbe6aab779","1067194949884")
    //    {
    //        ApiKey = "AIzaSyDfws5SlkWB0Vtt47YT39lbOwrhqNJttIE",
    //        ProjectId = "mealsy-b732b",
    //        BundleId = "com.companyname.RecipeApp",
    //        ClientId = "1067194949884-pbr5bbd66ka2hqros2khfpa5ccvdp825.apps.googleusercontent.com"
    //    }); // Configure Firebase when the app launches
    //    return base.FinishedLaunching(app, options);
    //}
}
