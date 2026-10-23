using RecipeApp.Services;
namespace RecipeApp.Views;

public class PlanPage : ContentPage
{
    private readonly Label status=ShoppingUi.Text("Checking your plan…");
    private readonly Button monthly, annual, signIn;
    private readonly Label monthlyPrice=ShoppingUi.Text(AppLocalization.Text("Checking store price…"),25,true);
    private readonly Label annualPrice=ShoppingUi.Text(AppLocalization.Text("Checking store price…"),25,true);
    private readonly LoadingIndicator loading=new("Checking plans & prices…",false);
    private bool busy;
    private readonly Dictionary<string,SubscriptionQuote> quotes=new();
    private readonly Label monthlyTerms=ShoppingUi.Text("Google Play confirms the price and any eligible trial before payment.",13);
    private readonly Label annualTerms=ShoppingUi.Text("Google Play confirms the price and any eligible trial before payment.",13);
    public PlanPage()
    {
        Title="Mealsy Premium"; ShoppingUi.Theme(this);
        signIn=ShoppingUi.Button("Sign in to manage Premium",()=>AppNavigation.Root("AccountPage"),true);signIn.IsVisible=false;
        monthly=ShoppingUi.Button("Check monthly price",()=>Subscribe(MonetizationConfig.MonthlyBasePlanId));
        annual=ShoppingUi.Button("Check annual price",()=>Subscribe(MonetizationConfig.BasePlanId));
        monthly.IsEnabled=annual.IsEnabled=false;
        var body=new VerticalStackLayout {Padding=20,Spacing=16,Children={
            ShoppingUi.Text("Your kitchen, on repeat",29,true),
            ShoppingUi.Text("Save time cooking for one or sharing with others. One subscription includes Premium tools and Family sharing.",17),
            ShoppingUi.Card(new VerticalStackLayout {Spacing=8,Children={ShoppingUi.Text(AppLocalization.Text("MONTHLY"),12,true),monthlyPrice,monthlyTerms,monthly}}),
            ShoppingUi.Card(new VerticalStackLayout {Spacing=8,Children={ShoppingUi.Text(AppLocalization.Text("ANNUAL"),12,true),annualPrice,annualTerms,annual}}),
            ShoppingUi.Text("Both options include exactly the same Premium features. The app store confirms your actual local price and billing period before payment.",13),
            ShoppingUi.Button("See everything included",async()=>await Navigation.PushAsync(new UpgradeFeaturesPage(true))),loading,status,signIn,
            ShoppingUi.Button("Refresh plans & prices",()=>Run(Refresh),true),
            ShoppingUi.Button("Extra household places · Check store price",async()=>await Navigation.PushAsync(new ExtraPlacesPage())),
            ShoppingUi.Button(AppLocalization.Text("Restore purchases"),()=>Run(async()=>{
#if ANDROID
                await RecipeApp.Platforms.Android.PlayBillingService.Instance.Restore();
#endif
                await Refresh();
            }),true),
            ShoppingUi.Button("Manage or cancel in the app store",async()=>await Launcher.Default.OpenAsync($"https://play.google.com/store/account/subscriptions?sku={MonetizationConfig.ProductId}&package=com.companyname.recipeapp"),true),
            ShoppingUi.Text("Subscriptions renew automatically each month or year, according to the option you choose. Cancel in the app store before renewal to avoid the next charge. Cancellation keeps access until the paid period ends. A 7-day free trial is shown only when Google Play returns an eligible offer. Payment details are handled by Google Play; Mealsy does not collect card details. Cancel before the trial ends to avoid the first charge. Existing subscribers can manage their subscription in the app store; this page does not sell a second subscription.",13),
            ShoppingUi.Button("Ad privacy choices",async()=>{
#if ANDROID
                await RecipeApp.Platforms.Android.AdConsent.PrivacyChoices();
#endif
            },true),
            ShoppingUi.Button("Privacy policy",async()=>{
                if(string.IsNullOrWhiteSpace(MonetizationConfig.PrivacyPolicyUrl)){await AppNavigation.Open(new PrivacyAndDataPage());return;}
                await Launcher.Default.OpenAsync(MonetizationConfig.PrivacyPolicyUrl);
            },true),
            ShoppingUi.Button("Store terms",async()=>await Launcher.Default.OpenAsync(MonetizationConfig.TermsUrl),true)
        }};
        if(!MonetizationConfig.LiveAdsConfigured)
            foreach(var child in body.Children.OfType<Button>().Where(button=>button.Text=="Ad privacy choices").ToList())body.Remove(child);
        if(!ClientPlatform.StorePurchasesAvailable)
        {
            monthlyPrice.Text=AppLocalization.Text("Monthly membership");annualPrice.Text=AppLocalization.Text("Annual membership");monthlyTerms.Text=annualTerms.Text="App Store subscriptions and trials are not available in this build.";
            // Keep price/feature information and verified status, but no inert purchase actions.
            foreach(var child in body.Children.ToList())if(child is Button button &&
                (button.Text.StartsWith("Extra household")||button.Text==AppLocalization.Text("Restore purchases")||button.Text.StartsWith("Manage or cancel")||button.Text=="Ad privacy choices"||button.Text=="Store terms"))body.Remove(child);
            monthly.IsVisible=annual.IsVisible=false;
            body.Insert(2,ShoppingUi.Card(ShoppingUi.Text(ClientPlatform.IosPurchaseNotice,14)));
        }
        Content=new ScrollView {Content=body};
    }
    protected override async void OnAppearing(){base.OnAppearing();await Run(Refresh);}
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;loading.IsBusy=true;monthly.IsEnabled=annual.IsEnabled=false;
        try{await action();}catch(Exception e){status.Text=e is InvalidOperationException or HouseholdException ? e.Message : "Your plan couldn't be checked. Connect and refresh your access.";monthly.IsEnabled=annual.IsEnabled=false;}
        finally{busy=false;loading.IsBusy=false;}
    }
    private async Task Refresh()
    {
        monthly.IsEnabled=annual.IsEnabled=false;quotes.Clear();
        var account=HouseholdService.UserId;signIn.IsVisible=account==null;
        monthly.IsVisible=annual.IsVisible=ClientPlatform.StorePurchasesAvailable;
        PlanStatus plan=null;
        async Task Access()
        {
            if(account==null){monthly.Text=annual.Text="Sign in to subscribe";status.Text="Sign in on the Account tab to manage Premium.";return;}
            plan=await PlanService.RefreshAsync();AccountSession.Check(account);
            status.Text=(plan.OwnPlan||plan.FamilyEnabled) ? plan.OwnPlan ? plan.BasePlanId=="admin" ? "Your administrator-granted Premium access is active." : $"Your {(plan.BasePlanId=="monthly" ? "monthly" : "annual")} Premium plan is active." : "Your household owner's plan covers you. No second subscription is needed."
                : !plan.Owner ? "Subscribe for personal Premium tools, or ask your household owner to renew their plan." : "Free plan · Choose monthly or annual to unlock Premium.";
            monthly.IsVisible=annual.IsVisible=ClientPlatform.StorePurchasesAvailable&&!plan.OwnPlan&&!plan.FamilyEnabled;
            if(!ClientPlatform.StorePurchasesAvailable&&!plan.OwnPlan&&!plan.FamilyEnabled)status.Text="Free account · App Store subscriptions are not available in this build.";
        }
        // Store availability must not delay an already verified account status.
        await Task.WhenAll(Access(),LoadPrices(account));
        AccountSession.Check(account);
        if(plan==null||!monthly.IsVisible)return;
        foreach(var option in new[]{(monthly,monthlyPrice,"Monthly"),(annual,annualPrice,"Annual")})
        {
            option.Item1.IsEnabled=option.Item2.Text!=AppLocalization.Text("Price unavailable");
            option.Item1.Text=option.Item1.IsEnabled?(quotes[option.Item3=="Monthly"?MonetizationConfig.MonthlyBasePlanId:MonetizationConfig.BasePlanId].SevenDayTrial?"Start 7-day free trial":$"Subscribe · {option.Item2.Text}"):$"{option.Item3} plan unavailable in Google Play";
        }
    }
    private async Task LoadPrices(string account)
    {
#if ANDROID
        foreach(var option in new[]{(monthlyPrice,MonetizationConfig.MonthlyBasePlanId,"month"),(annualPrice,MonetizationConfig.BasePlanId,"year")})
        {
            option.Item1.Text=AppLocalization.Text("Checking store price…");
            try{var quote=await RecipeApp.Platforms.Android.PlayBillingService.Instance.LoadQuote(option.Item2).WaitAsync(TimeSpan.FromSeconds(15));AccountSession.Check(account);quotes[option.Item2]=quote;option.Item1.Text=quote.DisplayPrice;(option.Item2==MonetizationConfig.MonthlyBasePlanId?monthlyTerms:annualTerms).Text=quote.Disclosure;}
            catch(Exception){AccountSession.Check(account);option.Item1.Text=AppLocalization.Text("Price unavailable");(option.Item2==MonetizationConfig.MonthlyBasePlanId?monthlyTerms:annualTerms).Text="Connect to Google Play and refresh to check prices and trial eligibility.";}
        }
#else
        await Task.CompletedTask;
#endif
    }
    private Task Subscribe(string basePlan)=>Run(async()=>{
#if ANDROID
        if(!quotes.TryGetValue(basePlan,out var quote))throw new InvalidOperationException("Refresh plans before subscribing.");
        await RecipeApp.Platforms.Android.PlayBillingService.Instance.Buy(basePlan,expected:quote);
#endif
        await Refresh();
    });
}

