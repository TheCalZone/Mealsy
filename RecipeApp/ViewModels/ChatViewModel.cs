using MvvmHelpers;
using MvvmHelpers.Commands;
using Plugin.Firebase.Auth;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace RecipeApp.ViewModels
{
    public class ChatViewModel : BaseViewModel
    {
        private string result;
        private AppUser appUser;
        private Recipe displayRecipe;
        private readonly OpenAIApiService openAIApiService = new();

        public AppUser CurrentAppUser { get => appUser; set => SetProperty(ref appUser, value); }
        public Recipe DisplayRecipe { get => displayRecipe; set => SetProperty(ref displayRecipe, value); }
        public string Result { get => result; set => SetProperty(ref result, value); }
        public ICommand AskDessertRecipeCommand { get; }
        public ICommand AskRecipeCommand { get; }
        public ICommand SaveRecipeCommand { get; }

        public ChatViewModel()
        {
            AskDessertRecipeCommand = new AsyncCommand(() => AskRecipe(true));
            AskRecipeCommand = new AsyncCommand(() => AskRecipe(false));
            SaveRecipeCommand = new AsyncCommand(SaveRecipe);
        }

        private async Task AskRecipe(bool dessert)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                var user = CrossFirebaseAuth.Current.CurrentUser;
                CurrentAppUser = user == null ? new AppUser() :
                    await FirestoreService.GetUserAsync(user.Uid) ?? new AppUser();
                Result = await openAIApiService.AskGpt(
                    $"I'm {CurrentAppUser.DietRestrictions} and I love {CurrentAppUser.FavouriteCuisines}. " +
                    $"Show me a {(dessert ? "dessert " : "")}recipe with metric measurements. " +
                    "Use the format: Recipe Name: Ingredients: Method:");

                string Extract(string pattern) => Regex.Match(Result ?? string.Empty, pattern,
                    RegexOptions.Singleline).Groups[1].Value.Trim();
                var name = Extract(@"Recipe Name:(.*?)Ingredients:");
                var ingredients = Extract(@"Ingredients:(.*?)Method:");
                var method = Extract(@"Method:(.*)");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ingredients) ||
                    string.IsNullOrWhiteSpace(method))
                    throw new InvalidOperationException("The recipe response was incomplete. Please try again.");

                DisplayRecipe = new Recipe
                {
                    RecipeName = name,
                    Ingredients = ingredients.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => new Ingredient { Name = line.Trim(), MainIngredient = true }).ToList(),
                    RecipeMethod = method,
                    FirebaseUID = user?.Uid,
                    LastUpdated = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Unable to generate recipe", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveRecipe()
        {
            if (DisplayRecipe == null || IsBusy)
                return;
            IsBusy = true;
            try
            {
                // SQLite assigns the ID used by the existing cloud document format.
                await RecipeService.AddRecipe(DisplayRecipe);
                await FirestoreService.AddRecipe(DisplayRecipe);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Unable to save recipe", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
