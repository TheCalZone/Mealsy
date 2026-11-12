using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class AccountPage : ContentPage
{
    private readonly ToolbarItem setupItem;
    private string account;
    private RecipeApp.Models.CommunityProfile community;
    private bool administrator;
    private bool? emailVerified;
    private readonly HashSet<string> expanded=new();
    public AccountPage(){InitializeComponent();Title=AppLocalization.Text("Your account");ShoppingUi.Theme(this);setupItem=new ToolbarItem(AppLocalization.Text("Get started"),null,async()=>await AppNavigation.Open(new OnboardingPage())){Order=ToolbarItemOrder.Secondary,AutomationId="OnboardingSetup"};if(!OnboardingState.Completed)ToolbarItems.Add(setupItem);ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Share Mealsy"),null,async()=>await OnboardingState.ShareApp()){Order=ToolbarItemOrder.Secondary});ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Settings"),null,async()=>await AppNavigation.Open(new SettingsPage())));}
    private readonly LoadGeneration requests=new();
    private bool visible,loading;
    private string loadWarning;
    protected override async void OnAppearing()
    {
        if(OnboardingState.Completed)ToolbarItems.Remove(setupItem);
        base.OnAppearing();visible=true;await RefreshAccountAsync();
    }
    protected override void OnDisappearing(){visible=false;requests.Invalidate();base.OnDisappearing();}
    private Task SignedIn()
    {
        // The Account tab is already selected: its OnAppearing won't fire again.
        _=RefreshAccountAsync();return Task.CompletedTask;
    }
    private async Task RefreshAccountAsync()
    {
        if(!visible)return;
        var uid=AccountSession.UserId;var ticket=requests.Begin(uid);
        if(account!=uid){community=null;expanded.Clear();}
        account=uid;emailVerified=AccountSession.EmailVerified==true?true:null;administrator=false;loadWarning=null;loading=uid!=null;Render();
        if(uid==null)return;
        var unavailable=false;
        async Task Profile(){try{await AccountSession.LoadProfile();}catch(Exception){unavailable=true;}}
        async Task Community(){try{var saved=await FoodFeedService.Profile();if(visible&&requests.IsCurrent(ticket,AccountSession.UserId))community=saved;}catch(Exception){unavailable=true;}}
        async Task Role(){try{var saved=await AdminService.Status();if(visible&&requests.IsCurrent(ticket,AccountSession.UserId))administrator=saved.Admin;}catch(Exception){unavailable=true;}}
        async Task Verification(){try{var confirmed=await AccountSession.RefreshEmailVerification();if(visible&&requests.IsCurrent(ticket,AccountSession.UserId))emailVerified=confirmed;}catch(Exception){unavailable=true;}}
        try{await Task.WhenAll(Profile(),ProductFeatures.SocialEnabled?Community():Task.CompletedTask,Role(),Verification());}
        finally
        {
            if(visible&&requests.IsCurrent(ticket,AccountSession.UserId))
            {
                loading=false;loadWarning=unavailable?"Signed in. Some account details couldn't load. Your saved data is kept; retry when connected.":null;Render();
            }
        }
    }
    private void Render()
    {
        if(AccountSession.UserId==null){Content=new AuthFormView(false,SignedIn);return;}
        var profile=AccountSession.CachedProfile;var status=ShoppingUi.Text(AccountSession.Status,13);var body=new VerticalStackLayout{Padding=20,Spacing=16};
        body.Add(ShoppingUi.Text(AppLocalization.Text("Your place at the table"),28,true));body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text(string.IsNullOrWhiteSpace(profile.FirstName)?"Welcome to Mealsy":profile.FirstName+" "+profile.LastName,23,true),ShoppingUi.Text(AccountSession.Email??"",14),ShoppingUi.Text(AppLocalization.Text("Private account · Each family member uses their own login"),13)}}));
        if(ProductFeatures.SocialEnabled&&ContentSharing.HasPending)body.Add(ShoppingUi.Button("Open saved shared post or plan",ContentSharing.Resume));
        if(loading)body.Add(new LoadingIndicator("Updating profile and account permissions…"));
        if(loadWarning!=null){body.Add(ShoppingUi.Text(loadWarning,13));body.Add(ShoppingUi.Button("Retry account details",RefreshAccountAsync,true));}
        var profileDetails=new VerticalStackLayout{Spacing=10};
        if(ProductFeatures.SocialEnabled)
        {
            profileDetails.Add(ShoppingUi.Text("Your community username",22,true));
            var username=ShoppingUi.Entry(community?.Username??"","Choose your public name",_=>{},40);username.IsEnabled=!loading;profileDetails.Add(username);
            profileDetails.Add(ShoppingUi.Text("Only your chosen name appears on posts. A hidden identifier keeps accounts distinct when names match.",13));
            profileDetails.Add(ShoppingUi.Button("Save username",async()=>{
                if(loading)throw new InvalidOperationException("Wait for your account details to finish loading.");AccountSession.Check(account);await FoodFeedService.Call<object>(new{action="setUsername",username=username.Text??""});
                var saved=await FoodFeedService.Profile();AccountSession.Check(account);community=saved;Render();
            }));
        }
        else
        {
            profileDetails.Add(ShoppingUi.Text(AppLocalization.Text("Email confirmation"),22,true));
        }
        profileDetails.Add(ShoppingUi.Text(emailVerified==true?"✓ Email confirmed":loading?"Checking email confirmation…":emailVerified==false?"Your email address has not been confirmed yet.":"Email confirmation status is unavailable. Retry when connected.",14,true));
        if(!loading&&emailVerified==false)profileDetails.Add(ShoppingUi.Button(AppLocalization.Text("Send confirmation email"),async()=>{
            AccountSession.Check(account);var sent=await AccountSession.SendVerification();AccountSession.Check(account);
            if(!sent){emailVerified=true;Render();await DisplayAlert("Email confirmed","Your email address is already confirmed. No further confirmation is needed.",AppLocalization.Text("OK"));return;}
            await DisplayAlert("Check your inbox","Open the confirmation link, then return and tap Check email confirmation.",AppLocalization.Text("OK"));
        },true));
        if(!loading&&emailVerified!=true)profileDetails.Add(ShoppingUi.Button("Check email confirmation",RefreshAccountAsync,true));
        profileDetails.Add(status);profileDetails.Add(ShoppingUi.Button(AppLocalization.Text("Sync my account"),async()=>{AccountSession.Check(account);status.Text="Syncing your recipes and checklists…";status.Text=await AccountSession.SyncSafely();},true));
        profileDetails.Add(ShoppingUi.Button(AppLocalization.Text("Edit my profile"),()=>AppNavigation.Open(new EditdetailsPage())));
        profileDetails.Add(ShoppingUi.Button("Copy device recipes into this account…",async()=>{
            var uid=account;AccountSession.Check(uid);if(!await DisplayAlert("Copy your device recipes?","Copy recipes created in device mode into this private account? The device originals are kept. Existing imports won't be duplicated.","Copy recipes",AppLocalization.Text("Cancel")))return;AccountSession.Check(uid);var count=await RecipeService.CopyDeviceRecipes(uid);AccountSession.Check(uid);_=AccountSession.SyncSafely();await DisplayAlert("Recipes copied",$"{count} recipes copied. Device originals are unchanged.",AppLocalization.Text("OK"));
        },true));
        var householdTools=new VerticalStackLayout{Spacing=8,Children={
            ShoppingUi.Button("Manage household",()=>AppNavigation.Open(new HouseholdPage())),
            ShoppingUi.Button("Shared meal plan",()=>AppNavigation.Open(new HouseholdMealsPage()),true),
            ShoppingUi.Button("Shopping questions & replies",()=>AppNavigation.Open(new HouseholdShoppingInboxPage()),true)
        }};
        var premiumTools=new VerticalStackLayout{Spacing=8,Children={
            ShoppingUi.Text("Household sharing, recipe-link imports, saved weeks and reusable shopping trips.",14),
            ShoppingUi.Button("Plans & benefits",()=>AppNavigation.Open(new PlanPage())),
            ShoppingUi.Button("Saved weekly templates",PremiumAccess.OpenWeeks,true),
            ShoppingUi.Button("Saved shopping trips",PremiumAccess.OpenShopping,true)
        }};
        View Section(string key,string title,string detail,View content,bool initial=false)=>new DisclosureSection(title,detail,content,expanded.Contains(key)||initial,open=>{if(open)expanded.Add(key);else expanded.Remove(key);});
        body.Add(Section("profile","Profile, sign-in & sync",emailVerified==true?"Email confirmed":"Profile details and email confirmation",profileDetails,initial:emailVerified==false));
        body.Add(Section("household","My household","Members, shared meals and shopping conversations",householdTools));
        body.Add(Section("premium","Premium & saved tools","Membership options and reusable weeks/trips",premiumTools));
        var privacy=new VerticalStackLayout{Spacing=8,Children={
            ShoppingUi.Text("Recipes and your main personal shopping list sync when connected. Extra lists, personal meal plans, saved weeks, shopping trips and drafts stay on this device. Household lists and meals are shared online.",13),
            ShoppingUi.Button("Privacy, data & account deletion",()=>AppNavigation.Open(new PrivacyAndDataPage()),true)
        }};
        body.Add(Section("privacy","Privacy & account deletion","Data controls and deletion requests",privacy));
        if(administrator)body.Add(Section("admin","Administrator","Support accounts, access and patch notes",new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Button("Open administrator tools",()=>AppNavigation.Open(new AdminPage()))}}));
        if(ProductFeatures.SocialEnabled)body.Add(Section("community","Community",null,new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Button("Friends & public profile",()=>AppNavigation.Open(new FriendsPage()),true),ShoppingUi.Button("Business & creators",()=>AppNavigation.Open(new BusinessHubPage()),true)}}));
        body.Add(ShoppingUi.Button(AppLocalization.Text("Sign out"),async()=>{if(!await DisplayAlert("Sign out?","Your private local data is kept for this account. The app will return to device mode.",AppLocalization.Text("Sign out"),"Stay"))return;AccountSession.Check(account);await AccountSession.SignOut();requests.Invalidate();loading=false;loadWarning=null;account=null;community=null;administrator=false;Render();},true));
        Content=new ScrollView{Content=body};
    }
}
