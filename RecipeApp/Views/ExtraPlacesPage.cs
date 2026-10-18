using RecipeApp.Services;
namespace RecipeApp.Views;
public class ExtraPlacesPage:ContentPage
{
    private readonly Picker places=new(){Title="Extra places",ItemsSource=Enumerable.Range(1,16).Select(SubscriptionPricing.ExtraPlaces).ToList(),SelectedIndex=0};
    private readonly Label status=ShoppingUi.Text("");private bool busy;
    public ExtraPlacesPage(){
        if(!ClientPlatform.StorePurchasesAvailable){Title="Extra household places";ShoppingUi.Theme(this);Content=new ScrollView{Content=new VerticalStackLayout{Padding=20,Spacing=14,Children={ShoppingUi.Text("Household capacity",25,true),ShoppingUi.Text("App Store purchases for extra places are not available in this build. Existing verified household capacity still applies. Ask your household owner to manage their plan on its original platform.")}}};return;}
        Title="Extra household places";ShoppingUi.Theme(this);Content=new ScrollView{Content=new VerticalStackLayout{Padding=20,Spacing=14,Children={ShoppingUi.Text("Make room for more",25,true),ShoppingUi.Text("Family includes four people, including the owner. Extra places are a separate monthly subscription at £0.99 per person at the planned UK price. The app store confirms your local total before payment. Places are shared by the owner's household."),places,ShoppingUi.Button("Continue in app store",Buy),status,ShoppingUi.Button("Manage existing places",()=>Launcher.Default.OpenAsync("https://play.google.com/store/account/subscriptions?sku=mealsy_extra_places&package=com.companyname.recipeapp")),ShoppingUi.Text("Extra places renew monthly until cancelled. Cancellation keeps places until the paid period ends. Existing members keep their data if the allowance drops; new members cannot join while the household is full.",13)}}};}
    private async Task Buy(){if(busy)return;busy=true;try{var plan=await PlanService.RefreshAsync();if(!plan.Owner||!plan.FamilyEnabled)throw new InvalidOperationException("The household owner needs active Family access before buying extra places.");if(plan.ExtraSeats>0)throw new InvalidOperationException("You already have extra places. Manage your existing subscription in the app store.");
#if ANDROID
        await RecipeApp.Platforms.Android.PlayBillingService.Instance.Buy("places-"+(places.SelectedIndex+1),"mealsy_extra_places");status.Text="Extra places verified. Reopen your household to invite someone.";
#else
        status.Text="Extra-place purchases aren't configured for this platform yet.";
#endif
    }catch(Exception e){status.Text=e is InvalidOperationException or HouseholdException?e.Message:"The purchase couldn't be checked. Restore purchases before trying again.";}finally{busy=false;}}
}
