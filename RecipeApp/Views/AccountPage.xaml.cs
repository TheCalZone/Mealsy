using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class AccountPage : ContentPage
{
    private string account;
    public AccountPage(){InitializeComponent();Title="Your account";ShoppingUi.Theme(this);}
    protected override async void OnAppearing()
    {
        base.OnAppearing();account=AccountSession.UserId;Render();if(account==null)return;
        await AccountSession.LoadProfile();if(account==AccountSession.UserId)Render();
    }
    private void Render()
    {
        if(AccountSession.UserId==null){Content=new AuthFormView(false);return;}
        var profile=AccountSession.CachedProfile;var status=ShoppingUi.Text(AccountSession.Status,13);var body=new VerticalStackLayout{Padding=20,Spacing=16};
        body.Add(ShoppingUi.Text("Your place at the table",28,true));body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text(string.IsNullOrWhiteSpace(profile.FirstName)?"Welcome to Mealsy":profile.FirstName+" "+profile.LastName,23,true),ShoppingUi.Text(AccountSession.Email??"",14),ShoppingUi.Text("Private account · Each family member uses their own login",13)}}));
        body.Add(status);body.Add(ShoppingUi.Button("Sync my account",async()=>{AccountSession.Check(account);status.Text="Syncing your recipes and checklists…";status.Text=await AccountSession.SyncSafely();},true));
        body.Add(ShoppingUi.Button("Edit my profile",()=>AppNavigation.Open(new EditdetailsPage())));
        body.Add(ShoppingUi.Button("Copy device recipes into this account…",async()=>{
            var uid=account;AccountSession.Check(uid);if(!await DisplayAlert("Copy your device recipes?","Copy recipes created in device mode into this private account? The device originals are kept. Existing imports won't be duplicated.","Copy recipes","Cancel"))return;AccountSession.Check(uid);var count=await RecipeService.CopyDeviceRecipes(uid);AccountSession.Check(uid);_=AccountSession.SyncSafely();await DisplayAlert("Recipes copied",$"{count} recipes copied. Device originals are unchanged.","OK");
        },true));
        body.Add(ShoppingUi.Button("My recipes",()=>AppNavigation.Root("RecipePage"),true));
        body.Add(ShoppingUi.Button("My household",()=>AppNavigation.Open(new HouseholdPage()),true));
        body.Add(ShoppingUi.Button("Family · £3/month or £24/year",()=>AppNavigation.Open(new PlanPage())));
        body.Add(ShoppingUi.Button("See everything included",()=>AppNavigation.Open(new UpgradeFeaturesPage()),true));
        body.Add(ShoppingUi.Text("Your recipes and shopping checklists sync when Firebase is available. Meal plans and editor drafts are device-local. Signing in doesn't automatically upload recipes created in device mode.",13));
        body.Add(ShoppingUi.Button("Sign out",async()=>{if(!await DisplayAlert("Sign out?","Your private local data is kept for this account. The app will return to device mode.","Sign out","Stay"))return;AccountSession.Check(account);await AccountSession.SignOut();account=null;Render();},true));
        Content=new ScrollView{Content=body};
    }
}
