
using RecipeApp.Helpers;
using RecipeApp.ViewModels;
using Firebase.Auth;
using Plugin.Firebase.Auth;
using RecipeApp.Services;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccountPage : ContentPage
    {
        public IFirebaseUser User { get; set; }

        public AccountPage()
        {

            InitializeComponent();
            User = CrossFirebaseAuth.Current.CurrentUser;
            ((AccountPageViewModel)BindingContext).User = User;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CheckIfUserSignedIn();
        }

        private void CheckIfUserSignedIn()
        {
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                ((AccountPageViewModel)BindingContext).IsAuthenticated = true;
                emailEntry.Text = CrossFirebaseAuth.Current.CurrentUser.Email;
            }
            else
            {
                ((AccountPageViewModel)BindingContext).IsAuthenticated = false;
            }
        }

        private void EmailEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSignInButtonState();
        }

        private void PasswordEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSignInButtonState();
        }

        private void UpdateSignInButtonState()
        {
            signInButton.IsEnabled = !string.IsNullOrEmpty(emailEntry.Text) && !string.IsNullOrEmpty(passwordEntry.Text);
        }

        private async void Plan_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new PlanPage());

        private async void Household_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new HouseholdPage());
    }
}
