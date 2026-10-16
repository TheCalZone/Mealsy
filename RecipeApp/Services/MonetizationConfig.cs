namespace RecipeApp.Services;

public static class MonetizationConfig
{
    public const string ProductId = "mealsy_family";
    public const string BasePlanId = "annual";
    public const string MonthlyBasePlanId = "monthly";
    public const string BillingEndpoint = "https://us-central1-mealsy-b732b.cloudfunctions.net/billing";
    public const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
    // Configure your own IDs, app-ads.txt, privacy policy, and UMP messages before enabling live ads.
    public static bool LiveAdsConfigured => false;
    public static string LiveBannerId => "";
    public static string PrivacyPolicyUrl => "";
    public const string TermsUrl = "https://play.google.com/about/play-terms/";
}
