using MvvmHelpers;
using Plugin.Firebase.Auth;
using RecipeApp.Helpers;
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
        public AppUser CurrentAppUser { get => appUser; set => SetProperty(ref appUser, value); }

        public Recipe DisplayRecipe { get => displayRecipe; set => SetProperty(ref displayRecipe, value); }

        public string Result { get => result; set => SetProperty(ref result, value); }

        private OpenAIApiService openAIApiService;
        private Recipe displayRecipe;

        public ICommand AskDessertRecipeCommand { get; }

        public ICommand AskRecipeCommand { get; }

        public ICommand SaveRecipeCommand { get; }

        public ChatViewModel()
        {
            openAIApiService = new OpenAIApiService();
            AskDessertRecipeCommand = new Command(AskDessertRecipe);
            AskRecipeCommand = new Command(AskRecipe);
            SaveRecipeCommand = new Command(SaveRecipe);
            LoadUserDataAsync();
        }

        private async void AskDessertRecipe()
        {
            IsBusy = true;
            LoadUserDataAsync();

           Result = await openAIApiService.AskGpt($"I'm {CurrentAppUser.DietRestrictions} and I love {CurrentAppUser.FavouriteCuisines} Can you show me a dessert recipe with metric measurements? In the format of: Recipe Name: Ingredients: Method:");

            Regex recipeNameRegex = new Regex(@"Recipe Name:(.*?)Ingredients:", RegexOptions.Singleline);
            string recipeName = recipeNameRegex.Match(Result).Groups[1].Value.Trim();

            Regex ingredientsRegex = new Regex(@"Ingredients:(.*?)Method:", RegexOptions.Singleline);
            string ingredients = ingredientsRegex.Match(Result).Groups[1].Value.Trim();

            Regex methodRegex = new Regex(@"Method:(.*)", RegexOptions.Singleline);
            string method = methodRegex.Match(Result).Groups[1].Value.Trim();


            string[] ingredientsArray = ingredients.Split('\n');

            List<Ingredient> ingredientsList = new List<Ingredient>();
            foreach (string ingredient in ingredientsArray)
            {
                ingredientsList.Add(new Ingredient { Name = ingredient.Trim() });
            }

            DisplayRecipe = new Recipe { RecipeName = recipeName, Ingredients = ingredientsList, RecipeMethod = method };

            IsBusy = false;
        }

        private async void AskRecipe()
        {
            IsBusy = true;

            LoadUserDataAsync();

            Result = await openAIApiService.AskGpt($"I'm {CurrentAppUser.DietRestrictions} and I love {CurrentAppUser.FavouriteCuisines} Can you show me a recipe with metric measurements? In the format of: Recipe Name: Ingredients: Method:");

            Regex recipeNameRegex = new Regex(@"Recipe Name:(.*?)Ingredients:", RegexOptions.Singleline);
            string recipeName = recipeNameRegex.Match(Result).Groups[1].Value.Trim();

            Regex ingredientsRegex = new Regex(@"Ingredients:(.*?)Method:", RegexOptions.Singleline);
            string ingredients = ingredientsRegex.Match(Result).Groups[1].Value.Trim();

            Regex methodRegex = new Regex(@"Method:(.*)", RegexOptions.Singleline);
            string method = methodRegex.Match(Result).Groups[1].Value.Trim();

            string[] ingredientsArray = ingredients.Split('\n');

            List<Ingredient> ingredientsList = new List<Ingredient>();
            foreach (string ingredient in ingredientsArray)
            {
                ingredientsList.Add(new Ingredient { Name = ingredient.Trim() });
            }

            DisplayRecipe = new Recipe { RecipeName = recipeName, Ingredients = ingredientsList, RecipeMethod = method };

            IsBusy = false;
        }

        public async void LoadUserDataAsync()
        {
            CurrentAppUser = await FirestoreService.GetUserAsync(CrossFirebaseAuth.Current.CurrentUser.Uid);
        }

        public void SaveRecipe()
        {
            SaveRecipeToFireStore();
            SaveRecipeToLocalDatabase();
        }

        public async void SaveRecipeToFireStore()
        {
            await FirestoreService.AddRecipe(DisplayRecipe);
        }

        public async void SaveRecipeToLocalDatabase()
        {
            await RecipeService.AddRecipe(DisplayRecipe);
        }

        // add other commands here...
    }

}
