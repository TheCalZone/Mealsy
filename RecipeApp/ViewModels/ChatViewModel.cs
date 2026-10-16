using MvvmHelpers;
using RecipeApp.Helpers;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace RecipeApp.ViewModels;

public class ChatViewModel : BaseViewModel
{
    private string result;
    private string apiKey;
    private Recipe displayRecipe;
    public string Result { get => result; set => SetProperty(ref result, value); }
    public string ApiKey { get => apiKey; set => SetProperty(ref apiKey, value); }
    public Recipe DisplayRecipe { get => displayRecipe; set => SetProperty(ref displayRecipe, value); }
    public ICommand AskDessertRecipeCommand { get; }
    public ICommand AskRecipeCommand { get; }
    public ICommand SaveRecipeCommand { get; }
    public ICommand SaveApiKeyCommand { get; }

    public ChatViewModel()
    {
        AskDessertRecipeCommand = new Command(async () => await AskRecipeAsync("dessert recipe"));
        AskRecipeCommand = new Command(async () => await AskRecipeAsync("recipe"));
        SaveRecipeCommand = new Command(async () => await SaveRecipeAsync());
        SaveApiKeyCommand = new Command(async () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ApiKey)) return;
                await SecureStorage.SetAsync("OpenAIApiKey", ApiKey.Trim());
                ApiKey = string.Empty;
                await Shell.Current.DisplayAlert("API key saved", "Your key is stored securely on this device.", "OK");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Key not saved", "Secure storage is unavailable. Please try again.", "OK");
            }
        });
    }

    private async Task AskRecipeAsync(string kind)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var user = string.IsNullOrEmpty(AuthHelper.UserId) ? null : await FirestoreService.GetUserAsync(AuthHelper.UserId);
            Result = await new OpenAIApiService().AskGpt($"Suggest a {kind} with metric measurements. Dietary restrictions: {user?.DietRestrictions}. Favourite cuisines: {user?.FavouriteCuisines}. Use exactly these headings: Recipe Name: Ingredients: Method:");
            var match = Regex.Match(Result, @"Recipe Name:\s*(.*?)\s*Ingredients:\s*(.*?)\s*Method:\s*(.+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!match.Success) throw new InvalidOperationException("The response was not a complete recipe. Please try again.");
            DisplayRecipe = new Recipe
            {
                RecipeName = match.Groups[1].Value.Trim(), RecipeMethod = match.Groups[3].Value.Trim(),
                Ingredients = match.Groups[2].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => new Ingredient { Name = line.Trim(), MainIngredient = true }).ToList(),
                Tags = "all", FirebaseUID = AuthHelper.UserId
            };
        }
        catch (Exception ex)
        {
            DisplayRecipe = null;
            await Shell.Current.DisplayAlert("Recipe unavailable", ex is InvalidOperationException ? ex.Message : "Could not generate a recipe. Check your connection and API key.", "OK");
        }
        finally { IsBusy = false; }
    }

    private async Task SaveRecipeAsync()
    {
        if (IsBusy || DisplayRecipe == null) return;
        IsBusy = true;
        try
        {
            await RecipeService.AddRecipe(DisplayRecipe);
            try { await FirestoreService.AddRecipe(DisplayRecipe); }
            catch (Exception) { await Shell.Current.DisplayAlert("Saved locally", "Cloud sync failed. Your recipe is available on this device.", "OK"); }
            DisplayRecipe = null;
            await Shell.Current.DisplayAlert("Recipe saved", "Your recipe is in the recipe book.", "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Save failed", "The recipe could not be saved. Please try again.", "OK");
        }
        finally { IsBusy = false; }
    }
}
