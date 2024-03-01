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
            List<Ingredient> ListOfIngredients = new List<Ingredient>();
            List<Recipe> dailyRecipes = new List<Recipe>();

            foreach (MealPlanDay mealPlanDay in MealPlan.Days)
            {
                if (mealPlanDay.BreakfastRecipeId != 0 && !dailyRecipes.Any(r => r.Id == mealPlanDay.BreakfastRecipeId))
                {
                    dailyRecipes.Add(await RecipeService.GetRecipe(mealPlanDay.BreakfastRecipeId));
                }

                if (mealPlanDay.LunchRecipeId != 0 && !dailyRecipes.Any(r => r.Id == mealPlanDay.LunchRecipeId))
                {
                    dailyRecipes.Add(await RecipeService.GetRecipe(mealPlanDay.LunchRecipeId));
                }

                if (mealPlanDay.DinnerRecipeId != 0 && !dailyRecipes.Any(r => r.Id == mealPlanDay.DinnerRecipeId))
                {
                    dailyRecipes.Add(await RecipeService.GetRecipe(mealPlanDay.DinnerRecipeId));
                }
            }

            foreach (Recipe recipe in dailyRecipes)
            {
                if (recipe.Serving > 0)
                {
                    int timesRecipeAppears = MealPlan.Days.Count(day => day.BreakfastRecipeId == recipe.Id || day.LunchRecipeId == recipe.Id || day.DinnerRecipeId == recipe.Id);
                    int timesToAdd = (int)Math.Ceiling((double)timesRecipeAppears / recipe.Serving);

                    foreach (Ingredient ingredient in recipe.Ingredients)
                    {
                        if (ingredient.MainIngredient)
                        {
                            for (int i = 0; i < timesToAdd; i++)
                            {
                                if (ingredientCounts.ContainsKey(ingredient))
                                {
                                    ingredientCounts[ingredient]++;
                                }
                                else
                                {
                                    ingredientCounts.Add(ingredient, 1);
                                }
                            }

                        }
                    }
                }
                else
                {
                    foreach (Ingredient ingredient in recipe.Ingredients)
                    {
                        if (ingredient.MainIngredient)
                        {
                            if (ingredientCounts.ContainsKey(ingredient))
                            {
                                ingredientCounts[ingredient]++;
                            }
                            else
                            {
                                ingredientCounts.Add(ingredient, 1);
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<Ingredient, int> entry in ingredientCounts)
            {
                ListOfIngredients.Add(new Ingredient { Name = entry.Key.Name + " x" + entry.Value, Tag = entry.Key.Tag});
            }


            var ingredientsJson = JsonConvert.SerializeObject(ListOfIngredients);

            var route = $"//MealPlanOverviewPage/ShoppingListPage?ingredientsJson={HttpUtility.UrlEncode(ingredientsJson)}";
            await Shell.Current.GoToAsync(route);
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
