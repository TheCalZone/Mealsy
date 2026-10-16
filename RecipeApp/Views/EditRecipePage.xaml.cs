using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views;
[QueryProperty(nameof(RecipeId), nameof(RecipeId))]
public partial class EditRecipePage : ContentPage
{
    public string RecipeId { get; set; }
    private readonly EditRecipeViewModel model = new();
    private bool loaded;
    public EditRecipePage()
    {
        InitializeComponent(); BindingContext = model; Content = new RecipeEditorView(model);
        ToolbarItems.Add(new ToolbarItem("Preview", null, async () => await model.Preview()));
        ToolbarItems.Add(new ToolbarItem("Save", null, async () => await model.Save()));
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (loaded) return;
        try
        {
            var recipe = long.TryParse(RecipeId, out var id) ? await RecipeService.GetRecipe(id) : null;
            if (recipe == null) { await DisplayAlert("Recipe unavailable", "This recipe no longer exists.", "OK"); await Shell.Current.GoToAsync(".."); return; }
            loaded = true; await model.Initialize(recipe);
        }
        catch (Exception) { await DisplayAlert("Recipe unavailable", "The recipe couldn't be opened. Please return to the recipe book and try again.", "OK"); }
    }
    protected override async void OnDisappearing() { base.OnDisappearing(); await model.SaveDraft(); }
}
