using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [QueryProperty(nameof(SnackName), "SnackName")]
    [QueryProperty(nameof(StapleName), "StapleName")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditExtraPage : ContentPage
    {
        public string SnackName { get; set; }

        public string StapleName { get; set; }

        public Snack Snack { get; }
        public Staple Staple { get; }

        public  EditExtraPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (SnackName == null)
            {
                var staple = await RecipeService.GetStapleByName(StapleName);
                BindingContext = new EditExtraViewModel(staple);
            }
            else
            {
                var snack = await RecipeService.GetSnackByName(SnackName);
                BindingContext = new EditExtraViewModel(snack);
            }

        }
    }
}