using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls.PlatformConfiguration;
using MvvmHelpers;
using MvvmHelpers.Commands;
using Newtonsoft.Json;
using Plugin.Firebase.Auth;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;
using System.Diagnostics;
using System.Web;

namespace RecipeApp.ViewModels
{
    public class ShoppingListMakerViewModel : CustomViewModelBase
    {
        string helpMessage = $"Welcome to the shopping list maker!\n\n" +
                     $"Choose your recipes, snacks, and essentials, and hit 'Go Shopping' to generate a shopping list based on ingredient tags. Use the 'List Checker' to confirm if you need to add an ingredient or if you already have it.\n\n Happy shopping!";

        Recipe selectedRecipe;
        public Recipe SelectedRecipe
        {
            get => selectedRecipe;

            set => SetProperty(ref selectedRecipe, value);
        }
        public string ShoppingListString { get; set; }

        private bool isRecipeSelected;
        public bool IsRecipeSelected
        {
            get => isRecipeSelected;
            set => SetProperty(ref isRecipeSelected, value);
        }

        private bool showSnacks;
        public bool ShowSnacks
        {
            get { return showSnacks; }
            set { SetProperty(ref showSnacks, value); }
        }

        private bool showStaples;

        public bool ShowStaples
        {
            get { return showStaples; }
            set { SetProperty(ref showStaples, value); }
        }

        private bool checkerToggle = false;
        private ObservableRangeCollection<Grouping<string, Ingredient>> collectionIngredients;
        public bool CheckerToggle
        {
            get { return checkerToggle; }
            set { SetProperty(ref checkerToggle, value); }
        }
        public bool SaveComplete
        {
            get { return saveComplete; }
            set { SetProperty(ref saveComplete, value); }
        }
        private bool saveComplete = false;

        private SemaphoreSlim _navigationSemaphore = new SemaphoreSlim(1);
        private Color snackColour = Color.FromArgb("#FF6347");
        private Color stapleColour = Color.FromArgb("#FF6347");

        public MealPlan MealPlan { get; set; }
        public AsyncCommand GoShoppingCommand { get; set; }
        public AsyncCommand RefreshCommand { get; set; }
        public AsyncCommand ShowHelpCommand { get; set; }
        public MvvmHelpers.Commands.Command ShowPlainTextCommand { get; set; }
        public AsyncCommand AddSnackCommand { get; set; }
        public AsyncCommand<Snack> DeleteSnackCommand { get; set; }
        public AsyncCommand AddStapleCommand { get; set; }
        public AsyncCommand<Staple> DeleteStaplesCommand { get; set; }
        public AsyncCommand ShowSnacksCommand { get; set; }
        public AsyncCommand ShowStaplesCommand { get; set; }
        public AsyncCommand<Snack> EditSnackCommand { get; set; }
        public AsyncCommand<Staple> EditStapleCommand { get; set; }
        public AsyncCommand BlankShoppingCommand { get; set; }
        public AsyncCommand CreateNewRecipe { get; set; }
        public MvvmHelpers.Commands.Command ClearPickerSelectionCommand { get; set; }


        public AsyncCommand SaveCommand { get; set; }
        public MvvmHelpers.Commands.Command ClearSelectedCommand { get; set; }
        private ObservableRangeCollection<Staple> staples;
        public ObservableRangeCollection<Staple> Staples { get => staples; set => SetProperty(ref staples, value); }
        private ObservableRangeCollection<Staple> originalStaples;
        public ObservableRangeCollection<Staple> OriginalStaples { get => originalStaples; set => SetProperty(ref originalStaples, value); }
        private ObservableRangeCollection<Snack> snacks;
        public ObservableRangeCollection<Snack> Snacks { get => snacks; set => SetProperty(ref snacks, value); }
        private ObservableRangeCollection<Snack> originalSnacks;
        public ObservableRangeCollection<Snack> OriginalSnacks { get => originalSnacks; set => SetProperty(ref originalSnacks, value); }
        public ObservableRangeCollection<Recipe> recipes;
        public ObservableRangeCollection<Recipe> Recipes { get => recipes; set => SetProperty(ref recipes, value); }
        public ObservableRangeCollection<Recipe> originalRecipes;
        public ObservableRangeCollection<Recipe> OriginalRecipes { get => originalRecipes; set => SetProperty(ref originalRecipes, value); }
        public ObservableRangeCollection<string> Filters { get; set; } = new ObservableRangeCollection<string>();
        public AsyncCommand<Recipe> SelectedCommand { get; set; }
        public List<Ingredient> ListOfIngredients { get; set; }
        public List<Ingredient> PlainListOfIngredients { get; set; } = new List<Ingredient>();
        public Color SnackColour { get => snackColour; set => SetProperty(ref snackColour, value); }
        public Color StapleColour { get => stapleColour; set => SetProperty(ref stapleColour, value); }

