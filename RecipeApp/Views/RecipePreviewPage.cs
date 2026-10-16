using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

public class RecipeReadView : ContentView
{
    private readonly Switch cooking = new();
    private bool previousKeepAwake, holding;
    public RecipeReadView(Recipe recipe, bool privateDetails = true)
    {
        var body = new VerticalStackLayout { Padding = 16, Spacing = 16 };
        try
        {
            if (!string.IsNullOrWhiteSpace(recipe.RecipeImage))
            {
                var bytes = Convert.FromBase64String(recipe.RecipeImage);
                body.Add(new Image { Source = ImageSource.FromStream(() => new MemoryStream(bytes)), HeightRequest = 240, Aspect = Aspect.AspectFill });
            }
        }
        catch (FormatException) { }
        body.Add(RecipeEditorView.Text(recipe.RecipeName, 30, true));
        if (!string.IsNullOrWhiteSpace(recipe.Description)) body.Add(RecipeEditorView.Text(recipe.Description, 16));
        body.Add(RecipeEditorView.Card(RecipeEditorView.Text(RecipePresentation.TimeText(recipe), 14, true)));
        if (!string.IsNullOrWhiteSpace(recipe.Tags)) body.Add(RecipeEditorView.Text(string.Join(" · ", recipe.Tags.Split(',').Where(t => !t.Equals("all", StringComparison.OrdinalIgnoreCase))), 12));
        if (privateDetails && recipe.Rating > 0) body.Add(RecipeEditorView.Text($"Your rating · {recipe.Rating}/5", 14, true));
        var cook = new HorizontalStackLayout { Spacing = 8, Children = { cooking, RecipeEditorView.Text("Keep screen awake while cooking", 13) } };
        cooking.Toggled += (_, e) => {
            if (e.Value) { previousKeepAwake = DeviceDisplay.Current.KeepScreenOn; holding = true; DeviceDisplay.Current.KeepScreenOn = true; }
            else Release();
        }; body.Add(cook);
        body.Add(RecipeEditorView.Text("Ingredients", 23, true));
        body.Add(RecipeEditorView.Text("Tick as you prepare. These checks don't change your shopping list.", 12));
        foreach (var section in recipe.Ingredients.GroupBy(i => string.IsNullOrWhiteSpace(i.IngredientSection) ? "Ingredients" : i.IngredientSection))
        {
            var group = new VerticalStackLayout { Spacing = 8 }; group.Add(RecipeEditorView.Text(section.Key, 17, true));
            foreach (var item in section)
            {
                var row = new Grid { ColumnDefinitions = new() { new(new GridLength(44)), new(GridLength.Star) } };
                row.Add(new CheckBox(), 0);
                var info = new VerticalStackLayout { Spacing = 2 }; info.Add(RecipeEditorView.Text($"{item.Quantity} {item.Name}".Trim()));
                if (!string.IsNullOrWhiteSpace(item.Note)) info.Add(RecipeEditorView.Text(item.Note, 12)); row.Add(info, 1); group.Add(row);
            }
            body.Add(RecipeEditorView.Card(group));
        }
        body.Add(RecipeEditorView.Text("Method", 23, true));
        int number = 0;
        foreach (var step in RecipeDraft.ParseSteps(recipe.RecipeMethod))
        {
            var card = new VerticalStackLayout { Spacing = 8 }; card.Add(RecipeEditorView.Text($"STEP {++number}", 12, true)); card.Add(RecipeEditorView.Text(step, 16));
            card.Add(new HorizontalStackLayout { Children = { new CheckBox(), RecipeEditorView.Text("Done", 12) } }); body.Add(RecipeEditorView.Card(card));
        }
        if (privateDetails && !string.IsNullOrWhiteSpace(recipe.Notes))
            body.Add(RecipeEditorView.Card(new VerticalStackLayout { Spacing = 8, Children = { RecipeEditorView.Text("Your private cooking notes", 17, true), RecipeEditorView.Text(recipe.Notes) } }));
        if (privateDetails && Uri.TryCreate(recipe.SourceUrl, UriKind.Absolute, out var source) && source.Scheme is "https" or "http" && string.IsNullOrEmpty(source.UserInfo))
        {
            var button = new Button { Text = "Open original source", HeightRequest = 48, CornerRadius = 12 };
            button.Clicked += async (_, _) => {
                try { await Launcher.Default.OpenAsync(source); } catch (Exception) { await Shell.Current.DisplayAlert("Source unavailable", "The original link couldn't be opened.", "OK"); }
            }; body.Add(button);
        }
        Content = new ScrollView { Content = body };
    }
    private void Release() { if (holding) { DeviceDisplay.Current.KeepScreenOn = previousKeepAwake; holding = false; } }
    public void StopCooking() { Release(); cooking.IsToggled = false; }
}

public class RecipePreviewPage : ContentPage
{
    private readonly RecipeReadView read;
    public RecipePreviewPage(RecipeDraft draft)
    {
        Title = "Preview · not saved yet";
        this.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F5F7F2"), Color.FromArgb("#111A16"));
        var recipe = RecipePresentation.Preview(draft); Content = read = new(recipe);
        ToolbarItems.Add(new ToolbarItem("Copy recipe", null, async () => {
            try { await Clipboard.Default.SetTextAsync(RecipePresentation.PlainText(recipe)); } catch (Exception) { await DisplayAlert("Copy unavailable", "The recipe couldn't be copied. Your draft is kept.", "OK"); }
        }));
    }
    protected override void OnDisappearing() { read.StopCooking(); base.OnDisappearing(); }
}
