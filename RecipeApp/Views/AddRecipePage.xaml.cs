using RecipeApp.ViewModels;
using RecipeApp.Services;

namespace RecipeApp.Views;
public partial class AddRecipePage : ContentPage
{
    private readonly AddRecipeViewModel model = new();
    private RecipeApp.Models.Recipe imported;
    public AddRecipePage(RecipeApp.Models.Recipe recipe, Func<RecipeApp.Models.Recipe, Task> saved = null):this(){imported=recipe;model.RecipeSaved=saved;}
    public AddRecipePage()
    {
        InitializeComponent(); BindingContext = model; Content = new RecipeEditorView(model);
        ToolbarItems.Add(new ToolbarItem("Preview", null, async () => await model.Preview()));
        ToolbarItems.Add(new ToolbarItem("Save", null, async () => await model.Save()));
    }
    protected override async void OnAppearing() { base.OnAppearing(); if(model.EditorAccount!=AccountSession.UserId){Content=ShoppingUi.Text("Reopen the editor for your current account.");return;} await model.Initialize(imported,imported==null); if(model.EditorAccount!=AccountSession.UserId)Content=ShoppingUi.Text("Your account changed. Reopen the editor."); }
    protected override async void OnDisappearing() { base.OnDisappearing(); await model.SaveDraft(); }
}
