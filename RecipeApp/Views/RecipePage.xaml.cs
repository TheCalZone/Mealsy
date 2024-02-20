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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ((RecipeViewModel)BindingContext).Recipes.ReplaceRange(RecipeService.Recipes);
            ((RecipeViewModel)BindingContext).OriginalRecipes.ReplaceRange(RecipeService.Recipes);
            if (((RecipeViewModel)BindingContext).Recipes.Count == 0)
            {
                ((RecipeViewModel)BindingContext).Recipes.Clear();


                if (CrossFirebaseAuth.Current.CurrentUser != null)
                {
                    var recipes = RecipeService.Recipes;

                    ((RecipeViewModel)BindingContext).Recipes.ReplaceRange(recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
                    ((RecipeViewModel)BindingContext).OriginalRecipes.ReplaceRange(recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
                }
                else
                {
                    var recipes = RecipeService.Recipes;

                    ((RecipeViewModel)BindingContext).Recipes.ReplaceRange(recipes);
                    ((RecipeViewModel)BindingContext).OriginalRecipes.ReplaceRange(recipes);
                }

            }


            LoadFilters();
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