using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [QueryProperty(nameof(SelectedDayIndex), nameof(SelectedDayIndex))]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MealPlannerPage : ContentPage
    {
        public int SelectedDayIndex { get; set; }

        public MealPlannerPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Get all recipes and set the view model's Recipes property
            var recipes = await RecipeService.GetAllRecipes();
            ((MealPlannerViewModel)BindingContext).Recipes.ReplaceRange(recipes);
            var mealPlan = await RecipeService.GetMealPlan();
            ((MealPlannerViewModel)BindingContext).MealPlan = mealPlan;
            var viewModel = (MealPlannerViewModel)BindingContext;
            var day = mealPlan?.Days.FirstOrDefault(item => item.MealDayIndex == SelectedDayIndex);
            if (day == null)
                return;
            viewModel.SelectedDay = day;
            viewModel.SelectedBreakfast = recipes.FirstOrDefault(recipe => recipe.Id == day.BreakfastRecipeId);
            viewModel.SelectedLunch = recipes.FirstOrDefault(recipe => recipe.Id == day.LunchRecipeId);
            viewModel.SelectedDinner = recipes.FirstOrDefault(recipe => recipe.Id == day.DinnerRecipeId);
        }
    }
}