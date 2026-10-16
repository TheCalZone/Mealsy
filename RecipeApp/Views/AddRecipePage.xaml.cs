using RecipeApp.ViewModels;

namespace RecipeApp.Views;
public partial class AddRecipePage : ContentPage
{
    private readonly AddRecipeViewModel model = new();
    public AddRecipePage()
    {
        InitializeComponent(); BindingContext = model; Content = new RecipeEditorView(model);
        ToolbarItems.Add(new ToolbarItem("Preview", null, async () => await model.Preview()));
        ToolbarItems.Add(new ToolbarItem("Save", null, async () => await model.Save()));
    }
    protected override async void OnAppearing() { base.OnAppearing(); await model.Initialize(); }
    protected override async void OnDisappearing() { base.OnDisappearing(); await model.SaveDraft(); }
}
