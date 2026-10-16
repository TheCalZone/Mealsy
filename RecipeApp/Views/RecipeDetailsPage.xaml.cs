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

        private async void ShareToFeed_Clicked(object sender, EventArgs e)
        {
            if (Recipe != null) await Navigation.PushAsync(new FeedComposerPage(Recipe));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!long.TryParse(RecipeId, out var result)) return;

            var recipe = await RecipeService.GetRecipe(result);
            if (recipe == null)
            {
                await DisplayAlert("Recipe unavailable", "This recipe no longer exists.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            Recipe = recipe;
            BindingContext = new RecipeDetailsViewModel(recipe);
        }




    }
}