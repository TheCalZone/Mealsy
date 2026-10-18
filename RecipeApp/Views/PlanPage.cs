using RecipeApp.Services;
namespace RecipeApp.Views;

public class PlanPage : ContentPage
{
    private readonly Label status=ShoppingUi.Text("Checking your plan…");
    private readonly Button monthly, annual;
    private bool busy;
    public PlanPage()
    {
        Title="Mealsy Premium"; ShoppingUi.Theme(this);
        monthly=ShoppingUi.Button("Check monthly price",()=>Subscribe(MonetizationConfig.MonthlyBasePlanId));
        annual=ShoppingUi.Button("Check annual price",()=>Subscribe(MonetizationConfig.BasePlanId));
        monthly.IsEnabled=annual.IsEnabled=false;
        var body=new VerticalStackLayout {Padding=20,Spacing=16,Children={
            ShoppingUi.Text("Your kitchen, on repeat",29,true),
            ShoppingUi.Text("Save time cooking for one or sharing with others. One subscription includes Premium tools and Family sharing.",17),
            ShoppingUi.Card(new VerticalStackLayout {Spacing=8,Children={ShoppingUi.Text("MONTHLY",12,true),ShoppingUi.Text(SubscriptionPricing.Pounds(SubscriptionPricing.Monthly)+" per month",25,true),ShoppingUi.Text("UK reference price · billed monthly",13),monthly}}),
            ShoppingUi.Card(new VerticalStackLayout {Spacing=8,Children={ShoppingUi.Text("ANNUAL · BEST VALUE",12,true),ShoppingUi.Text(SubscriptionPricing.Pounds(SubscriptionPricing.Annual)+" per year",25,true),ShoppingUi.Text(SubscriptionPricing.AnnualDetails,13),annual}}),
            ShoppingUi.Text("Both options include exactly the same Premium features. The app store confirms your actual local price and billing period before payment.",13),
            ShoppingUi.Button("See everything included",async()=>await Navigation.PushAsync(new UpgradeFeaturesPage())),status,
            ShoppingUi.Button("Extra household places · "+SubscriptionPricing.Pounds(SubscriptionPricing.ExtraPlaceMonthly)+"/month each",async()=>await Navigation.PushAsync(new ExtraPlacesPage())),
            ShoppingUi.Button("Restore purchases",()=>Run(async()=>{
#if ANDROID
                await RecipeApp.Platforms.Android.PlayBillingService.Instance.Restore();
#endif
                await Refresh();
            }),true),
            ShoppingUi.Button("Manage or cancel in the app store",async()=>await Launcher.Default.OpenAsync($"https://play.google.com/store/account/subscriptions?sku={MonetizationConfig.ProductId}&package=com.companyname.recipeapp"),true),
            ShoppingUi.Text("Subscriptions renew automatically each month or year, according to the option you choose. Cancel in the app store before renewal to avoid the next charge. Cancellation keeps access until the paid period ends. No free trial is included. Existing subscribers can manage their subscription in the app store; this page does not sell a second subscription.",13),
            ShoppingUi.Button("Ad privacy choices",async()=>{
#if ANDROID
                await RecipeApp.Platforms.Android.AdConsent.PrivacyChoices();
#endif
            },true),
            ShoppingUi.Button("Privacy policy",async()=>{
                if(string.IsNullOrWhiteSpace(MonetizationConfig.PrivacyPolicyUrl)){await DisplayAlert("Privacy policy","The publisher hasn't supplied a privacy policy link yet. Live advertising remains disabled.","OK");return;}
                await Launcher.Default.OpenAsync(MonetizationConfig.PrivacyPolicyUrl);
            },true),
            ShoppingUi.Button("Store terms",async()=>await Launcher.Default.OpenAsync(MonetizationConfig.TermsUrl),true)
        }};
        if(!ClientPlatform.StorePurchasesAvailable)
        {
            // Keep price/feature information and verified status, but no inert purchase actions.
            foreach(var child in body.Children.ToList())if(child is Button button &&
                (button.Text.StartsWith("Extra household")||button.Text=="Restore purchases"||button.Text.StartsWith("Manage or cancel")||button.Text=="Ad privacy choices"||button.Text=="Store terms"))body.Remove(child);
            monthly.IsVisible=annual.IsVisible=false;
            body.Insert(2,ShoppingUi.Card(ShoppingUi.Text(ClientPlatform.IosPurchaseNotice,14)));
            body.Add(ShoppingUi.Button("Refresh my access",()=>Run(Refresh),true));
        }
        Content=new ScrollView {Content=body};
    }
    protected override async void OnAppearing(){base.OnAppearing();await Run(Refresh);}
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;var uid=HouseholdService.UserId;var previousMonthly=monthly.IsEnabled;var previousAnnual=annual.IsEnabled;monthly.IsEnabled=annual.IsEnabled=false;
        try{await action();}catch(Exception e){status.Text=e is InvalidOperationException or HouseholdException ? e.Message : "Your plan couldn't be checked. Connect and refresh your access.";if(uid==HouseholdService.UserId){monthly.IsEnabled=previousMonthly;annual.IsEnabled=previousAnnual;}}
        finally{busy=false;}
    }
    private async Task Refresh()
    {
        monthly.IsEnabled=annual.IsEnabled=false;
        if(HouseholdService.UserId==null){status.Text=ClientPlatform.StorePurchasesAvailable?"Sign in on the Account tab before subscribing.":"Sign in on the Account tab to check your existing access.";return;}
        var plan=await PlanService.RefreshAsync();
        status.Text=(plan.OwnPlan||plan.FamilyEnabled) ? plan.OwnPlan ? plan.BasePlanId=="admin" ? "Your administrator-granted Premium access is active." : $"Your {(plan.BasePlanId=="monthly" ? "monthly" : "annual")} Premium plan is active." : "Your household owner's plan covers you. No second subscription is needed."
            : !plan.Owner ? "Subscribe for personal Premium tools, or ask your household owner to renew their plan." : "Free plan · Choose monthly or annual to unlock Premium.";
        monthly.IsVisible=annual.IsVisible=!plan.OwnPlan && !plan.FamilyEnabled;
        if(!ClientPlatform.StorePurchasesAvailable){monthly.IsVisible=annual.IsVisible=false;if(!plan.OwnPlan&&!plan.FamilyEnabled)status.Text="Free account · App Store subscriptions are not available in this build.";return;}
        if(!monthly.IsVisible)return;
#if ANDROID
        foreach(var option in new[]{(monthly,MonetizationConfig.MonthlyBasePlanId,"month"),(annual,MonetizationConfig.BasePlanId,"year")})
        {
            try{var price=await RecipeApp.Platforms.Android.PlayBillingService.Instance.LoadPrice(option.Item2);option.Item1.Text=$"Subscribe — {price}/{option.Item3}";option.Item1.IsEnabled=true;}
            catch(Exception){option.Item1.Text=$"{(option.Item3=="month"?"Monthly":"Annual")} plan unavailable in the app store";}
        }
#endif
    }
    private Task Subscribe(string basePlan)=>Run(async()=>{
#if ANDROID
        await RecipeApp.Platforms.Android.PlayBillingService.Instance.Buy(basePlan);
#endif
        await Refresh();
    });
}