        public ObservableRangeCollection<Grouping<string, Ingredient>> CollectionIngredients
        {
            get { return collectionIngredients; }
            set { SetProperty(ref collectionIngredients, value); }
        }
        #region Searchbar

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
        private bool searchBarIsActive;
        public bool SearchBarIsActive
        {
            get { return searchBarIsActive; }
            set { SetProperty(ref searchBarIsActive, value); }
        }

        public MvvmHelpers.Commands.Command ToggleSearchBarCommand => new MvvmHelpers.Commands.Command(() =>
        {
            SearchBarIsActive = !SearchBarIsActive;
        });

        #endregion
        public bool IsManualList { get; private set; }

        public ShoppingListMakerViewModel()
        {
            ShowSnacksCommand = new AsyncCommand(async () =>
            {
                ShowSnacks = !ShowSnacks;
                if (Snacks.Count == 0)
                {
                    GetSnacks();
                }
                SnackColour = ShowSnacks ? Color.FromArgb("#36C977") : Color.FromArgb("#FF6347");
            });

            ShowStaplesCommand = new AsyncCommand(async () =>
            {
                ShowStaples = !ShowStaples;
                if (Staples.Count == 0)
                {
                    GetStaples();
                }
                StapleColour = ShowStaples ? Color.FromArgb("#36C977") : Color.FromArgb("#FF6347");

            });

            RefreshCommand = new AsyncCommand(Refresh);
            ShowHelpCommand = new AsyncCommand(ShowHelp);
            GoShoppingCommand = new AsyncCommand(GoShoppingAsync);
            AddSnackCommand = new AsyncCommand(AddSnack);
            DeleteSnackCommand = new AsyncCommand<Snack>(DeleteSnack);
            AddStapleCommand = new AsyncCommand(AddStaple);
            DeleteStaplesCommand = new AsyncCommand<Staple>(DeleteStaple);
            SelectedCommand = new AsyncCommand<Recipe>(Selected);
            EditSnackCommand = new AsyncCommand<Snack>(EditSnack);
            EditStapleCommand = new AsyncCommand<Staple>(EditStaple);
            ClearPickerSelectionCommand = new MvvmHelpers.Commands.Command(ClearPickerSelection);
            Recipes = new ObservableRangeCollection<Recipe>();
            OriginalRecipes = new ObservableRangeCollection<Recipe>();
            Snacks = new ObservableRangeCollection<Snack>();
            OriginalSnacks = new ObservableRangeCollection<Snack>();
            Staples = new ObservableRangeCollection<Staple>();
            OriginalStaples = new ObservableRangeCollection<Staple>();
            ListOfIngredients = new List<Ingredient>();
            MealPlan = new MealPlan();
            SaveCommand = new AsyncCommand(UpdateIsSelected);
            ClearSelectedCommand = new MvvmHelpers.Commands.Command(DeselectAll);
            BlankShoppingCommand = new AsyncCommand(BlankShopping);
            CreateNewRecipe = new AsyncCommand(NewRecipe);
            LoadDataAsync();
        }


