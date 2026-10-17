namespace RecipeApp.Views;
public partial class SignupPage : ContentPage
{
    public SignupPage(){InitializeComponent();Title="Create account";ShoppingUi.Theme(this);Content=new AuthFormView(true);}
    protected override void OnAppearing(){base.OnAppearing();if(RecipeApp.Services.AccountSession.UserId!=null)Content=ShoppingUi.Button("Already signed in · Open account",()=>RecipeApp.Services.AppNavigation.Root("AccountPage"));}
}
