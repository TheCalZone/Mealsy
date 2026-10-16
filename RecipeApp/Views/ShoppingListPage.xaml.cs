using MvvmHelpers;
using Newtonsoft.Json;
using RecipeApp.Models;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [QueryProperty(nameof(IsManualList), nameof(IsManualList))]
    [QueryProperty(nameof(ShoppingList), nameof(ShoppingList))]
    [QueryProperty(nameof(IngredientsJson), nameof(IngredientsJson))]
    public partial class ShoppingListPage : ContentPage
    {
        public bool IsManualList { get; set; }
        public string ShoppingList { get; set; }
        public string IngredientsJson { get; set; }
        private bool loaded;

        public ShoppingListPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Reappearing after an alert or another page must not duplicate or reset the list.
            if (loaded)
                return;

            var viewModel = (ShoppingListViewModel)BindingContext;
            viewModel.IsManualList = IsManualList;
            if (IsManualList)
            {
                viewModel.ShoppingListText = ShoppingList ?? Preferences.Get("ShoppingListText", string.Empty);
                viewModel.ProcessPastedListCommand.Execute(null);
            }
            else
            {
                // Shell decodes QueryProperty values; stored JSON must not be URL-decoded again.
                var json = IngredientsJson ?? Preferences.Get("IngredientsJson", string.Empty);
                List<Ingredient> ingredients;
                try
                {
                    ingredients = string.IsNullOrWhiteSpace(json) ? new List<Ingredient>() :
                        JsonConvert.DeserializeObject<List<Ingredient>>(json) ?? new List<Ingredient>();
                }
                catch (JsonException)
                {
                    ingredients = new List<Ingredient>();
                }
                viewModel.PlainIngredients = ingredients;
                viewModel.ListOfIngredients = new ObservableRangeCollection<IngredientGroup>(
                    ingredients.GroupBy(item => item.Tag?.ToLowerInvariant() ?? string.Empty)
                        .OrderBy(group => group.Key)
                        .Select(group => new IngredientGroup(group.Key, group.ToList())));
            }
            viewModel.CheckBoxChangedCommand.Execute(null);
            loaded = true;
        }
    }
}