        public async Task ShowHelp()
        {
            await Application.Current.MainPage.DisplayAlert("Help", helpMessage, "Close");
        }
        public async Task GoShoppingAsync()
        {
            var shoppingList = new List<string>();
            var shoppingListDictionary = new Dictionary<string, List<string>>();

            var allIngredients = Recipes.Where(r => r.IsSelected).SelectMany(r => r.Ingredients.Where(r => r.MainIngredient))
                .Concat(Snacks.Where(s => s.IsSelected).Select(s => new Ingredient { Name = s.Name, Tag = s.Tag.Replace(" ", "") }))
                .Concat(Staples.Where(s => s.IsSelected).Select(s => new Ingredient { Name = s.Name, Tag = s.Tag.Replace(" ", "") }));

            // Add these extra items to your allIngredients list
            //allIngredients = allIngredients.Concat(extraItems.Select(item => new Ingredient { Name = item })).ToList();

            if (CheckerToggle)
            {
                foreach (var ingredient in allIngredients)
                {
                    if (!await Application.Current.MainPage.DisplayAlert($"Do you have {ingredient.Name}?", "", "Yes", "No"))
                    {
                        if (!shoppingListDictionary.ContainsKey(ingredient.Tag))
                        {
                            shoppingListDictionary.Add(ingredient.Tag, new List<string>());
                        }

                        shoppingListDictionary[ingredient.Tag].Add(ingredient.Name);
                    }
                }

                var shoppingListText = string.Join("\n", shoppingListDictionary.Select(kv =>
                {
                    var sectionTitle = $"[{kv.Key}]";
                    var sectionItems = string.Join("\n", kv.Value);
                    return $"{sectionTitle}\n{sectionItems}\n";
                }));

                var ingredientsJson = JsonConvert.SerializeObject(allIngredients);


                var route = $"//ShoppingListMakerPage/ShoppingListPage?ShoppingList={HttpUtility.UrlEncode(shoppingListText)}&ingredientsJson={HttpUtility.UrlEncode(ingredientsJson)}";
                await Shell.Current.GoToAsync(route);
            }
            else
            {
                var ingredients = allIngredients
                    .OrderBy(i => i.Tag?.Replace(" ", "") ?? ".")
                    .ThenBy(i => i.Name)
                    .ToList();

                if (ingredients.Count > 0) // I assume this should be > 0 instead of < 0 for logic to make sense.
                {
                    // Assuming ListOfIngredients is meant to be assigned here
                    ListOfIngredients = ingredients;

                    var groupedIngredients = ingredients
                        .GroupBy(i => i.Tag?.Replace(" ", "") ?? ".")
                        .OrderBy(g => g.Key)
                        .Select(g => new Grouping<string, Ingredient>(
                            g.Key,
                            g.GroupBy(i => i.Name)
                             .Select(subg => new Ingredient
                             {
                                 Name = $"{subg.Key} x{subg.Count()}",
                                 Tag = g.Key // Assuming you have a Tag property to keep consistent with grouping
                             })
                             .ToList()))
                        .ToList();

                    // Generate shopping list text with counts included in the item names
                    var shoppingListText = string.Join("\n", groupedIngredients.Select(g =>
                    {
                        var sectionTitle = $"[{g.Key}]";
                        var sectionItems = string.Join("\n", g.Select(i => i.Name)); // Name now includes counts
                        return $"{sectionTitle}\n{sectionItems}\n";
                    }));

                    var ingredientsJson = JsonConvert.SerializeObject(ListOfIngredients);
                    Preferences.Set("IngredientsJson", ingredientsJson);

                    var route = $"//ShoppingListMakerPage/ShoppingListPage";
                    await Shell.Current.GoToAsync(route);
                }
                else
                {

                    //Show a toast notification
                    await Toast.Make("No ingredients selected").Show();
                }
            }

        }

