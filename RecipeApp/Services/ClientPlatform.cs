namespace RecipeApp.Services;

/// <summary>Keep platform-only actions out of clients that cannot fulfil them.</summary>
public static class ClientPlatform
{
    public static bool StorePurchasesAvailable => OperatingSystem.IsAndroid();
    public static bool HomeScreenWidgetAvailable => OperatingSystem.IsAndroid()
#if IOS_EXTENSIONS
        || OperatingSystem.IsIOS()
#endif
        ;
    public static bool PhonePushAvailable => OperatingSystem.IsAndroid();
    public const string IosPurchaseNotice="App Store purchases are not available in this iOS build. Existing account entitlements and a household owner's active plan still apply when you sign in. Personal recipes, shopping and basic meal planning remain free.";
}
