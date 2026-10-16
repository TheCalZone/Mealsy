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
        base.OnAppearing(); if(model.EditorAccount!=AccountSession.UserId){Content=ShoppingUi.Text("Reopen the editor for your current account.");return;} if (loaded) return;
        try
        {
            var recipe = long.TryParse(RecipeId, out var id) ? await RecipeService.GetRecipe(id) : null;
            if (recipe == null || !AccountSession.Owns(recipe.FirebaseUID)) { await DisplayAlert("Recipe unavailable", "This recipe no longer exists.", "OK"); await AppNavigation.Back(); return; }
            loaded = true; await model.Initialize(recipe);
        }
        catch (Exception) { await DisplayAlert("Recipe unavailable", "The recipe couldn't be opened. Please return to the recipe book and try again.", "OK"); }
    }
    protected override async void OnDisappearing() { base.OnDisappearing(); await model.SaveDraft(); }
}