        public async Task BlankShopping()
        {
            IsManualList = true;

            // Step 1: Retrieve the previous shopping list and parse it
            var previousListText = Preferences.Get("ShoppingListText", string.Empty);
            // Assume previous list is stored in a simple format; adjust if it's more complex
            var previousItems = previousListText.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(line => new Ingredient { Name = line }) // Simplified; adjust based on actual format
                                                 .ToList();

            var allIngredients = Recipes.Where(r => r.IsSelected).SelectMany(r => r.Ingredients.Where(r => r.MainIngredient))
                .Concat(Snacks.Where(s => s.IsSelected).Select(s => new Ingredient { Name = s.Name, Tag = s.Tag.Replace(" ", "") }))
                .Concat(Staples.Where(s => s.IsSelected).Select(s => new Ingredient { Name = s.Name, Tag = s.Tag.Replace(" ", "") }));

            var ingredientsWithNotes = allIngredients
                .OrderBy(i => i.Tag?.Replace(" ", "") ?? ".")
                .ThenBy(i => i.Name)
                .ToList();

            // Step 2: Merge the previous list with the new list

            if (ingredientsWithNotes.Count > 0)
            {
                ListOfIngredients = ingredientsWithNotes;

                // Group ingredients by tag, then by name, and count duplicates
                var groupedIngredients = ingredientsWithNotes
                    .GroupBy(i => i.Tag?.Replace(" ", "") ?? ".")
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Tag = g.Key,
                        Ingredients = g.GroupBy(i => i.Name)
                                       .Select(ig => new
                                       {
                                           Name = ig.Key,
                                           Count = ig.Count(),
                                           Note = ig.Select(i => i.Note).FirstOrDefault(n => !string.IsNullOrEmpty(n)) // Optionally aggregate notes
                                       })
                    })
                    .ToList();

                // Step 2: Merge previous and new ingredients
                var combinedIngredients = previousItems.Concat(ListOfIngredients).ToList();

                // Step 3: Apply duplicate consolidation to combined list
                // Adjust the grouping logic if your Ingredient model or storage format is more complex
                var consolidatedList = combinedIngredients.GroupBy(i => i.Name)
                    .Select(g => new
                    {
                        Name = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(item => item.Name) // Or any other ordering you prefer
                    .Select(item => item.Count() > 1 ? $"{item.Name} x{item.Count()}" : item.Name)
                    .ToList();

                // Convert the consolidated list back to the string format expected for storage
                var updatedListText = string.Join("\n", consolidatedList);

                // Append the new list to any existing shopping list and save
                // Step 4: Save the updated list to preferences
                Preferences.Set("ShoppingListText", updatedListText);

                var route = $"//ShoppingListMakerPage/ShoppingListsManualList={IsManualList}";
                await Shell.Current.GoToAsync(route);
            }
            else
            {
                //Show a toast notification
                await Toast.Make("No ingredients selected").Show();
            }

        }


        async Task Selected(Recipe recipe)
        {
            IsBusy = true;

                await _navigationSemaphore.WaitAsync();
                try
                {
                    IsBusy = false;
                    var route = $"//ShoppingListMakerPage/RecipeDetailsPage?RecipeId={recipe.Id}";
                    await Shell.Current.GoToAsync(route);
                }
                finally
                {
                    IsBusy = false;
                    _navigationSemaphore.Release();
                }

        }

        private async Task NewRecipe()
        {
            var route = $"//ShoppingListMakerPage/AddRecipePage";
            await Shell.Current.GoToAsync(route);
        }

        async Task EditSnack(Snack snack)
        {
            var route = $"//ShoppingListMakerPage/EditExtraPage?SnackName={snack.Name}";
            await Shell.Current.GoToAsync(route);
        }

        async Task EditStaple(Staple staple)
        {
            var route = $"//ShoppingListMakerPage/EditExtraPage?StapleName={staple.Name}";
            await Shell.Current.GoToAsync(route);
        }

        public void GetSnacks()
        {
            Snacks.ReplaceRange(RecipeService.Snacks);
        }
        public void GetStaples()
        {
            Staples.ReplaceRange(RecipeService.Staples);
        }

