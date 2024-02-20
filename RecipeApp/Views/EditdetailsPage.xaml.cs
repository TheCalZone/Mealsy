using Plugin.Firebase.Auth;
using RecipeApp.Helpers;
using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditdetailsPage : ContentPage
    {

        private EditdetailsViewModel ViewModel => BindingContext as EditdetailsViewModel;
        public EditdetailsPage()
        {
            InitializeComponent();
            BindingContext = new EditdetailsViewModel();
        }

        private async void SaveChanges_Clicked(object sender, EventArgs e)
        {
            var email = emailEntry.Text;
            var firstname = firstnameEntry.Text;
            var lastname = lastnameEntry.Text;
            var dietRestrictions = dietPicker.Text;
            var favouritecuisine = favouriteCuisine.Text;

            var userdata = new Dictionary<object, object>
                {
                    { "email", email },
                    {"firstname", firstname },
                    {"lastname", lastname },
                    {"dietRestrict", dietRestrictions },
                    {"favouriteCuisine", favouritecuisine },
                };



            ////Add user to firestore with info
            await FirestoreService.SetUserData(CrossFirebaseAuth.Current.CurrentUser.Uid, userdata);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.InitializeAsync();
        }

    }
}