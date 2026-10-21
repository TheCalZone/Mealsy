using Foundation;
using Microsoft.Maui;
using UIKit;

namespace RecipeApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
#if IOS_EXTENSIONS
        if(Platforms.iOS.ExtensionNavigation.Handle(url?.AbsoluteString))return true;
#endif
        if(Platforms.iOS.InvitationNavigation.Handle(url?.AbsoluteString))return true;
        // Preserve native authentication/provider callback handling.
        return base.OpenUrl(application,url,options);
    }
}
