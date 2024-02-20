using MvvmHelpers;
using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels.TabBarViewModels
{
    class RecipesForTabsViewModel : BaseViewModel
    {
        public ObservableRangeCollection<Recipe> recipes;
        public ObservableRangeCollection<Recipe> Recipes { get => recipes; set => SetProperty(ref recipes, value); }
        public ObservableRangeCollection<Recipe> originalRecipes;
        public ObservableRangeCollection<Recipe> OriginalRecipes { get => originalRecipes; set => SetProperty(ref originalRecipes, value); }
        public AsyncCommand RefreshCommand { get; }

        public RecipesForTabsViewModel()
        {
            Recipes = new ObservableRangeCollection<Recipe>();
            OriginalRecipes = new ObservableRangeCollection<Recipe>();
            RefreshCommand = new AsyncCommand(Refresh);
            LoadDataAsync();
        }

        private void LoadDataAsync()
        {
            if (Recipes.Count <= 0)
            {
                Recipes.AddRange(RecipeService.Recipes);
                OriginalRecipes.AddRange(RecipeService.Recipes);
            }
        }

        async Task Refresh()
        {
            IsBusy = true;
            await Task.Delay(10);

            await UpdateIsSelected();

            Recipes = null;
            OriginalRecipes = null;

            Recipes = new ObservableRangeCollection<Recipe>(RecipeService.Recipes);
            OriginalRecipes = new ObservableRangeCollection<Recipe>(RecipeService.Recipes);


            IsBusy = false;
        }

        public async Task UpdateIsSelected()
        {
            foreach (Recipe recipe in Recipes)
            {
                await RecipeService.UpdateRecipeIsSelected(recipe);
            }
        }
    }
}

