using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class FeedPreferencesPage : ContentPage
{
    private readonly string account = AccountSession.UserId;
    public FeedPreferencesPage(FeedPreferences preferences)
    {
        Title = "Your food interests"; ShoppingUi.Theme(this);
        var selected = preferences.Interests.ToHashSet();
        var body = new VerticalStackLayout { Padding = 20, Spacing = 12 };
        body.Add(ShoppingUi.Text("What looks good to you?", 27, true));
        body.Add(ShoppingUi.Text("Choose the food you enjoy. Your For you feed also learns from posts you like and cooks you follow. You can change these choices any time."));
        body.Add(ShoppingUi.Text("These are interests, not allergy or dietary guarantees. Always check ingredients.", 13));
        body.Add(ShoppingUi.Text("Ingredients to hide",18,true));
        var excluded=ShoppingUi.Entry(string.Join(", ",preferences.ExcludedIngredients??new()),"e.g. pork, prawns · separated by commas",_=>{},2000);body.Add(excluded);
        body.Add(ShoppingUi.Text("Hides posts whose ingredient names contain these words. This does not detect all substitutions or allergens; always check the recipe.",13));
        foreach (var option in preferences.Options)
            body.Add(new HorizontalStackLayout { Spacing = 10, Children = {
                ShoppingUi.Check(selected.Contains(option), value => { if (value) selected.Add(option); else selected.Remove(option); }),
                ShoppingUi.Text(option, 16) } });
        async Task Save(IEnumerable<string> interests)
        {
            AccountSession.Check(account);
            await FoodFeedService.Call<object>(new { action = "savePreferences", interests = interests.ToArray(),excludedIngredients=(excluded.Text??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries) });
            AccountSession.Check(account); await Navigation.PopAsync();
        }
        body.Add(FoodFeedPage.Button("Save my interests", () => Save(selected)));
        body.Add(FoodFeedPage.Button("Clear interests · keep ingredient exclusions", () => Save(Array.Empty<string>())));
        body.Add(ShoppingUi.Text("Cooks you follow", 22, true));
        if (preferences.Following.Count == 0) body.Add(ShoppingUi.Text("Tap Follow on a community post to start following a cook.", 14));
        foreach (var cook in preferences.Following)
        {
            var row = new VerticalStackLayout { Spacing = 4 };
            row.Add(ShoppingUi.Text(cook.Name, 17, true));
            row.Add(FoodFeedPage.Button("Unfollow", async () => {
                AccountSession.Check(account);
                await FoodFeedService.Call<object>(new { action = "unfollow", authorId = cook.Id });
                AccountSession.Check(account); row.IsVisible = false;
            }));
            body.Add(ShoppingUi.Card(row));
        }
        Content = new ScrollView { Content = body };
    }
}
