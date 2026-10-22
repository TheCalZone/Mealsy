namespace RecipeApp.Services;

public static class MonetizationConfig
{
    public const string SupportEmail = "Callum@brightbark.co.uk";
    public const string TrialOfferId = "seven-day-trial";
    public const string ProductId = "mealsy_family";
    public const string BasePlanId = "annual";
    public const string MonthlyBasePlanId = "monthly";
    public const string BillingEndpoint = "https://us-central1-mealsy-b732b.cloudfunctions.net/billing";
    public const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
    // Configure your own IDs, app-ads.txt, privacy policy, and UMP messages before enabling live ads.
    public static bool LiveAdsConfigured => false;
    public static string LiveBannerId => "";
    public static string PrivacyPolicyUrl => "https://thecalzone.github.io/Mealsy/privacy/";
    public const string AccountDeletionUrl = "https://thecalzone.github.io/Mealsy/delete-account/";
    public const string TermsUrl = "https://play.google.com/about/play-terms/";
}
