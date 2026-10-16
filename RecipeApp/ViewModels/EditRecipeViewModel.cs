using MvvmHelpers.Commands;
using RecipeApp.Interfaces;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Collections.ObjectModel;

namespace RecipeApp.ViewModels
{
    public class EditRecipeViewModel : CustomViewModelBase
    {
        public int RecipeId { get; set; }
        private ImageSource recipeImage;
        public ImageSource RecipeImage
        {
            get
            {
                return recipeImage;
            }
            set
            {
                SetProperty(ref recipeImage, value);
            }
        }
        public Recipe Recipe { get; }
        public ObservableCollection<Ingredient> Ingredients { get => ingredients; set => SetProperty(ref ingredients, value); }

        public AsyncCommand SaveCommand { get; }
        public MvvmHelpers.Commands.Command AddIngredientCommand { get; }
        public AsyncCommand<Ingredient> DeleteIngredientCommand { get; }
        public MvvmHelpers.Commands.Command<Ingredient> AddNoteCommand { get; }
        public AsyncCommand UploadImageCommand { get; }
        public MvvmHelpers.Commands.Command ClearImageCommand { get; }

        private ObservableCollection<Ingredient> ingredients;

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

        public EditRecipeViewModel()
        {
            // This constructor is required for XAML
        }
        public EditRecipeViewModel(Recipe recipe)
        {
            Recipe = recipe;
            RecipeImage = new RecipeApp.Converter.Base64ToImageSourceConverter().Convert(recipe.RecipeImage, typeof(ImageSource), null, System.Globalization.CultureInfo.InvariantCulture) as ImageSource;
            UploadImageCommand = new AsyncCommand(UploadImage);
            ClearImageCommand = new MvvmHelpers.Commands.Command(ClearImage);
            Ingredients = new ObservableCollection<Ingredient>(Recipe.Ingredients);
            SaveCommand = new AsyncCommand(SaveRecipe);
            AddIngredientCommand = new MvvmHelpers.Commands.Command(AddIngredient);
            DeleteIngredientCommand = new AsyncCommand<Ingredient>(DeleteIngredient);
            AddNoteCommand = new MvvmHelpers.Commands.Command<Ingredient>(AddNote);
            foreach (Ingredient ingredient in Ingredients)
            {
                ingredient.OnNameChanged += Ingredient_OnNameChanged;
            }
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

        private void ClearImage()
        {
            RecipeImage = null;
            Recipe.RecipeImage = null;
        }

        async Task DeleteIngredient(Ingredient ingredient)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Delete", "Are you sure you want to delete " + ingredient.Name, "Yes", "No");
            if (confirm)
            {
                // Check that the Ingredients collection and Recipe object are not null before updating them
                if (Ingredients != null && Recipe != null)
                {
                    Ingredients.Remove(ingredient);
                    Recipe.Ingredients = Ingredients.ToList();
                }
            }
            return;
        }

        private void AddIngredient()
        {
            var ingredient = new Ingredient();
            ingredient.OnNameChanged += Ingredient_OnNameChanged;
            Ingredients.Add(ingredient);
        }

        private async Task SaveRecipe()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (string.IsNullOrWhiteSpace(Recipe.RecipeName))
                {
                    await Shell.Current.DisplayAlert("Recipe name required", "Please enter a recipe name.", "OK");
                    return;
                }
                // Update the recipe and its ingredients in the database
                await RecipeService.UpdateRecipeAndIngredientsAsync(Recipe, Ingredients.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList());
                try { await FirestoreService.UpdateRecipe(Recipe); }
                catch (Exception) { await Shell.Current.DisplayAlert("Saved locally", "Cloud sync failed. Your changes are saved on this device.", "OK"); }

                if (Shell.Current.Navigation != null)
                {
                    await Shell.Current.Navigation.PopAsync();
                }



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
                using var stream = await result.OpenReadAsync();

                // Read the stream into a byte array
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                // Save the byte array to the recipe object
                Recipe.RecipeImage = Convert.ToBase64String(bytes);
                RecipeImage = ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(Recipe.RecipeImage)));
            }
        }
    }
}
