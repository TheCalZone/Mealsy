using CommunityToolkit.Maui.Alerts;
using MvvmHelpers;
using MvvmHelpers.Commands;
using RecipeApp.Helpers;
using RecipeApp.Interfaces;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;
using System.Windows.Input;

namespace RecipeApp.ViewModels
{
    internal class HomepageViewModel : BaseViewModel
    {
        public AsyncCommand RecipeBookCommand { get;set;}
        public AsyncCommand ListMakerCommand { get; set; }
        public AsyncCommand MealPlannerCommand { get; set; }
        public AsyncCommand ShoppingListCommand { get; }
        public AsyncCommand ChatCommand { get; set; }

        public MvvmHelpers.Commands.Command QuickAddToShoppingListCommand { get; set; }
        public MvvmHelpers.Commands.Command QuickAddNewRecipeCommand { get; }

        private ObservableRangeCollection<Recipe> recipes;
        // Properties
        public ObservableRangeCollection<Recipe> Recipes { get => recipes; set => SetProperty(ref recipes, value); }
        private ObservableRangeCollection<Recipe> sqlRecipes;

        public ObservableRangeCollection<Recipe> SqlRecipes { get => sqlRecipes; set => SetProperty(ref sqlRecipes, value); }

        private ObservableRangeCollection<Snack> snacks;
        // Properties
        public ObservableRangeCollection<Snack> Snacks { get => snacks; set => SetProperty(ref snacks, value); }
        private ObservableRangeCollection<Snack> sqlSnacks;

        public ObservableRangeCollection<Snack> SqlSnacks { get => sqlSnacks; set => SetProperty(ref sqlSnacks, value); }


        private ObservableRangeCollection<Staple> staples;
        // Properties
        public ObservableRangeCollection<Staple> Staples { get => staples; set => SetProperty(ref staples, value); }
        private ObservableRangeCollection<Staple> sqlStaples;

        public ObservableRangeCollection<Staple> SqlStaples { get => sqlStaples; set => SetProperty(ref sqlStaples, value); }



        public HomepageViewModel()
        {
            Recipes = new ObservableRangeCollection<Recipe>();
            Snacks = new ObservableRangeCollection<Snack>();
            Staples = new ObservableRangeCollection<Staple>();
            ListMakerCommand = new AsyncCommand(GoToShoppingList);
            RecipeBookCommand = new AsyncCommand(GoToRecipeBook);
            MealPlannerCommand = new AsyncCommand(GoToMealPlanner);
            ShoppingListCommand = new AsyncCommand(GoToList);
            ChatCommand = new AsyncCommand(Chat);
            QuickAddToShoppingListCommand = new MvvmHelpers.Commands.Command(QuickAddToShoppingList);
            QuickAddNewRecipeCommand = new MvvmHelpers.Commands.Command(QuickAddNewRecipe);
            //LoadDataAsync();
        }

        private async void QuickAddNewRecipe(object obj)
        {
            await Shell.Current.GoToAsync($"//Homepage/AddRecipePage");
        }

        private async void QuickAddToShoppingList()
        {
            var name = await Shell.Current.DisplayPromptAsync("Quick add", "What do you need?", "Add", "Cancel");
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                var match = RecipeService.MasterIngredientList.FirstOrDefault(i => name.Contains(i.Name, StringComparison.OrdinalIgnoreCase));
                ShoppingListStore.AppendIngredients(new[] { new Ingredient { Name = name.Trim(), Tag = match?.Tag } });
                await Toast.Make("Added to your shopping list").Show();
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Item not added", "The saved list could not be updated. Please open your shopping list and try again.", "OK");
            }
        }




        private async Task Chat()
        {
            var route = $"{nameof(ChatPage)}";
            await Shell.Current.GoToAsync(route);
        }

        async Task GoToRecipeBook()
        {
            await Shell.Current.GoToAsync($"//RecipePage");
        }

        async Task GoToShoppingList()
        {
            await Shell.Current.GoToAsync($"//ShoppingListMakerPage");
        }

        async Task GoToMealPlanner()
        {
            await Shell.Current.GoToAsync($"//MealPlanOverviewPage");
        }

        async Task GoToList()
        {
            var IsManualList = true;
            await Shell.Current.GoToAsync($"//ShoppingListPage?IsManualList={IsManualList}");
        }

        private async void LoadDataAsync(string userId)
        {
            try
            {
                Recipes.Clear();
                Snacks.Clear();
                Staples.Clear();

                //Fetch recipes
                Recipes.AddRange(await FirestoreService.GetAllRecipesAsync());
                SqlRecipes.AddRange(RecipeService.Recipes);

                await UpdateLocalData(Recipes.ToList(), SqlRecipes.ToList(), recipe => RecipeService.UpdateRecipeAndIngredientsAsync(recipe, recipe.Ingredients));

                // Fetch snacks
                Snacks.AddRange(await FirestoreService.GetAllSnacks());
                SqlSnacks.AddRange(RecipeService.Snacks);

                await UpdateLocalData(Snacks.ToList(), SqlSnacks.ToList(), RecipeService.UpdateSnack);

                // Fetch staples
                Staples.AddRange(await FirestoreService.GetAllEssentials());
                SqlStaples.AddRange(RecipeService.Staples);

                await UpdateLocalData(Staples.ToList(), SqlStaples.ToList(), RecipeService.UpdateStaple);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task UpdateLocalData<T>(List<T> firestoreList, List<T> sqlList, Func<T, Task> updateFunc) where T : IIdentifiable
        {
            // Convert the list of SQL items into a dictionary
            var sqlItemsDict = sqlList.ToDictionary(r => r.Id);

            // Iterate over the Firestore items
            foreach (var item in firestoreList)
            {
                // Try to find the corresponding SQL item
                if (sqlItemsDict.TryGetValue(item.Id, out var sqlItem))
                {
                    // Now you have both the Firestore item and the corresponding SQL item
                    // You can compare their LastUpdated property or perform any other comparison you need
                    if (item.LastUpdated > sqlItem.LastUpdated)
                    {
                        // The Firestore item is more recent, so update the SQL item
                        await updateFunc(item);
                    }
                }
                else
                {
                    // There is no corresponding SQL item for this Firestore item
                    // You can handle this case as needed, for example, add the Firestore item to the SQL database
                }
            }
        }
    }
}
