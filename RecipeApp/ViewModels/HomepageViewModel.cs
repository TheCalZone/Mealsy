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
            var newIngName = await Application.Current.MainPage.DisplayPromptAsync("Add to Shopping List", "Enter the name of the item you want to add to your shopping list", "Add", "Cancel");
            var shoppingList = Preferences.Get("ShoppingListText", "");
            if (!string.IsNullOrEmpty(newIngName))
            {
                var shoppingListItems = new ObservableRangeCollection<IngredientGroup>();
                string currentTag = "";
                var lines = shoppingList.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentTag = line.Trim('[', ']');
                        if (!shoppingListItems.Any(group => group.Key == currentTag))
                        {
                            shoppingListItems.Add(new IngredientGroup(currentTag, new List<Ingredient>()));
                        }
                    }
                    else
                    {
                        var currentGroup = shoppingListItems.FirstOrDefault(group => group.Key == currentTag);
                        if (currentGroup != null)
                        {
                            currentGroup.Add(new Ingredient { Name = line });
                        }
                    }
                }

                Ingredient matchedIngredient = null;
                string matchedTag = null;

                if (Recipes.Count > 0)
                {
                    // Search for a match in Recipes
                    foreach (var recipe in Recipes)
                    {
                        if (recipe.Ingredients != null)
                        {
                            matchedIngredient = recipe.Ingredients.FirstOrDefault(ing => ing?.Name?.IndexOf(newIngName, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (matchedIngredient != null)
                            {
                                matchedTag = matchedIngredient.Tag;
                                if (matchedTag != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                // Search for a match in Snacks
                if (matchedIngredient == null)
                {
                    if (Snacks.Count > 0)
                    {
                        foreach (var snack in Snacks)
                        {
                            Snack matchedSnack = Snacks.FirstOrDefault(ing => ing.Name.IndexOf(newIngName, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (matchedSnack != null)
                            {
                                matchedIngredient = new Ingredient { Name = matchedSnack.Name, Tag = matchedSnack.Tag };
                                if (matchedSnack != null)
                                {
                                    matchedTag = matchedSnack.Tag;
                                    break;
                                }
                            }
                        }
                    }

                }

                // Search for a match in Staples
                if (matchedIngredient == null)
                {
                    if (Staples.Count > 0)
                    {
                        foreach (var staple in Staples)
                        {
                            Staple matchedStaple = Staples.FirstOrDefault(ing => ing.Name.IndexOf(newIngName, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (matchedStaple != null)
                            {
                                matchedIngredient = new Ingredient { Name = matchedStaple.Name, Tag = matchedStaple.Tag };
                                if (matchedStaple != null)
                                {
                                    matchedTag = matchedStaple.Tag;
                                    break;
                                }
                            }
                        }
                    }

                }

                if (matchedIngredient == null)
                {
                    matchedIngredient = RecipeService.MasterIngredientList.FirstOrDefault(ing => newIngName.IndexOf(ing.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matchedIngredient != null)
                    {
                        matchedTag = matchedIngredient.Tag;
                    }
                }

                // Create an Ingredient based on the matched item
                Ingredient ingredient;
                if (matchedIngredient != null)
                {
                    ingredient = new Ingredient { Name = newIngName, Tag = matchedTag };
                }
                else
                {
                    ingredient = new Ingredient { Name = newIngName };
                }

                // Adding the ingredient to the list
                var targetGroup = shoppingListItems.FirstOrDefault(group => group.Key == (ingredient.Tag ?? "Other"));
                if (targetGroup == null)
                {
                    targetGroup = new IngredientGroup(ingredient.Tag ?? "Other", new List<Ingredient> { ingredient });
                    shoppingListItems.Add(targetGroup);
                }
                else if (targetGroup.All(ing => !ing.Name.Equals(ingredient.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    targetGroup.Add(ingredient);
                }

                // Building the new shopping list string
                var updatedShoppingListText = string.Join("\n", shoppingListItems.OrderBy(g => g.Key).Select(g =>
                {
                    var sectionTitle = $"[{g.Key}]";
                    var sectionItems = string.Join("\n", g.Select(i => i.Name));
                    return $"{sectionTitle}\n{sectionItems}\n";
                }));

                await Toast.Make($"{newIngName} added to shopping list").Show();

                // Saving the new shopping list to preferences
                Preferences.Set("ShoppingListText", updatedShoppingListText);

            }
            else
            {
                return;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cloud refresh failed: {ex.Message}");
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
                    if (item.FirebaseUID == sqlItem.FirebaseUID && item.LastUpdated > sqlItem.LastUpdated)
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
