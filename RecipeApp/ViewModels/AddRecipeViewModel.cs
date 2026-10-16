
using CommunityToolkit.Maui.Alerts;
using MvvmHelpers;
using MvvmHelpers.Commands;
using Newtonsoft.Json;
using RecipeApp.Actions;
using RecipeApp.Helpers;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RecipeApp.ViewModels
{
    public class AddRecipeViewModel : CustomViewModelBase
    {
        private string recipeName;
        private string recipeMethod;
        private string ingredientsText;
        private string tagsText;
        private string recipeImage;
        private string servingText;
        private string prepText;
        private string ratingText;
        public string RecipeName { get => recipeName; set => SetProperty(ref recipeName, value); }
        public string RecipeMethod { get => recipeMethod; set => SetProperty(ref recipeMethod, value); }
        public string RecipeImage { get => recipeImage; set => SetProperty(ref recipeImage, value); }
        public string IngredientsText
        {
            get => ingredientsText;
            set
            {
                SetProperty(ref ingredientsText, value);
            }
        }
        public string TagsText { get => tagsText; set => SetProperty(ref tagsText, value); }

        private ObservableRangeCollection<Ingredient> ingredients;
        public ObservableRangeCollection<Ingredient> Ingredients { get => ingredients; set => SetProperty(ref ingredients, value); }

        public string ServingText
        {
            get => servingText;
            set
            {
                SetProperty(ref servingText, value.ToString());
            }
        }
        public string PrepText
        {
            get => prepText;
            set
            {
                SetProperty(ref prepText, value.ToString());
            }
        }
        public string RatingText
        {
            get => ratingText;
            set
            {
                SetProperty(ref ratingText, value.ToString());
            }
        }

        private ObservableCollection<string> possibleTags;
        public ObservableCollection<string> PossibleTags { get => possibleTags; set => SetProperty(ref possibleTags, value); }

        public ObservableCollection<string> SuggestedTags { get; } = new ObservableCollection<string>();

        private string _tagInput;
        public string TagInput
        {
            get => _tagInput;
            set
            {
                SetProperty(ref _tagInput, value);
                UpdateSuggestedTags();
            }
        }

        private bool listViewVisible;
        public bool ListViewVisible
        {
            get { return listViewVisible; }
            set
            {
                SetProperty(ref listViewVisible, value);
            }
        }

        private bool isBulkEditorVisible;
        public bool IsBulkEditorVisible
        {
            get { return isBulkEditorVisible; }
            set
            {
                SetProperty(ref isBulkEditorVisible, value);
            }
        }

        private string bulkIngredientsText;
        public string BulkIngredientsText
        {
            get => bulkIngredientsText;
            set
            {
                SetProperty(ref bulkIngredientsText, value);
            }
        }

        private Color iconColor;
        public Color IconColor
        {
            get
            {
                if (AppInfo.RequestedTheme == AppTheme.Dark)
                {
                    // Dark theme is active, set icon color to white
                    return Color.FromArgb("#FFFFFF");
                }
                else
                {
                    // Light theme is active, set icon color to black
                    return Color.FromArgb("#000000");
                }
            }
            set
            {
                SetProperty(ref iconColor, value);
            }
        }

        public IngredientGroup CurrentGroup { get; private set; }

        private ObservableRangeCollection<IngredientGroup> listOfIngredients;
        public ObservableRangeCollection<IngredientGroup> ListOfIngredients { get => listOfIngredients; set => SetProperty(ref listOfIngredients, value); }

        private string selectedTag;
        public string SelectedTag { get => selectedTag;set=>SetProperty(ref selectedTag, value); }
        public MvvmHelpers.Commands.Command PerformSearchCommand => new MvvmHelpers.Commands.Command<string>((query) =>
        {
            SuggestedTags.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            foreach (var tag in PossibleTags) // allTags should be a collection of all possible tags
            {
                if (tag.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    SuggestedTags.Add(tag);
                }
            }
        });
        public AsyncCommand UploadImageCommand { get; set; }

        public MvvmHelpers.Commands.Command AddIngredientCommand { get; set; }
        public MvvmHelpers.Commands.Command<Ingredient> RemoveIngredientCommand { get; set; }
        public MvvmHelpers.Commands.Command<IngredientGroup> AddIngredientToGroupCommand { get; set; }
        public MvvmHelpers.Commands.Command AddGroupCommand { get; set; }
        public MvvmHelpers.Commands.Command AddNoteCommand { get; set; }
        public MvvmHelpers.Commands.Command<IngredientGroup> RemoveGroupCommand { get; set; }
        public MvvmHelpers.Commands.Command BulkAddIngredientsCommand { get; set; }
        public MvvmHelpers.Commands.Command ShowBulkEditorForGroupCommand { get; set; }
        public AsyncCommand ImportRecipeCommand { get; set; }
        public AsyncCommand SaveCommand { get; set; }

        public AddRecipeViewModel()
        {
            Title = "Add Recipe";
            ListOfIngredients = new ObservableRangeCollection<IngredientGroup>();
            ListOfIngredients.Add(new IngredientGroup("Ingredients", new List<Ingredient>()));
            //for (int i = 0; i < 2; i++)
            //{
            //    var group = new IngredientGroup { Key = "New group" + (ListOfIngredients.Count +1)};
            //    for (int j = 0; j < 1; j++) // You can adjust the number of ingredients per group
            //    {
            //        group.Add(new Ingredient { Name = $"TestIngredient{i * 2 + j + 1}" });
            //    }
            //    ListOfIngredients.Add(group);
            //}


            AddGroupCommand = new MvvmHelpers.Commands.Command(AddGroup);
            RemoveGroupCommand = new MvvmHelpers.Commands.Command<IngredientGroup>(RemoveGroup);
            Ingredients = new ObservableRangeCollection<Ingredient>();
            SaveCommand = new AsyncCommand(Save);
            UploadImageCommand = new AsyncCommand(UploadImage);
            AddIngredientCommand = new MvvmHelpers.Commands.Command(AddIngredient);
            RemoveIngredientCommand = new MvvmHelpers.Commands.Command<Ingredient>(DeleteIngredient);
            AddNoteCommand = new MvvmHelpers.Commands.Command<Ingredient>(AddNote);
            ImportRecipeCommand = new AsyncCommand(ImportRecipe);
            AddIngredientToGroupCommand = new MvvmHelpers.Commands.Command<IngredientGroup>(AddIngredientToGroup);
            BulkAddIngredientsCommand = new MvvmHelpers.Commands.Command<IngredientGroup>(BulkAddIngredients);
            ShowBulkEditorForGroupCommand = new MvvmHelpers.Commands.Command<IngredientGroup>(ShowBulkEditorForGroup);
            PossibleTags = new ObservableCollection<string> { "Meat","Dairy","Bakery","Meal"};
        }

        private async void AddNote(Ingredient ing)
        {
            if (ing.Note != null)
            {
                bool changeNote = await Shell.Current.DisplayAlert("Note", ing.Note, "Change Note", "Close");

                if (changeNote)
                {
                    string Note = await Shell.Current.DisplayPromptAsync("Note", "Add Note");
                    ing.Note = Note;
                }
            }
            else
            {
                string Note = await Shell.Current.DisplayPromptAsync("Note", "Add Note");
                ing.Note = Note;
            }
        }

        private async Task<Recipe> ImportRecipe()
        {
            string url = await Shell.Current.DisplayPromptAsync("Recipe Import", "Type the link below");

            HttpClient httpClient = new HttpClient();

            try
            {
                // Make an HTTP GET request to the provided URL
                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Read the content as a string
                    string content = await response.Content.ReadAsStringAsync();

                    // Parse the content (assuming it's in JSON format)
                    var recipe = JsonConvert.DeserializeObject(content);

                    await Toast.Make("Recipe Imported").Show();
                    // You can now use the 'recipe' object to update your UI or ViewModel
                    return null;
                }
                else
                {
                    // Handle unsuccessful response (e.g., display an error message)
                    Console.WriteLine($"Error: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., network issues, JSON parsing errors)
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }

        async Task Save()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (string.IsNullOrWhiteSpace(RecipeName))
                {
                    await Shell.Current.DisplayAlert("Error", "Please enter a recipe name.", "OK");
                    return;
                }
                var recipe = new Recipe { RecipeName = RecipeName, RecipeMethod = RecipeMethod };

                if (int.TryParse(ServingText, out int servingInt))
                {
                    recipe.Serving = servingInt;
                }
                else
                {
                    recipe.Serving = 0;
                }

                if (int.TryParse(PrepText, out int prepInt))
                {
                    recipe.PrepTime = prepInt;
                }
                else
                {
                    recipe.PrepTime = 0;
                }

                if (int.TryParse(RatingText, out int ratingInt))
                {
                    recipe.Rating = ratingInt;
                }
                else
                {
                    recipe.Rating = 0;
                }

                Ingredients.Clear();
                foreach (IngredientGroup group in ListOfIngredients)
                {
                    foreach (Ingredient item in group)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Name))
                        {
                            item.IngredientSection = group.Key;
                            Ingredients.Add(item);
                        }
                    }
                }


                recipe.Tags = TagsText + ",all";
                recipe.RecipeImage = RecipeImage;
                recipe.Ingredients = Ingredients.ToList();
                recipe.FirebaseIngredients = Ingredients.ToList();
                recipe.RecipeMethod = RecipeMethod;
                recipe.LastUpdated = DateTime.UtcNow;

                if (AuthHelper.UserId != null)
                {
                    recipe.FirebaseUID = AuthHelper.UserId;
                }

                await RecipeService.AddRecipe(recipe);
                var firebaserecipe = await RecipeService.GetRecipe(recipe.Id);
                try { await FirestoreService.AddRecipe(firebaserecipe); }
                catch (Exception) { await Shell.Current.DisplayAlert("Saved locally", "Cloud sync failed. Your recipe is saved on this device.", "OK"); }
                await Shell.Current.GoToAsync("..");


            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Save failed", "Your changes could not be saved. Please check your connection and try again.", "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task UploadImage()
        {
            // Prompt the user to select a photo
            var result = await MediaPicker.PickPhotoAsync();

            if (result != null)
            {
                // Get the stream of the selected photo
                var stream = await result.OpenReadAsync();

                // Read the stream into a byte array
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                // Save the byte array to the recipe object
                RecipeImage = Convert.ToBase64String(bytes);

            }
            else
            {
                return;
            }
        }


        private void AddIngredient()
        {
            var ingredient = new Ingredient();
            ingredient.OnNameChanged += Ingredient_OnNameChanged;
            Ingredients.Add(ingredient);
        }

        private void AddIngredientToGroup(IngredientGroup group)
        {
            var ingredient = new Ingredient();
            ingredient.MainIngredient = true;
            ingredient.OnNameChanged += Ingredient_OnNameChanged;
            ingredient.IngredientSection = group.Key;
            group.Add(ingredient);
        }
        private void Ingredient_OnNameChanged(object sender, EventArgs e)
        {
            var ingredient = sender as Ingredient;

            if (ingredient != null)
            {
                // Suggest a tag based on the ingredient name
                var suggestedTag = SuggestTagForIngredient(ingredient.Name);

                if (!string.IsNullOrWhiteSpace(suggestedTag))
                {
                    ingredient.Tag = suggestedTag;
                }
            }
        }
        private string SuggestTagForIngredient(string ingredientName)
        {
            if (string.IsNullOrWhiteSpace(ingredientName)) return string.Empty;
            var matchedIngredient = RecipeService.MasterIngredientList.FirstOrDefault(ing => ingredientName.IndexOf(ing.Name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (matchedIngredient != null)
            {
                string matchedTag = matchedIngredient.Tag;
                return matchedTag;
            }

            return string.Empty;
        }
        private void UpdateSuggestedTags()
        {
            SuggestedTags.Clear();

            if (!string.IsNullOrWhiteSpace(TagInput))
            {
                var suggestions = PossibleTags
                    .Where(tag => tag.StartsWith(TagInput, StringComparison.InvariantCultureIgnoreCase));

                foreach (var suggestion in suggestions)
                {
                    SuggestedTags.Add(suggestion);
                }
            }
        }

        private void ShowBulkEditorForGroup(IngredientGroup group)
        {
            //Show the bulkk editor for the group if it's closed, and close it if it's open
            group.IsGroupBulkEditorVisible = !group.IsGroupBulkEditorVisible;
        }

        private void BulkAddIngredients(IngredientGroup group)
        {
            //if (string.IsNullOrWhiteSpace(group.BulkEditorText)) return;

            //if (group == null) return;

            List<Ingredient> extractedIngredients = new List<Ingredient>();

            // Regular expression to match quantities, units, ingredients, and notes
            var regex = new Regex(@"(?<Quantity>\d+\/\d+|\d+\.\d+|\d+|\d+\s\d+\/\d+)(?:\s*(?<Unit>tbsp|tsp|cups?|pounds?|lbs?|oz|kg|g|ml|l|teaspoons?|tablespoons?|cup|pound|lb|ounce|ounces))?\s+(?<Ingredient>[^,]+)(?:,\s*(?<Note>.*))?", RegexOptions.IgnoreCase);

            // Split the pasted list into individual items
            if (!string.IsNullOrWhiteSpace(group.BulkEditorText))
            {
                List<string> items = group.BulkEditorText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("[") && !line.TrimStart().StartsWith("]"))
    .ToList();

                // Iterate through each item in the list
                foreach (string item in items)
                {
                    // Use regular expression to parse the input
                    var match = regex.Match(item);

                    if (match.Success)
                    {
                        var quantity = match.Groups["Quantity"].Value;
                        var unit = match.Groups["Unit"].Value;
                        var ingredientName = match.Groups["Ingredient"].Value;
                        var note = match.Groups["Note"].Value;

                        // Combine quantity and unit
                        var combinedQuantity = string.IsNullOrEmpty(unit) ? quantity : $"{quantity} {unit}";

                        if (string.IsNullOrEmpty(ingredientName))
                        {
                            // Use the entire match as the ingredient
                            ingredientName = match.Value.Trim();
                        }
                        // Try to find the matched ingredient in your master list
                        Ingredient matchedIngredient = RecipeService.MasterIngredientList.FirstOrDefault(ing => ingredientName.IndexOf(ing.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                        string matchedTag = (matchedIngredient != null) ? matchedIngredient.Tag : null;

                        // Create an Ingredient based on the matched item
                        Ingredient ingredient;
                        if (matchedIngredient != null)
                        {
                            ingredient = new Ingredient { Name = ingredientName, Note = note, Tag = matchedTag, Quantity = combinedQuantity };
                        }
                        else
                        {
                            ingredient = new Ingredient { Name = ingredientName, Note = note, Quantity = combinedQuantity };
                        }

                        ingredient.MainIngredient = true;
                        // Instead of adding to PlainIngredients, add to the extractedIngredients list:
                        extractedIngredients.Add(ingredient);
                    }
                }

                group.AddRange(extractedIngredients);
                group.BulkEditorText = "";
                group.IsGroupBulkEditorVisible = false;

            }
            else
            {
                return;
            }
        }

        private void AddGroup()
        {
            var newGroup = new IngredientGroup("New Group", new List<Ingredient>());
            ListOfIngredients.Add(newGroup);
        }

        private async void RemoveGroup(IngredientGroup group)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert("Delete", "Are you sure you want to delete " + group.Key, "Yes", "No");
            if (confirm)
            { ListOfIngredients.Remove(group); }
        }

        private async void DeleteIngredient(Ingredient ingredient)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Delete", "Are you sure you want to delete " + ingredient.Name, "Yes", "No");
            if (confirm)
            {
                var group = ListOfIngredients.FirstOrDefault(g => g.Key == ingredient.IngredientSection);
                group?.Remove(ingredient);
            }
            return;
        }




    }
}
