using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;
using MvvmHelpers;
using Newtonsoft.Json;
using System.Text.Json;

namespace RecipeApp.ViewModels
{
    internal class MealPlannerViewModel : CustomViewModelBase
    {
        public AsyncCommand SaveMealPlanCommand { get; }

        public MvvmHelpers.Commands.Command AddBreakFastNoteCommand { get; set; }
        public MvvmHelpers.Commands.Command AddLunchNoteCommand { get; set; }
        public MvvmHelpers.Commands.Command AddDinnerNoteCommand { get; set; }
        public MvvmHelpers.Commands.Command AddBreakFastRecipeCommand { get; set; }
        public MvvmHelpers.Commands.Command AddLunchRecipeCommand{ get; set; }
        public MvvmHelpers.Commands.Command AddDinnerRecipeCommand { get; set; }


        public ObservableRangeCollection<Recipe> Recipes { get; set; }
        private MealPlanDay selectedDay;
        private MealPlan mealPlan;
        private Recipe selectedBreakfast;
        public Recipe SelectedBreakfast { get => selectedBreakfast; set => SetProperty(ref selectedBreakfast, value); }
        private Recipe selectedLunch;
        public Recipe SelectedLunch { get => selectedLunch; set => SetProperty(ref selectedLunch, value); }
        private Recipe selectedDinner;
        public Recipe SelectedDinner { get => selectedDinner; set => SetProperty(ref selectedDinner, value); }


        public MealPlan MealPlan { get => mealPlan; set => SetProperty(ref mealPlan, value); }

        public MealPlanDay SelectedDay
        {
            get => selectedDay;
            set => SetProperty(ref selectedDay, value);
        }

        public MealPlannerViewModel()
        {
            Recipes = new ObservableRangeCollection<Recipe>();
            SaveMealPlanCommand = new AsyncCommand(SaveMealDay);
            AddBreakFastNoteCommand = new MvvmHelpers.Commands.Command(AddBreakfastNote);
            AddLunchNoteCommand = new MvvmHelpers.Commands.Command(AddLunchNote);
            AddDinnerNoteCommand = new MvvmHelpers.Commands.Command(AddDinnerNote);
            AddBreakFastRecipeCommand = new MvvmHelpers.Commands.Command(AddBreakfastRecipe);
            AddLunchRecipeCommand = new MvvmHelpers.Commands.Command(AddLunchRecipe);
            AddDinnerRecipeCommand = new MvvmHelpers.Commands.Command(AddDinnerRecipe);
        }

        private async void AddBreakfastNote()
        {
            string note = await Shell.Current.DisplayPromptAsync("Add Note", "Enter a note for breakfast");
            if (!string.IsNullOrWhiteSpace(note))
            {
                SelectedDay.BreakfastNotes.Add(note);
            }
        }

        private async void AddLunchNote()
        {
            string note = await Shell.Current.DisplayPromptAsync("Add Note", "Enter a note for lunch");
            if (!string.IsNullOrWhiteSpace(note))
                SelectedDay.LunchNotes.Add(note);
        }
        private async void AddDinnerNote()
        {
            string note = await Shell.Current.DisplayPromptAsync("Add Note", "Enter a note for dinner");
            if (!string.IsNullOrWhiteSpace(note))
                SelectedDay.DinnerNotes.Add(note);
        }
        private async void AddBreakfastRecipe()
        {
            string recipename = await Shell.Current.DisplayActionSheet("Select a Recipe", "Cancel", null, Recipes.Select(r => r.RecipeName).ToArray());
            if (!string.IsNullOrWhiteSpace(recipename))
            {
                Recipe recipe = Recipes.Where(r => r.RecipeName == recipename).FirstOrDefault();
                if (recipe != null)
                {
                    selectedDay.BreakfastRecipesIDs.Add(recipe.Id);
                    SelectedDay.BreakfastRecipes.Add(recipe);
                }
            }
        }
        private async void AddLunchRecipe()
        {
            string recipename = await Shell.Current.DisplayActionSheet("Select a Recipe", "Cancel", null, Recipes.Select(r => r.RecipeName).ToArray());
            if (!string.IsNullOrWhiteSpace(recipename))
            {
                Recipe recipe = Recipes.Where(r => r.RecipeName == recipename).FirstOrDefault();
                if (recipe != null)
                {
                    selectedDay.LunchRecipesIDs.Add(recipe.Id);
                    SelectedDay.LunchRecipes.Add(recipe);
                }
            }
        }
        private async void AddDinnerRecipe()
        {
            string recipename = await Shell.Current.DisplayActionSheet("Select a Recipe", "Cancel", null, Recipes.Select(r => r.RecipeName).ToArray());
            if (!string.IsNullOrWhiteSpace(recipename))
            {
                Recipe recipe = Recipes.Where(r => r.RecipeName == recipename).FirstOrDefault();
                if (recipe != null)
                {
                    selectedDay.DinnerRecipesIDs.Add(recipe.Id);
                    SelectedDay.DinnerRecipes.Add(recipe);
                }
            }
        }


        private async Task SaveMealDay()
        {
            if (SelectedBreakfast != null)
            {
                SelectedDay.BreakfastRecipeId = SelectedBreakfast.Id;
                SelectedDay.Breakfast = SelectedBreakfast.RecipeName;
            }
            else
            {
                SelectedDay.BreakfastRecipeId = 0;
                selectedDay.Breakfast = String.Empty;
            }
            SelectedDay.BreakfastIDJson = System.Text.Json.JsonSerializer.Serialize(SelectedDay.BreakfastRecipesIDs);
            SelectedDay.BreakfastNotesJson = System.Text.Json.JsonSerializer.Serialize(SelectedDay.BreakfastNotes);


            if (SelectedLunch != null)
            {
                SelectedDay.LunchRecipeId = SelectedLunch.Id;
                SelectedDay.Lunch = SelectedLunch.RecipeName;
            }
            else
            {
                SelectedDay.LunchRecipeId = 0;
                SelectedDay.Lunch = String.Empty;
            }
            SelectedDay.LunchIDJson = System.Text.Json.JsonSerializer.Serialize(SelectedDay.LunchRecipesIDs);
            SelectedDay.LunchNotesJson = System.Text.Json.JsonSerializer.Serialize(SelectedDay.LunchNotes);


            if (SelectedDinner != null)
            {
                SelectedDay.DinnerRecipeId = SelectedDinner.Id;
                SelectedDay.Dinner = SelectedDinner.RecipeName;
            }
            else
            {
                SelectedDay.DinnerRecipeId = 0;
                SelectedDay.Dinner = String.Empty;
            }
            SelectedDay.DinnerIDJson = System.Text.Json.JsonSerializer.Serialize(SelectedDay.DinnerRecipesIDs);
            SelectedDay.DinnerNotesJson = System.Text.Json.JsonSerializer.Serialize(SelectedDay.DinnerNotes);

            await RecipeService.SaveMealPlanDay(SelectedDay, MealPlan.Id, SelectedDay.MealDayIndex);
            await Shell.Current.Navigation.PopAsync();
        }

    }
}
