using RecipeApp.ViewModels;
using RecipeApp.Services;
using RecipeApp.Models;
using Plugin.Firebase.Auth;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RecipePage : ContentPage
    {
        public RecipePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await RecipeService.GetRecipesForLocal();
                var userId = CrossFirebaseAuth.Current.CurrentUser?.Uid;
                var recipes = RecipeService.Recipes.Where(recipe => userId == null || recipe.FirebaseUID == userId);
                var viewModel = (RecipeViewModel)BindingContext;
                viewModel.Recipes.ReplaceRange(recipes);
                viewModel.OriginalRecipes.ReplaceRange(recipes);
                LoadFilters();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Unable to load recipes", ex.Message, "OK");
            }
        }

        private void LoadFilters()
        {
            ((RecipeViewModel)BindingContext).Filters.Clear();

            // Get all unique tags in the recipes list
            var recipes = ((RecipeViewModel)BindingContext).Recipes;

            var tags = recipes
                .Where(r => r.Tags != null)
                .SelectMany(r => r.Tags.Split(',').Select(tag => tag.Trim().ToLower()))
                .Distinct()
                .ToList();

            // Update the filter list with the unique tags
            ((RecipeViewModel)BindingContext).Filters.ReplaceRange(tags);
            if (!((RecipeViewModel)BindingContext).Filters.Contains("all"))
            {
                ((RecipeViewModel)BindingContext).Filters.Add("all");
            }
        }


        private async void Button_Pressed(object sender, EventArgs e)
        {
            foreach (var recipe in ((RecipeViewModel)BindingContext).Recipes)
            {
                await RecipeService.RemoveRecipe(recipe.Id);
                await FirestoreService.DeleteRecipe(recipe.Id);
            }
        }
    }
}