        async Task AddSnack()
        {
            string name = await Application.Current.MainPage.DisplayPromptAsync("New Snack", "Name:", "Add", "Cancel");
            if (!string.IsNullOrWhiteSpace(name))
            {
                string tag = await Application.Current.MainPage.DisplayPromptAsync("New Snack", "Tag:", "Add", "Cancel");
                Snack snack = new Snack { Name = name, Tag = tag, FirebaseUID = CrossFirebaseAuth.Current.CurrentUser?.Uid ?? "" };
                await RecipeService.InsertSnack(snack);
                await FirestoreService.AddSnack(snack);
                Snacks.Clear();
                Snacks.AddRange(RecipeService.Snacks);
            }

        }
        async Task DeleteSnack(Snack snack)
        {
            await RecipeService.DeleteSnack(snack);
            await FirestoreService.DeleteSnack(snack);
            Snacks.Clear();
            var snacks = RecipeService.Snacks;
            Snacks.AddRange(snacks);
        }
        async Task AddStaple()
        {
            string name = await Application.Current.MainPage.DisplayPromptAsync("New Essential", "Name:", "Add", "Cancel");
            if (!string.IsNullOrWhiteSpace(name))
            {
                string tag = await Application.Current.MainPage.DisplayPromptAsync("New Essential", "Tag:", "Add", "Cancel");
                Staple staple = new Staple { Name = name, Tag = tag, FirebaseUID = CrossFirebaseAuth.Current.CurrentUser?.Uid ?? "" };
                await RecipeService.InsertStaple(staple);
                await FirestoreService.AddStaple(staple);
                Staples.Clear();
                Staples.AddRange(RecipeService.Staples);
            }
        }
        async Task DeleteStaple(Staple staple)
        {
            await RecipeService.DeleteStaple(staple);
            await RecipeService.DeleteStaple(staple);
            Staples.Clear();
            var staples = RecipeService.Staples;
            Staples.AddRange(staples);
        }
        async Task Refresh()
        {
            IsBusy = true;

            await Task.Delay(10);

            await UpdateIsSelected();

            Recipes = null;
            Snacks = null;
            Staples = null;

            Recipes = new ObservableRangeCollection<Recipe>();
            Snacks = new ObservableRangeCollection<Snack>();
            Staples = new ObservableRangeCollection<Staple>();

            var recipes = RecipeService.Recipes;
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                Recipes.ReplaceRange(recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
                OriginalRecipes.ReplaceRange(recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
            }
            else
            {
                Recipes.ReplaceRange(recipes);
                OriginalRecipes.ReplaceRange(recipes);
            }

            var snacks = RecipeService.Snacks;
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                Snacks.ReplaceRange(snacks.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
            }
            else
            {
                Snacks.AddRange(snacks);
            }

            var staples = RecipeService.Staples;
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                Staples.ReplaceRange(staples.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
            }
            else
            {
                Staples.AddRange(staples);
            }

            IsBusy = false;
        }

        public async Task UpdateIsSelected()
        {
                foreach (Recipe recipe in Recipes)
                {
                    await RecipeService.UpdateRecipeIsSelected(recipe);
                }

                foreach (Snack snack in Snacks)
                {
                    await RecipeService.UpdateSnackIsSelected(snack);
                }

                foreach (Staple staple in Staples)
                {
                    await RecipeService.UpdateEssentialIsSelected(staple);
                }

                SaveComplete = true;

            await Task.Delay(250);

            SaveComplete = false;
        }

        private async void LoadDataAsync()
        {
            await Refresh();
            //if (Recipes.Count <= 0)
            //{
            //    Recipes.AddRange(RecipeService.Recipes);
            //    OriginalRecipes.AddRange(RecipeService.Recipes);
            //}
            //if (Snacks.Count <= 0)
            //{
            //    Snacks.Clear();
            //    Snacks.AddRange(RecipeService.Snacks);
            //    OriginalSnacks.AddRange(RecipeService.Snacks);
            //}
            //if (Staples.Count <= 0)
            //{
            //    Staples.Clear();
            //    Staples.AddRange(RecipeService.Staples);
            //    OriginalStaples.AddRange(RecipeService.Staples);
            //}
            LoadFilters();
        }

        public async void DeselectAll()
        {
            foreach (var recipe in Recipes)
            {
                recipe.IsSelected = false;
            }
            foreach (var recipe in OriginalRecipes)
            {
                recipe.IsSelected = false;
            }

            foreach (var snack in Snacks)
            {
                snack.IsSelected = false;
            }
            foreach (var snack in OriginalSnacks)
            {
                snack.IsSelected = false;
            }

            foreach (var staple in Staples)
            {
                staple.IsSelected = false;
            }
            foreach (var staple in OriginalStaples)
            {
                staple.IsSelected = false;
            }

            await UpdateIsSelected();
        }

        private void FilterRecipes()
        {
            if (string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == "all")
            {
                Recipes.ReplaceRange(OriginalRecipes);
                Snacks.ReplaceRange(OriginalSnacks);
                Staples.ReplaceRange(OriginalStaples);
            }
            else if (string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == null)
            {
                Recipes.ReplaceRange(OriginalRecipes);
                Snacks.ReplaceRange(OriginalSnacks);
            }
            else if (!string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == "all")
            {
                Recipes.ReplaceRange(OriginalRecipes.Where(r => r.RecipeName.ToLower().Contains(SearchText.ToLower())).ToList());
                Snacks.ReplaceRange(OriginalSnacks.Where(r => r.Name.ToLower().Contains(SearchText.ToLower())).ToList());

            }
            else if (string.IsNullOrWhiteSpace(SearchText) && SelectedFilter != null)
            {
                Recipes.ReplaceRange(OriginalRecipes.Where(r => r.Tags != null && r.Tags.ToLower().Contains(SelectedFilter.ToLower())));
                Snacks.ReplaceRange(OriginalSnacks.Where(r => r.Tag != null && r.Tag.ToLower().Contains(SelectedFilter.ToLower())));
                Staples.ReplaceRange(OriginalStaples.Where(r => r.Tag != null && r.Tag.ToLower().Contains(SelectedFilter.ToLower())));
            }
            else if (!string.IsNullOrWhiteSpace(SearchText) && SelectedFilter == null)
            {
                Recipes.ReplaceRange(OriginalRecipes.Where(r => r.RecipeName.ToLower().Contains(SearchText.ToLower())).ToList());
                Snacks.ReplaceRange(OriginalSnacks.Where(r => r.Name.ToLower().Contains(SearchText.ToLower())).ToList());
                Staples.ReplaceRange(OriginalStaples.Where(r => r.Name.ToLower().Contains(SearchText.ToLower())).ToList());
            }
            else if (!string.IsNullOrWhiteSpace(SearchText) && SelectedFilter != null)
            {
                var searchedRecipes = OriginalRecipes.Where(r => r.Tags != null && r.Tags.ToLower().Contains(SelectedFilter.ToLower()));
                var filteredRecipes = searchedRecipes.Where(r => r.RecipeName.ToLower().Contains(SearchText.ToLower())).ToList();
                Recipes.ReplaceRange(filteredRecipes);

                var searchedSnacks = OriginalSnacks.Where(r => r.Tag != null && r.Tag.ToLower().Contains(SelectedFilter.ToLower()));
                var filteredSnacks = searchedSnacks.Where(r => r.Name.ToLower().Contains(SearchText.ToLower())).ToList();
                Snacks.ReplaceRange(filteredSnacks);

                var searchedStaples = OriginalStaples.Where(r => r.Tag != null && r.Tag.ToLower().Contains(SelectedFilter.ToLower()));
                var filteredStaples = searchedStaples.Where(r => r.Name.ToLower().Contains(SearchText.ToLower())).ToList();
                Staples.ReplaceRange(filteredStaples);
            }
        }
        private void ClearPickerSelection()
        {
            SelectedFilter = null;
            FilterRecipes();
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