public class UpgradeFeaturesPage : ContentPage
{
    public UpgradeFeaturesPage(bool returnToPlans=false)
    {
        Title="What's included";ShoppingUi.Theme(this);
        var body=new VerticalStackLayout {Padding=20,Spacing=16};
        body.Add(ShoppingUi.Text("More useful every week",29,true));
        if(!ClientPlatform.StorePurchasesAvailable)body.Add(ShoppingUi.Text(ClientPlatform.IosPurchaseNotice,14));
        if(ClientPlatform.StorePurchasesAvailable)body.Add(ShoppingUi.Text("Check subscription options for a 7-day free trial. Trials are available only when Google Play confirms eligibility; renewal pricing and cancellation terms are shown before checkout.",14));
        body.Add(ShoppingUi.Text("Monthly or annual Premium membership. Both include the same benefits for a solo cook or a household owner. View subscription options for the current app-store price.",16));
        void Card(string title,string detail)=>body.Add(ShoppingUi.Card(new VerticalStackLayout {Spacing=7,Children={ShoppingUi.Text(title,20,true),ShoppingUi.Text(detail)}}));
        Card("Import recipes from supported websites","Bring a recipe link straight into the editor for review. Keep ingredients, portions and method in your private recipe book. Some sources block imports; manual entry and ingredient paste remain free.");
        Card("Reusable weekly meal-plan templates","Save favourite weeks with meals, portions and notes. Reapply a template to your current week after confirming replacement. Your templates are private to your account on this device.");
        Card("Saved shopping trips","Save named personal shopping lists and add them again with quantities and notes. Items reset to uncollected; existing items remain and compatible amounts combine. Saved trips stay private on this device.");
        Card("More personal shopping lists","Free includes two named personal lists. Premium lets you create additional lists for different shops, occasions or projects. Additional lists stay on this device; your main personal list retains its existing cloud sync. If Premium ends, existing lists remain readable and editable.");
        Card("One shared household shopping list","Four people, including the owner, can add, edit, remove and collect items. Optional extra places have a separate monthly subscription; check availability and the current price in the app store. Foreground sync, offline pending edits and conflict recovery keep everyone working from the same list.");
        Card("Shared household meal plans","Plan breakfast, lunch and dinner together, with portions, notes and optional recipe snapshots. Changes are saved online; refresh to see the latest household edits. Your personal meal plan stays separate.");
        Card("Invite your family","Create a household, send single-use invitation codes, revoke invitations and manage members. Each person has their own secure login.");
        Card("Ad-free for your household","The subscriber and members covered by the owner's active plan use Mealsy without banner advertisements.");
        Card("Keep your personal space","Every member keeps their own personal shopping list, recipes and local meal plan. Joining a household doesn't publish private recipes or notes.");
        Card("Your everyday free tools remain included","Recipe book and editor, photos, drafts, ingredient parsing, cooking view, shopping creator, cupboard checks, snacks and essentials, quantity totals, meal planning, Android quick-add widget and personal shopping widgets. Household widget access is included with Premium.");
        if(ClientPlatform.StorePurchasesAvailable)Card("Restore and manage your plan","Restore the app store purchases and manage renewal or cancellation in the app store. Paid access continues to the end of a cancelled subscription's paid period.");
        body.Add(ShoppingUi.Text("Free accounts keep basic personal tools, with advertising. Premium adds recipe-link imports, weekly templates, saved shopping trips, household sharing and ad removal. You do not need to create a household to use Premium. Your personal recipe book stays private. The household meal planner is a separate shared workspace. The local Use what you have finder searches your saved recipes for free, without an AI key. Household quick-add requires active Family access; personal widget use is free.",13));
        body.Add(ShoppingUi.Button(returnToPlans?"Back to subscription options":"View subscription options",async()=>{if(returnToPlans)await Navigation.PopAsync();else await Navigation.PushAsync(new PlanPage());}));
        Content=new ScrollView {Content=body};
    }
}
