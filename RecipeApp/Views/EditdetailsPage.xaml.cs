using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class EditdetailsPage : ContentPage
{
    private readonly string account=AccountSession.UserId;private bool loaded,saved;
    public EditdetailsPage(){InitializeComponent();Title="Your profile";ShoppingUi.Theme(this);}
    protected override void OnAppearing()
    {
        base.OnAppearing();if(account==null||account!=AccountSession.UserId){Content=ShoppingUi.Text("Sign in again before editing your profile.");return;}if(loaded)return;loaded=true;
        var profile=AccountSession.CachedProfile;var body=new VerticalStackLayout{Padding=20,Spacing=14};body.Add(ShoppingUi.Text("Make Mealsy feel like you",28,true));body.Add(ShoppingUi.Text(AccountSession.Email,14));
        Entry Field(string label,string value,int max){body.Add(ShoppingUi.Text(label,12,true));var input=ShoppingUi.Entry(value,label,_=>{},max);body.Add(input);return input;}
        var first=Field("First name",profile.FirstName,80);var last=Field("Last name",profile.LastName,80);var diet=Field("Dietary preferences",profile.DietRestrictions,500);var cuisines=Field("Favourite cuisines",profile.FavouriteCuisines,500);
        body.Add(ShoppingUi.Text("Your sign-in email is read-only. Preferences can be saved locally while cloud sync is unavailable.",13));
        body.Add(ShoppingUi.Button("Save profile",async()=>{if(saved)return;AccountSession.Check(account);AccountSession.SaveProfile(new AppUser{FirebaseUID=account,FirstName=first.Text,LastName=last.Text,DietRestrictions=diet.Text,FavouriteCuisines=cuisines.Text});saved=true;_=AccountSession.SyncSafely();await AppNavigation.Back();}));
        body.Add(ShoppingUi.Button(AppLocalization.Text("Cancel"),AppNavigation.Back,true));Content=new ScrollView{Content=body};
    }
}
