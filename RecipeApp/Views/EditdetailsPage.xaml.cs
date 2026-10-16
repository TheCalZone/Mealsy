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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.InitializeAsync();
        }

    }
}
