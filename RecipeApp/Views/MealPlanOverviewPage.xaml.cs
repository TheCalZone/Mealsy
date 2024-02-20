using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MealPlanOverviewPage : ContentPage
    {
        public MealPlanOverviewPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (((MealPlanOverviewViewModel)BindingContext).Recipes.Count == 0)
            {
                // Get all recipes and set the view model's Recipes property
                var recipes = RecipeService.Recipes;
                ((MealPlanOverviewViewModel)BindingContext).Recipes.ReplaceRange(recipes);
            }
                await RecipeService.CreateMealPlan();
                ((MealPlanOverviewViewModel)BindingContext).MealPlan = await RecipeService.GetMealPlan();
        }

    }
}