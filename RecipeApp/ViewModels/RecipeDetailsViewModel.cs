using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;
using System.Text;

namespace RecipeApp.ViewModels
{
    public class RecipeDetailsViewModel : CustomViewModelBase
    {

        private bool methodVisible = false;
        public bool MethodVisible { get => methodVisible; set => SetProperty(ref methodVisible, value); }
        public AsyncCommand EditCommand { get; set; }
        public MvvmHelpers.Commands.Command ShowMethodCommand { get; set; }
        public AsyncCommand CopyRecipeCommand { get; set; }
        public MvvmHelpers.Commands.Command EditFavouriteCommand { get; set; }

        public MvvmHelpers.Commands.Command AddRecipeToListCommand { get;private set; }
        public MvvmHelpers.Commands.Command DeleteRecipeCommand { get; private set; }
        public MvvmHelpers.Commands.Command<Ingredient> ShowNoteCommand { get; private set; }

        private List<IngredientGroup> groupedIngredients;
        public List<IngredientGroup> GroupedIngredients
        {
            get => groupedIngredients;
            set => SetProperty(ref groupedIngredients, value);
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


        private string _starImageSource = "star_Fill0.png"; // default value
        public string StarImageSource
        {
            get => _starImageSource;
            set => SetProperty(ref _starImageSource, value);
        }

        public Recipe Recipe { get; }

        public RecipeDetailsViewModel()
        {
            // This constructor is required for XAML
            EditCommand = new AsyncCommand(EditRecipe);
        }
        public RecipeDetailsViewModel(Recipe recipe)
        {
            Recipe = recipe;
            EditCommand = new AsyncCommand(EditRecipe);
            ShowMethodCommand = new MvvmHelpers.Commands.Command(() => { MethodVisible = !MethodVisible; });
            CopyRecipeCommand = new AsyncCommand(Copy);
            EditFavouriteCommand = new MvvmHelpers.Commands.Command(EditFavourite);
            AddRecipeToListCommand = new MvvmHelpers.Commands.Command(AddRecipeToManualShoppingList);
            DeleteRecipeCommand = new MvvmHelpers.Commands.Command(DeleteRecipe);
            ShowNoteCommand = new MvvmHelpers.Commands.Command<Ingredient>(ShowNote);

            GroupedIngredients = recipe.Ingredients
            .GroupBy(i => i.IngredientSection)
            .OrderBy(g => g.Key)
            .Select(g => new IngredientGroup(g.Key, g.ToList()))
            .ToList();

            UpdateStarImageSource();
        }

        private void ShowNote(Ingredient ing)
        {
            if (ing.Note != null)
            {
                Shell.Current.DisplayAlert("Note", ing.Note, "OK");
            }
        }

        private async void DeleteRecipe()
        {
            

            bool confirm = await Shell.Current.DisplayAlert("Delete Recipe", "Are you sure you want to delete this recipe?", "Yes", "No");

            if (confirm)
            {
                await RecipeService.RemoveRecipe(Recipe.Id);
                await FirestoreService.DeleteRecipe(Recipe.Id);
                await Shell.Current.Navigation.PopToRootAsync();
            }

            Toast.Make("Recipe deleted", ToastDuration.Short, 14);
            return;
        }

        public void UpdateStarImageSource()
        {
            if (Recipe.Tags != null)
            {
                if (Recipe.Tags.Contains("favourites"))
                {
                    StarImageSource = "star_Fill1.png";
                }
                else
                {
                    StarImageSource = "star_Fill0.png";
                }

            }
        }

        private async void EditFavourite()
        {
            if (Recipe?.Tags?.Contains(",favourites") == true)
            {
                Recipe.Tags = Recipe.Tags.Replace(",favourites", "");
            }
            else
            {
                Recipe.Tags = Recipe.Tags + ",favourites";
            }
            StarImageSource = Recipe?.Tags?.Contains(",favourites") == true ? "star_Fill1.png" : "star_Fill0.png";

            await RecipeService.UpdateRecipeFavouriteState(Recipe);
        }
        private async void AddRecipeToManualShoppingList()
        {
            try
            {
                await Shell.Current.Navigation.PushAsync(new ShoppingListMakerPage(new[] { new RecipeShoppingChoice(Recipe) { Selected=true } }));
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Recipe not added", "The saved shopping list could not be updated. Your previous list has been kept.", "OK");
            }
        }

        private async Task EditRecipe()
        {
            // Navigate to the EditRecipePage passing in the current recipe
            await Shell.Current.GoToAsync($"//RecipePage/EditRecipePage?RecipeId={Recipe.Id}", true);
        }

        private async Task Copy()
        {
            await Clipboard.SetTextAsync(RecipePresentation.PlainText(Recipe));

            var toast = Toast.Make("Recipe copied to clipboard", ToastDuration.Short, 14);
            await toast.Show();

        }


    }
}
