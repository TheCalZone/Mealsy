namespace RecipeApp.Platforms.iOS;

internal static class IosLinkNavigation
{
    public static bool Handle(string url)
    {
#if IOS_EXTENSIONS
        if (ExtensionNavigation.Handle(url)) return true;
#endif
        return InvitationNavigation.Handle(url);
    }
}
