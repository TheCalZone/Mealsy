using Foundation;
using Microsoft.Maui;
using UIKit;

namespace RecipeApp;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        // MAUI creates the window and forwards its lifecycle before navigation.
        base.WillConnect(scene, session, connectionOptions);
        HandleLinks(connectionOptions?.UrlContexts);
    }

    public override bool OpenUrl(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
    {
        // Retain MAUI/WebAuthenticator forwarding as well as our custom routes.
        var handled = base.OpenUrl(scene, urlContexts);
        return HandleLinks(urlContexts) || handled;
    }

    private static bool HandleLinks(NSSet<UIOpenUrlContext> contexts)
    {
        if (contexts == null) return false;
        var handled = false;
        foreach (var context in contexts)
            handled = Platforms.iOS.IosLinkNavigation.Handle(context.Url?.AbsoluteString) || handled;
        return handled;
    }
}
