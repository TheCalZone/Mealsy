using RecipeApp.Services;

namespace RecipeApp.Views;

public class PlanPage : ContentPage
{
    private readonly Label status = FoodFeedPage.Text("Checking your plan…");
    private readonly Button buy;
    private bool busy;
    public PlanPage()
    {
        Title = "Mealsy Family"; BackgroundColor = Color.FromArgb("#F5F7F2");
        buy = FoodFeedPage.Button("Check annual price", Subscribe);
        buy.IsEnabled = false;
        var body = new VerticalStackLayout { Padding = 24, Spacing = 16, Children = {
            FoodFeedPage.Text("More together. Fewer distractions.", 28, true),
            FoodFeedPage.Text("Mealsy Family · £24 per year in the UK", 22, true),
            FoodFeedPage.Text("One subscription held by the household owner covers up to 20 household members. Unlock a shared shopping list, family invitations and an ad-free app for everyone in the household."),
            FoodFeedPage.Text("Free includes your own recipes, personal shopping list, meal planning, the food community and advertising. Family sharing requires an active owner subscription."),
            status, buy, FoodFeedPage.Button("Restore purchases", Restore),
            FoodFeedPage.Button("Manage or cancel subscription", async () => await Launcher.Default.OpenAsync($"https://play.google.com/store/account/subscriptions?sku={MonetizationConfig.ProductId}&package=com.companyname.recipeapp")),
            FoodFeedPage.Button("Ad privacy choices", async () => {
#if ANDROID
                await RecipeApp.Platforms.Android.AdConsent.PrivacyChoices();
#endif
            }),
            FoodFeedPage.Button("Privacy policy", async () => {
                if (string.IsNullOrWhiteSpace(MonetizationConfig.PrivacyPolicyUrl)) { await DisplayAlert("Privacy policy", "The publisher has not supplied a privacy policy link yet. Live advertising remains disabled.", "OK"); return; }
                await Launcher.Default.OpenAsync(MonetizationConfig.PrivacyPolicyUrl);
            }),
            FoodFeedPage.Text("Google Play shows your local price before you confirm. This is an annual, automatically renewing subscription. Cancel in Google Play before renewal to avoid the next charge. Cancellation keeps access until the paid period ends. No free trial is offered by this plan.", 13),
            FoodFeedPage.Button("Google Play subscription terms", async () => await Launcher.Default.OpenAsync(MonetizationConfig.TermsUrl))
        } };
        Content = new ScrollView { Content = body };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Refresh); }
    private async Task Run(Func<Task> action)
    {
        if (busy) return; busy = true;
        try { await action(); }
        catch (HouseholdException e) { status.Text = e.Message; }
        catch (Exception e) { status.Text = e is InvalidOperationException ? e.Message : "Your plan could not be checked. Connect and try Restore purchases."; }
        finally { busy = false; }
    }
    private async Task Refresh()
    {
        if (HouseholdService.UserId == null) { status.Text = "Sign in on the Account tab before subscribing."; buy.IsEnabled = false; return; }
        buy.IsEnabled = false;
        var plan = await PlanService.RefreshAsync();
        status.Text = plan.FamilyEnabled ? plan.OwnPlan ? "Your annual plan is active and covers your household." : "Your household owner's plan covers you. Enjoy the ad-free app."
            : !plan.Owner ? "Your household owner needs to renew or restore their plan. You don't need a second subscription. To become a paying owner yourself, leave that household and create your own."
            : "Free plan · Family sharing is locked until you subscribe.";
        buy.IsVisible = !plan.OwnPlan && !plan.FamilyEnabled && plan.Owner;
        if (!buy.IsVisible) return;
#if ANDROID
        try { var price = await RecipeApp.Platforms.Android.PlayBillingService.Instance.LoadPrice(); buy.Text = $"Subscribe — {price} per year"; buy.IsEnabled = true; }
        catch (Exception) { buy.Text = "Annual plan unavailable in Google Play"; status.Text += " Install from Google Play and reopen this screen when the annual product is available."; }
#endif
    }
    private async Task Subscribe() => await Run(async () => {
#if ANDROID
        await RecipeApp.Platforms.Android.PlayBillingService.Instance.Buy();
        await Refresh();
#endif
    });
    private async Task Restore() => await Run(async () => {
#if ANDROID
        await RecipeApp.Platforms.Android.PlayBillingService.Instance.Restore();
        await Refresh();
#endif
    });
}
