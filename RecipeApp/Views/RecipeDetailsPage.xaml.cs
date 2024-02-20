using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [QueryProperty(nameof(RecipeId), nameof(RecipeId))]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RecipeDetailsPage : ContentPage
    {
        public string RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public RecipeDetailsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            long.TryParse(RecipeId, out var result);

            var recipe = await RecipeService.GetRecipe(result);
            Recipe = recipe;
            BindingContext = new RecipeDetailsViewModel(recipe);
        }




    }
}