public class UpgradeFeaturesPage : ContentPage
{
    public UpgradeFeaturesPage()
    {
        Title="What's included";ShoppingUi.Theme(this);
        var body=new VerticalStackLayout {Padding=20,Spacing=16};
        body.Add(ShoppingUi.Text("More useful every week",29,true));
        if(!ClientPlatform.StorePurchasesAvailable)body.Add(ShoppingUi.Text(ClientPlatform.IosPurchaseNotice,14));
        body.Add(ShoppingUi.Text("Premium · "+SubscriptionPricing.Summary+" in the UK. Same benefits on both plans; one subscription for a solo cook or a household owner.",16));
        void Card(string title,string detail)=>body.Add(ShoppingUi.Card(new VerticalStackLayout {Spacing=7,Children={ShoppingUi.Text(title,20,true),ShoppingUi.Text(detail)}}));
        Card("Import recipes from supported websites","Bring a recipe link straight into the editor for review. Keep ingredients, portions and method in your private recipe book. Some sources block imports; manual entry and ingredient paste remain free.");
        Card("Reusable weekly meal-plan templates","Save favourite weeks with meals, portions and notes. Reapply a template to your current week after confirming replacement. Your templates are private to your account on this device.");
        Card("Saved shopping trips","Save named personal shopping lists and add them again with quantities and notes. Items reset to uncollected; existing items remain and compatible amounts combine. Saved trips stay private on this device.");
        Card("One shared household shopping list","Four people, including the owner, can add, edit, remove and collect items. Extra places are planned at £0.99/month each and become available once their app-store purchase is configured. Foreground sync, offline pending edits and conflict recovery keep everyone working from the same list.");
        Card("Invite your family","Create a household, send single-use invitation codes, revoke invitations and manage members. Each person has their own secure login.");
        Card("Ad-free for your household","The subscriber and members covered by the owner's active plan use Mealsy without banner advertisements.");
        Card("Keep your personal space","Every member keeps their own personal shopping list, recipes and local meal plan. Joining a household doesn't publish private recipes or notes.");
        Card("Your everyday free tools remain included","Recipe book and editor, photos, drafts, ingredient parsing, cooking view, shopping creator, cupboard checks, snacks and essentials, quantity totals, meal planning, Android quick-add widget and household shopping widgets.");
        if(ClientPlatform.StorePurchasesAvailable)Card("Restore and manage your plan","Restore the app store purchases and manage renewal or cancellation in the app store. Paid access continues to the end of a cancelled subscription's paid period.");
        body.Add(ShoppingUi.Text("Free accounts keep basic personal tools, with advertising. Premium adds recipe-link imports, weekly templates, saved shopping trips, household sharing and ad removal. You do not need to create a household to use Premium. Meal plans and recipe books aren't shared household workspaces. AI suggestions use your own AI service key and are billed separately by that service. Household quick-add requires active Family access; personal widget use is free.",13));
        body.Add(ShoppingUi.Button("View subscription options",async()=>await Navigation.PushAsync(new PlanPage())));
        Content=new ScrollView {Content=body};
    }
}
