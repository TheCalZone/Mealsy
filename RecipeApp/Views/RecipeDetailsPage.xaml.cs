using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views;
[QueryProperty(nameof(RecipeId), nameof(RecipeId))]
public partial class RecipeDetailsPage : ContentPage
{
    public string RecipeId { get; set; }
    public Recipe Recipe { get; set; }
    private RecipeReadView read;
    private RecipeDetailsViewModel model;
    public RecipeDetailsPage()
    {
        InitializeComponent();
        ToolbarItems.Add(new ToolbarItem("Edit", null, async () => { if (model != null) await model.EditCommand.ExecuteAsync(); }));
        ToolbarItems.Add(new ToolbarItem("Share to food feed", null, async () => { if (Recipe != null) await Navigation.PushAsync(new FeedComposerPage(Recipe)); }) { Order = ToolbarItemOrder.Secondary });
        ToolbarItems.Add(new ToolbarItem("Copy recipe", null, async () => {
            if (Recipe == null) return;
            try { await Clipboard.Default.SetTextAsync(RecipePresentation.PlainText(Recipe)); } catch (Exception) { await DisplayAlert("Copy unavailable", "Please try copying again.", "OK"); }
        }) { Order = ToolbarItemOrder.Secondary });
        ToolbarItems.Add(new ToolbarItem("Add to shopping list", null, () => model?.AddRecipeToListCommand.Execute(null)) { Order = ToolbarItemOrder.Secondary });
        ToolbarItems.Add(new ToolbarItem("Favourite / unfavourite", null, () => model?.EditFavouriteCommand.Execute(null)) { Order = ToolbarItemOrder.Secondary });
        ToolbarItems.Add(new ToolbarItem("Delete recipe", null, () => model?.DeleteRecipeCommand.Execute(null)) { Order = ToolbarItemOrder.Secondary });
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            Recipe = long.TryParse(RecipeId, out var id) ? await RecipeService.GetRecipe(id) : null;
            if (Recipe == null) { await DisplayAlert("Recipe unavailable", "This recipe no longer exists.", "OK"); await Shell.Current.GoToAsync(".."); return; }
            model = new(Recipe); BindingContext = model; Title = Recipe.RecipeName; Content = read = new(Recipe);
        }
        catch (Exception) { await DisplayAlert("Recipe unavailable", "The recipe couldn't be opened. Please try again.", "OK"); }
    }
    protected override void OnDisappearing() { read?.StopCooking(); base.OnDisappearing(); }
}
