using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class LoginPage : ContentPage
{
    public LoginPage(){InitializeComponent();Title=AppLocalization.Text("Sign in");ShoppingUi.Theme(this);Content=new AuthFormView(false);}
    protected override void OnAppearing(){base.OnAppearing();if(RecipeApp.Services.AccountSession.UserId!=null)Content=ShoppingUi.Button("Already signed in · Open account",()=>RecipeApp.Services.AppNavigation.Root("AccountPage"));}
}
