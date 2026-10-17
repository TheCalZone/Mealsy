using System.Text.Json;
namespace RecipeApp.Services;

public static class IngredientSuggestionStore
{
    private static string HistoryKey(string account)=>"Shopping.ingredientSuggestions.v1"+(account==null?".device":".user."+account);
    public static Dictionary<string,int> History(string account)=>account==AccountSession.UserId?IngredientSuggestions.ReadHistory(Preferences.Get(HistoryKey(account),"null")):new();
    public static void Remember(string account,string name)
    {
        if(account!=AccountSession.UserId)return;
        try{Preferences.Set(HistoryKey(account),JsonSerializer.Serialize(IngredientSuggestions.Remember(History(account),name)));}catch{/* Suggestions must never prevent adding a shopping item. */}
    }
    public static async Task<List<string>> LoadNames(string account)
    {
        await RecipeService.GetRecipesForLocal();await RecipeService.GetSnacksForLocal();await RecipeService.GetStaplesForLocal();AccountSession.Check(account);
        var sources=RecipeService.Recipes.SelectMany(recipe=>(recipe.Ingredients??new()).Select(item=>new IngredientSuggestions.Source(item.Name,recipe.FirebaseUID)))
            .Concat(RecipeService.Snacks.Select(item=>new IngredientSuggestions.Source(item.Name,item.FirebaseUID)))
            .Concat(RecipeService.Staples.Select(item=>new IngredientSuggestions.Source(item.Name,item.FirebaseUID)));
        return IngredientSuggestions.ForAccount(sources,account);
    }
}
