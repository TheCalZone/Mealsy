using MvvmHelpers;
using MvvmHelpers.Commands;
using Plugin.Firebase.Auth;
using RecipeApp.Helpers;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;

namespace RecipeApp.ViewModels
{
    public class RecipeViewModel : CustomViewModelBase
    {
        private ObservableRangeCollection<Recipe> recipes;
        // Properties
        public ObservableRangeCollection<Recipe> Recipes { get => recipes; set => SetProperty(ref recipes, value); }
        private ObservableRangeCollection<Recipe> originalRecipes;
        public ObservableRangeCollection<Recipe> OriginalRecipes { get => originalRecipes; set => SetProperty(ref originalRecipes, value); }

        // Commands
        public AsyncCommand CreateNewRecipe { get; set; }
        public AsyncCommand<Recipe> DeleteRecipeCommand { get; set; }
        public AsyncCommand<Recipe> SelectedCommand { get; set; }
        public MvvmHelpers.Commands.Command RefreshCommand { get; set; }
        public AsyncCommand ShowHelpCommand { get; set; }

        public AsyncCommand DeleteAllRecipesCommand { get; set; }

        // SelectedRecipe property
        Recipe selectedRecipe;
        public Recipe SelectedRecipe
        {
            get => selectedRecipe;
            set => SetProperty(ref selectedRecipe, value);
        }

        // Search bar-related properties
        private bool searchBarIsActive;
        public bool SearchBarIsActive
        {
            get { return searchBarIsActive; }
            set { SetProperty(ref searchBarIsActive, value); }
        }

        // Help message
        string helpMessage = $"Welcome to the Recipe Page!\n\n" +
            $"Here, you can create and edit your own recipes.Once you have created a recipe, you can view its ingredients and method\n\n" +
            $"You can also search through your recipes by keyword, and sort them by name, rating or time.\n\n" +
            "Happy cooking!";

