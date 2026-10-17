using RecipeApp.Services;
namespace RecipeApp.Views;

public class PlanPage : ContentPage
{
    private readonly Label status=ShoppingUi.Text("Checking your plan…");
    private readonly Button monthly, annual;
    private bool busy;
    public PlanPage()
    {
        Title="Mealsy Family"; ShoppingUi.Theme(this);
        monthly=ShoppingUi.Button("Check monthly price",()=>Subscribe(MonetizationConfig.MonthlyBasePlanId));
        annual=ShoppingUi.Button("Check annual price",()=>Subscribe(MonetizationConfig.BasePlanId));
        monthly.IsEnabled=annual.IsEnabled=false;
        var body=new VerticalStackLayout {Padding=20,Spacing=16,Children={
            ShoppingUi.Text("Make room for everyone",29,true),
            ShoppingUi.Text("One owner subscription. Family shopping and an ad-free household.",17),
            ShoppingUi.Card(new VerticalStackLayout {Spacing=8,Children={ShoppingUi.Text("MONTHLY",12,true),ShoppingUi.Text("£3 per month",25,true),ShoppingUi.Text("UK price · billed monthly",13),monthly}}),
            ShoppingUi.Card(new VerticalStackLayout {Spacing=8,Children={ShoppingUi.Text("ANNUAL · BEST VALUE",12,true),ShoppingUi.Text("£24 per year",25,true),ShoppingUi.Text("£2/month equivalent, billed annually. Save £12/year versus twelve £3 monthly payments at these UK prices.",13),annual}}),
            ShoppingUi.Text("Both options include exactly the same Family features. Google Play confirms your actual local price and billing period before payment.",13),
            ShoppingUi.Button("See everything included",async()=>await Navigation.PushAsync(new UpgradeFeaturesPage())),status,
            ShoppingUi.Button("Restore purchases",()=>Run(async()=>{
#if ANDROID
                await RecipeApp.Platforms.Android.PlayBillingService.Instance.Restore();
#endif
                await Refresh();
            }),true),
            ShoppingUi.Button("Manage or cancel in Google Play",async()=>await Launcher.Default.OpenAsync($"https://play.google.com/store/account/subscriptions?sku={MonetizationConfig.ProductId}&package=com.companyname.recipeapp"),true),
            ShoppingUi.Text("Subscriptions renew automatically each month or year, according to the option you choose. Cancel in Google Play before renewal to avoid the next charge. Cancellation keeps access until the paid period ends. No free trial is included. Existing subscribers can manage their subscription in Google Play; this page does not sell a second subscription.",13),
            ShoppingUi.Button("Ad privacy choices",async()=>{
#if ANDROID
                await RecipeApp.Platforms.Android.AdConsent.PrivacyChoices();
#endif
            },true),
            ShoppingUi.Button("Privacy policy",async()=>{
                if(string.IsNullOrWhiteSpace(MonetizationConfig.PrivacyPolicyUrl)){await DisplayAlert("Privacy policy","The publisher hasn't supplied a privacy policy link yet. Live advertising remains disabled.","OK");return;}
                await Launcher.Default.OpenAsync(MonetizationConfig.PrivacyPolicyUrl);
            },true),
            ShoppingUi.Button("Google Play terms",async()=>await Launcher.Default.OpenAsync(MonetizationConfig.TermsUrl),true)
        }};
        Content=new ScrollView {Content=body};
    }
    protected override async void OnAppearing(){base.OnAppearing();await Run(Refresh);}
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;var uid=HouseholdService.UserId;var previousMonthly=monthly.IsEnabled;var previousAnnual=annual.IsEnabled;monthly.IsEnabled=annual.IsEnabled=false;
        try{await action();}catch(Exception e){status.Text=e is InvalidOperationException or HouseholdException ? e.Message : "Your plan couldn't be checked. Connect and try Restore purchases.";if(uid==HouseholdService.UserId){monthly.IsEnabled=previousMonthly;annual.IsEnabled=previousAnnual;}}
        finally{busy=false;}
    }
    private async Task Refresh()
    {
        monthly.IsEnabled=annual.IsEnabled=false;
        if(HouseholdService.UserId==null){status.Text="Sign in on the Account tab before subscribing.";return;}
        var plan=await PlanService.RefreshAsync();
        status.Text=plan.FamilyEnabled ? plan.OwnPlan ? $"Your {(plan.BasePlanId=="monthly" ? "monthly" : "annual")} Family plan is active." : "Your household owner's plan covers you. No second subscription is needed."
            : !plan.Owner ? "Your household owner needs to renew or restore their plan." : "Free plan · Choose monthly or annual to unlock Family.";
        monthly.IsVisible=annual.IsVisible=!plan.OwnPlan && !plan.FamilyEnabled && plan.Owner;
        if(!monthly.IsVisible)return;
#if ANDROID
        foreach(var option in new[]{(monthly,MonetizationConfig.MonthlyBasePlanId,"month"),(annual,MonetizationConfig.BasePlanId,"year")})
        {
            try{var price=await RecipeApp.Platforms.Android.PlayBillingService.Instance.LoadPrice(option.Item2);option.Item1.Text=$"Subscribe — {price}/{option.Item3}";option.Item1.IsEnabled=true;}
            catch(Exception){option.Item1.Text=$"{(option.Item3=="month"?"Monthly":"Annual")} plan unavailable in Google Play";}
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
        body.Add(ShoppingUi.Text("Your food life, together",29,true));
        body.Add(ShoppingUi.Text("Family · £3/month or £24/year in the UK. Same benefits on both plans; one subscription held by the household owner.",16));
        void Card(string title,string detail)=>body.Add(ShoppingUi.Card(new VerticalStackLayout {Spacing=7,Children={ShoppingUi.Text(title,20,true),ShoppingUi.Text(detail)}}));
        Card("One shared household shopping list","Up to 20 members, including the owner, can add, edit, remove and collect items. Foreground sync, offline pending edits and conflict recovery keep everyone working from the same list.");
        Card("Invite your family","Create a household, send single-use invitation codes, revoke invitations and manage members. Each person has their own secure login.");
        Card("Ad-free for your household","The subscriber and members covered by the owner's active plan use Mealsy without banner advertisements.");
        Card("Keep your personal space","Every member keeps their own personal shopping list, recipes and local meal plan. Joining a household doesn't publish private recipes or notes.");
        Card("All your free tools remain included","Recipe book and editor, photos, drafts, ingredient parsing, cooking view, shopping creator, cupboard checks, snacks and essentials, quantity totals, meal planning, Android quick-add widget, food feed, likes/comments, and saving shared recipes.");
        Card("Restore and manage your plan","Restore Google Play purchases and manage renewal or cancellation in Google Play. Paid access continues to the end of a cancelled subscription's paid period.");
        body.Add(ShoppingUi.Text("Free accounts keep all personal tools and the signed-in food community, with advertising. Family adds household sharing and ad removal. Meal plans and recipe books aren't shared household workspaces. AI suggestions use your own OpenAI key and are billed separately by OpenAI. Household quick-add requires active Family access; personal widget use is free.",13));
        body.Add(ShoppingUi.Button("View subscription options",async()=>await Navigation.PushAsync(new PlanPage())));
        Content=new ScrollView {Content=body};
    }
}
