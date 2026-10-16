using MvvmHelpers;
using MvvmHelpers.Commands;
using Newtonsoft.Json;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;
using System.Web;

namespace RecipeApp.ViewModels
{

    internal class MealPlanOverviewViewModel : CustomViewModelBase
    {
        private MealPlanDay _selectedDay;
        public AsyncCommand<MealPlanDay> SelectMealPlanDayCommand { get; set; }

        public AsyncCommand DeleteMealPlanDayCommand { get; set; }
        public AsyncCommand SaveMealPlanCommand { get; set; }
        public AsyncCommand ShopMealPlanCommand { get; set; }
        public AsyncCommand ClearMealPlanCommand { get; set; }
        public MvvmHelpers.Commands.Command AddDayCommand { get; set; }


        Dictionary<Ingredient, int> ingredientCounts = new Dictionary<Ingredient, int>();
        public ObservableRangeCollection<Recipe> Recipes { get; set; }
        private MealPlan mealPlan;
        public MealPlan MealPlan { get => mealPlan; set => SetProperty(ref mealPlan, value); }

        public MealPlanOverviewViewModel()
        {
            DeleteMealPlanDayCommand = new AsyncCommand(DeleteMealDay);
            SaveMealPlanCommand = new AsyncCommand(SaveMealDayAsync);
            ShopMealPlanCommand = new AsyncCommand(ShopMealPlan);
            ClearMealPlanCommand = new AsyncCommand(ClearMealPlan);
            SelectMealPlanDayCommand = new AsyncCommand<MealPlanDay>(ShowMealPlanDay);
            Recipes = new ObservableRangeCollection<Recipe>();
            Recipes.AddRange(RecipeService.Recipes);
            AddDayCommand = new MvvmHelpers.Commands.Command(AddDay);
        }
        public async Task DeleteMealDay()
        {
            await RecipeService.DeleteMealPlan();
        }

        public MealPlanDay SelectedDay
        {
            get => _selectedDay;
            set => SetProperty(ref _selectedDay, value);
        }

        public void AddDay()
        {
            MealPlan.Days.Add(new MealPlanDay());
        }

        private async Task ShowMealPlanDay(MealPlanDay args)
        {
            IsBusy = true;

            var route = $"//MealPlanOverviewPage/MealPlannerPage?SelectedDayIndex={args.MealDayIndex}";
            await Shell.Current.GoToAsync(route);

            IsBusy = false;

            //await Shell.Current.GoToAsync(nameof(MealPlannerPage))
        }

        public async Task SaveMealDayAsync()
        {
            // Navigate back to the meal plan overview page
            await Shell.Current.Navigation.PopAsync();
        }

        public async Task ShopMealPlan()
        {
            try
            {
                var choices=new List<RecipeShoppingChoice>();
                foreach (var entry in ShoppingPlan.MealOccurrences(MealPlan.Days))
                {
                    var recipe=await RecipeService.GetRecipe(entry.Key);
                    if (recipe==null) throw new InvalidOperationException("A meal-plan recipe was deleted. Update the plan before shopping.");
                    choices.Add(new RecipeShoppingChoice(recipe) { Selected=true, Batches=entry.Value.ToString() });
                }
                await Shell.Current.Navigation.PushAsync(new ShoppingListMakerPage(choices));
            }
            catch (Exception e) { await Shell.Current.DisplayAlert("Meal plan",e is InvalidOperationException ? e.Message : "The shopping review couldn't be opened. Your saved list hasn't changed.","OK"); }
        }

        public async Task ClearMealPlan()
        {
            foreach (MealPlanDay day in MealPlan.Days)
            {
                day.BreakfastRecipeId = 0;
                day.Breakfast = String.Empty;
                day.BreakfastRecipes.Clear();
                day.BreakfastNotes.Clear();
                day.BreakfastRecipesIDs.Clear();
                day.BreakfastNotesJson = String.Empty;
                day.BreakfastIDJson = String.Empty;

                day.LunchRecipeId = 0;
                day.Lunch = String.Empty;
                day.LunchRecipes.Clear();
                day.LunchNotes.Clear();
                day.LunchRecipesIDs.Clear();
                day.LunchNotesJson = String.Empty;
                day.LunchIDJson = String.Empty;

                day.DinnerRecipeId = 0;
                day.Dinner = String.Empty;
                day.DinnerRecipes.Clear();
                day.DinnerNotes.Clear();
                day.DinnerRecipesIDs.Clear();
                day.DinnerNotesJson = String.Empty;
                day.DinnerIDJson = String.Empty;

                await RecipeService.SaveMealPlanDay(day, MealPlan.Id, day.MealDayIndex);

            }


        }
    }
}