        // SearchText property
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterRecipes();
            }
        }

        // Filter-related properties
        public ObservableRangeCollection<string> Filters { get; set; } = new ObservableRangeCollection<string>();
        private string selectedFilter;
        public string SelectedFilter
        {
            get => selectedFilter;
            set
            {
                SetProperty(ref selectedFilter, value);
                FilterRecipes();
            }
        }

        // Toggle search bar command
        public MvvmHelpers.Commands.Command ToggleSearchBarCommand => new MvvmHelpers.Commands.Command(() =>
        {
            SearchBarIsActive = !SearchBarIsActive;
        });

        public MvvmHelpers.Commands.Command ClearPickerSelectionCommand { get; }




        // RecipeViewModel constructor
        public RecipeViewModel()
        {
            Title = "Recipe Book";
            Recipes = new ObservableRangeCollection<Recipe>();
            OriginalRecipes = new ObservableRangeCollection<Recipe>();
            ShowHelpCommand = new AsyncCommand(ShowHelp);
            CreateNewRecipe = new AsyncCommand(NewRecipe);
            DeleteRecipeCommand = new AsyncCommand<Recipe>(DeleteRecipe);
            SelectedCommand = new AsyncCommand<Recipe>(Selected);
            RefreshCommand = new MvvmHelpers.Commands.Command(Refresh);
            DeleteAllRecipesCommand = new AsyncCommand(DeleteAllRecipes);
            ClearPickerSelectionCommand = new MvvmHelpers.Commands.Command(ClearPickerSelection);
            SelectedRecipe = null;
            LoadDataAsync();
            //LoadFilters();
        }



        private async Task DeleteAllRecipes()
        {
            await RecipeService.DeleteAllRecipes();
            await FirestoreService.DeleteAllRecipes();
        }

        // Method to filter recipes based on search text and selected filter
        private void FilterRecipes()
        {
            if (string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == "all")
            {
                Recipes.ReplaceRange(OriginalRecipes);
            }
            else if (string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == null)
            {
                Recipes.ReplaceRange(OriginalRecipes);
            }
            else if (!string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == "all")
            {
                var searchedRecipes = OriginalRecipes.Where(r => r.RecipeName.ToLower().Contains(SearchText.ToLower())).ToList();
                Recipes.ReplaceRange(searchedRecipes);
            }
            else if (string.IsNullOrWhiteSpace(SearchText) && SelectedFilter != null)
            {
                var filteredRecipes = OriginalRecipes.Where(r => r.Tags != null && r.Tags.ToLower().Contains(SelectedFilter.ToLower()));
                Recipes.ReplaceRange(filteredRecipes);
            }
            else if (!string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == null)
            {
                var searchedRecipes = OriginalRecipes.Where(r => r.RecipeName.ToLower().Contains(SearchText.ToLower())).ToList();
                Recipes.ReplaceRange(searchedRecipes);
            }
            else if (!string.IsNullOrWhiteSpace(SearchText) && SelectedFilter != null)
            {
                var searchedRecipes = OriginalRecipes.Where(r => r.Tags != null && r.Tags.ToLower().Contains(SelectedFilter.ToLower()));
                var filteredRecipes = searchedRecipes.Where(r => r.RecipeName.ToLower().Contains(SearchText.ToLower())).ToList();
                Recipes.ReplaceRange(filteredRecipes);
            }
        }

        private void ClearPickerSelection()
        {
            SelectedFilter = null;
            FilterRecipes();
        }

        // Method to display help message
        public async Task ShowHelp()
        {
            await Application.Current.MainPage.DisplayAlert("Help", helpMessage, "Close");
        }

        // Method to create a new recipe
        private async Task NewRecipe()
        {
            var route = $"//RecipePage/AddRecipePage";
            await Shell.Current.GoToAsync(route);
        }

        // Method to handle item selection
        async Task Selected(Recipe recipe)
        {
                var route = $"//RecipePage/RecipeDetailsPage?RecipeId={recipe.Id}";
                await Shell.Current.GoToAsync(route);
        }

        // Method to delete a recipe
        async Task DeleteRecipe(Recipe recipe)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Delete", "Are you sure you want to delete " + recipe.RecipeName, "Yes", "No");
            if (confirm)
            {
                await RecipeService.RemoveRecipe(recipe.Id);
                await FirestoreService.DeleteRecipe(recipe.Id);
                Refresh();
            }
            return;
        }

        private void Refresh()
        {
            IsBusy = true;

            try
            {
                //await Task.Delay(50); // Simulates a brief asynchronous operation.

                LoadDataAsync(); // Ensure this method is awaited properly.

                // Process your data and update the UI here.
                var tags = recipes
                    .Where(r => r.Tags != null)
                    .SelectMany(r => r.Tags.Split(',').Select(tag => tag.Trim().ToLower()))
                    .Distinct()
                    .ToList();

                LoadFilters();
            }
            catch (Exception ex)
            {
                // Consider logging the exception or displaying an error message to the user.
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure IsBusy is always reset, allowing the UI to update correctly.
                IsBusy = false;
            }
        }
        private void LoadDataAsync()
        {
            Recipes.Clear();
            OriginalRecipes.Clear();


            var recipes = RecipeService.Recipes;

            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                var userRecipes = recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid);
                Recipes.ReplaceRange(userRecipes);
                OriginalRecipes.ReplaceRange(userRecipes);
            }
            else
            {
                Recipes.ReplaceRange(recipes);
                OriginalRecipes.ReplaceRange(recipes);
            }

            IsBusy = false;
        }

        private void LoadFilters()
        {
            Filters.Clear();

            // Get all unique tags in the recipes list
            var recipes = Recipes;

            var tags = recipes
                .Where(r => r.Tags != null)
                .SelectMany(r => r.Tags.Split(',').Select(tag => tag.Trim().ToLower()))
                .Distinct()
                .ToList();

            // Update the filter list with the unique tags
            Filters.ReplaceRange(tags);
            if (!Filters.Contains("all"))
            {
                Filters.Add("all");
            }
        }

    }
}
