using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class AccountPage : ContentPage
{
    private string account;
    private RecipeApp.Models.CommunityProfile community;
    private bool administrator;
    public AccountPage(){InitializeComponent();Title="Your account";ShoppingUi.Theme(this);ToolbarItems.Add(new ToolbarItem("Get started",null,async()=>await AppNavigation.Open(new OnboardingPage())));ToolbarItems.Add(new ToolbarItem("Share Mealsy",null,async()=>await OnboardingState.ShareApp()){Order=ToolbarItemOrder.Secondary});ToolbarItems.Add(new ToolbarItem("Settings",null,async()=>await AppNavigation.Open(new SettingsPage())));}
    private readonly LoadGeneration requests=new();
    private bool visible,loading;
    private string loadWarning;
    protected override async void OnAppearing()
    {
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
        if(account!=uid)community=null;
        account=uid;administrator=false;loadWarning=null;loading=uid!=null;Render();
        if(uid==null)return;
        var unavailable=false;
        async Task Profile(){try{await AccountSession.LoadProfile();}catch(Exception){unavailable=true;}}
        async Task Community(){try{var saved=await FoodFeedService.Profile();if(visible&&requests.IsCurrent(ticket,AccountSession.UserId))community=saved;}catch(Exception){unavailable=true;}}
        async Task Role(){try{var saved=await AdminService.Status();if(visible&&requests.IsCurrent(ticket,AccountSession.UserId))administrator=saved.Admin;}catch(Exception){unavailable=true;}}
        try{await Task.WhenAll(Profile(),ProductFeatures.SocialEnabled?Community():Task.CompletedTask,Role());}
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
        body.Add(ShoppingUi.Text("Your place at the table",28,true));body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text(string.IsNullOrWhiteSpace(profile.FirstName)?"Welcome to Mealsy":profile.FirstName+" "+profile.LastName,23,true),ShoppingUi.Text(AccountSession.Email??"",14),ShoppingUi.Text("Private account · Each family member uses their own login",13)}}));
        if(ProductFeatures.SocialEnabled&&ContentSharing.HasPending)body.Add(ShoppingUi.Button("Open saved shared post or plan",ContentSharing.Resume));
        if(loading)body.Add(new LoadingIndicator("Updating profile and account permissions…"));
        if(loadWarning!=null){body.Add(ShoppingUi.Text(loadWarning,13));body.Add(ShoppingUi.Button("Retry account details",RefreshAccountAsync,true));}
        if(ProductFeatures.SocialEnabled)
        {
            body.Add(ShoppingUi.Text("Your community username",22,true));
            var username=ShoppingUi.Entry(community?.Username??"","Choose your public name",_=>{},40);username.IsEnabled=!loading;body.Add(username);
            body.Add(ShoppingUi.Text("Only your chosen name appears on posts. A hidden identifier keeps accounts distinct when names match.",13));
            body.Add(ShoppingUi.Button("Save username",async()=>{
                if(loading)throw new InvalidOperationException("Wait for your account details to finish loading.");AccountSession.Check(account);await FoodFeedService.Call<object>(new{action="setUsername",username=username.Text??""});
                var saved=await FoodFeedService.Profile();AccountSession.Check(account);community=saved;Render();
            }));
            body.Add(ShoppingUi.Text(loading?"Checking email confirmation…":community?.EmailVerified==true?"✓ Email confirmed · You can share recipes":"Confirm your email before sharing recipes",14,true));
            if(!loading&&community?.EmailVerified!=true)body.Add(ShoppingUi.Button("Send confirmation email",async()=>{AccountSession.Check(account);await AccountSession.SendVerification();await DisplayAlert("Check your inbox","Open the confirmation link, then return and tap Check email confirmation.","OK");},true));
            body.Add(ShoppingUi.Button("Check email confirmation",async()=>{var saved=await FoodFeedService.Profile();AccountSession.Check(account);community=saved;Render();},true));
        }
        else
        {
            body.Add(ShoppingUi.Text("Email confirmation",22,true));
            body.Add(ShoppingUi.Text("Keep your email address confirmed for household invitations and account access.",13));
            body.Add(ShoppingUi.Button("Send confirmation email",async()=>{AccountSession.Check(account);await AccountSession.SendVerification();await DisplayAlert("Check your inbox","If confirmation is needed, open the link sent to your email address.","OK");},true));
        }
        body.Add(status);body.Add(ShoppingUi.Button("Sync my account",async()=>{AccountSession.Check(account);status.Text="Syncing your recipes and checklists…";status.Text=await AccountSession.SyncSafely();},true));
        body.Add(ShoppingUi.Button("Edit my profile",()=>AppNavigation.Open(new EditdetailsPage())));
        body.Add(ShoppingUi.Button("Copy device recipes into this account…",async()=>{
            var uid=account;AccountSession.Check(uid);if(!await DisplayAlert("Copy your device recipes?","Copy recipes created in device mode into this private account? The device originals are kept. Existing imports won't be duplicated.","Copy recipes","Cancel"))return;AccountSession.Check(uid);var count=await RecipeService.CopyDeviceRecipes(uid);AccountSession.Check(uid);_=AccountSession.SyncSafely();await DisplayAlert("Recipes copied",$"{count} recipes copied. Device originals are unchanged.","OK");
        },true));
        if(ProductFeatures.SocialEnabled)body.Add(ShoppingUi.Button("Friends & public profile",()=>AppNavigation.Open(new FriendsPage()),true));
        body.Add(ShoppingUi.Button("Household meal plan",()=>AppNavigation.Open(new HouseholdMealsPage()),true));
        body.Add(ShoppingUi.Button("My recipes",()=>AppNavigation.Root("RecipePage"),true));
        body.Add(ShoppingUi.Button("My household",()=>AppNavigation.Open(new HouseholdPage()),true));
        if(ProductFeatures.SocialEnabled)body.Add(FoodFeedPage.Button("Mealsy Business & creators",()=>AppNavigation.Open(new BusinessHubPage())));
        if(administrator){body.Add(FoodFeedPage.Button("Administrator",()=>AppNavigation.Open(new AdminPage())));body.Add(FoodFeedPage.Button("Give complimentary Premium",()=>AppNavigation.Open(new AdminPage(true))));}
        body.Add(ShoppingUi.Button("Weekly templates · Premium",PremiumAccess.OpenWeeks,true));
        body.Add(ShoppingUi.Button("Saved shopping trips · Premium",PremiumAccess.OpenShopping,true));
        body.Add(ShoppingUi.Button("Mealsy Premium · Plans & benefits",()=>AppNavigation.Open(new PlanPage())));
        body.Add(ShoppingUi.Button("Premium features",()=>AppNavigation.Open(new UpgradeFeaturesPage()),true));
        body.Add(ShoppingUi.Text("Your recipes and shopping lists sync when connected. Your personal meal plan, saved weeks, saved shopping trips and editor drafts stay on this device. Household shopping and household meal plans are shared online. Signing in does not automatically copy recipes created in device mode.",13));
        body.Add(ShoppingUi.Button("Sign out",async()=>{if(!await DisplayAlert("Sign out?","Your private local data is kept for this account. The app will return to device mode.","Sign out","Stay"))return;AccountSession.Check(account);await AccountSession.SignOut();requests.Invalidate();loading=false;loadWarning=null;account=null;community=null;administrator=false;Render();},true));
        Content=new ScrollView{Content=body};
    }
}
