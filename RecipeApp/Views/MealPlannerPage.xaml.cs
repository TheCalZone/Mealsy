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
            var recipes = RecipeService.Recipes;
            ((MealPlannerViewModel)BindingContext).Recipes.ReplaceRange(recipes);
            var mealPlan = await RecipeService.GetMealPlan();
            ((MealPlannerViewModel)BindingContext).MealPlan = mealPlan;
            ((MealPlannerViewModel)BindingContext).SelectedDay = mealPlan.Days[SelectedDayIndex];
        }
    }
}