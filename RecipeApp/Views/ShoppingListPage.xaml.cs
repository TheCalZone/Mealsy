using RecipeApp.ViewModels;

namespace RecipeApp.Views;

[QueryProperty(nameof(IsManualList), nameof(IsManualList))]
[QueryProperty(nameof(IngredientsJson), "ingredientsJson")]
public partial class ShoppingListPage : ContentPage
{
    public bool IsManualList { get; set; }
    public string IngredientsJson { get; set; }
    private ShoppingListViewModel Model => (ShoppingListViewModel)BindingContext;
    public ShoppingListPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var incoming = IngredientsJson;
        IngredientsJson = null;
        try { Model.Load(IsManualList, incoming); }
        catch (Exception) { await DisplayAlert("List unavailable", "The list could not be opened. Your previous saved list has been kept.", "OK"); }
    }
    protected override void OnDisappearing()
    {
        Model.Persist();
        base.OnDisappearing();
    }
    protected override bool OnBackButtonPressed()
    {
        if (!Model.IsEditing) return base.OnBackButtonPressed();
        Model.CancelEditCommand.Execute(null);
        return true;
    }
    private void RemoveEditingItem_Clicked(object sender, EventArgs e) => Model.RemoveEditingItem();
}
