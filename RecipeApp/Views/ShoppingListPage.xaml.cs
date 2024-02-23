using MvvmHelpers;
using Newtonsoft.Json;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;
using System.Web;

namespace RecipeApp.Views
{
    [QueryProperty(nameof(IsManualList), nameof(IsManualList))]
    [QueryProperty(nameof(ShoppingList), nameof(ShoppingList))]
    [QueryProperty(nameof(IngredientsJson), nameof(IngredientsJson))]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShoppingListPage : ContentPage
    {
        private bool isManualList = false;
        public bool IsManualList
        {
            get => isManualList;
            set
            {
                isManualList = value;
                OnPropertyChanged(nameof(IsManualList));
            }
        }
        private string shoppingList;
        public string IngredientsJson { get; set; }
        public List<Ingredient> ListOfIngredients { get; set; } = new List<Ingredient>();

        public ObservableRangeCollection<IngredientGroup> CollectionIngredients { get; set; } = new ObservableRangeCollection<IngredientGroup>();

        public string FormattedShoppingList { get; set; }
        public string ShoppingList
        {
            get => shoppingList;
            set
            {
                shoppingList = value;
                OnPropertyChanged(nameof(ShoppingList));
            }
        }

        public ShoppingListPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (IsManualList)
            {
                ((ShoppingListViewModel)BindingContext).ShoppingListText = Preferences.Get("ShoppingListText", string.Empty);
                ((ShoppingListViewModel)BindingContext).ProcessPastedListCommand.Execute(null);
                ((ShoppingListViewModel)BindingContext).IsManualList = true;
            }
            else
            {

                IngredientsJson = Preferences.Get("IngredientsJson", string.Empty);

                if (!string.IsNullOrEmpty(IngredientsJson))
                {
                    ListOfIngredients = JsonConvert.DeserializeObject<List<Ingredient>>(HttpUtility.UrlDecode(IngredientsJson));

                    var groupedIngredients = ListOfIngredients
                        .GroupBy(i => i.Tag)
                        .OrderBy(g => g.Key)
                        .Select(g => new IngredientGroup(g.Key, g.ToList()))
                        .ToList();

                    CollectionIngredients = new ObservableRangeCollection<IngredientGroup>(ListOfIngredients
                        .GroupBy(i => i.Tag?.ToLower() ?? string.Empty)
                        .OrderBy(g => g.Key)
                        .Select(g => new IngredientGroup(g.Key, g.ToList()))
                        .ToList());
                }
                else
                {
                    ListOfIngredients = new List<Ingredient>();
                }



                ((ShoppingListViewModel)BindingContext).PlainIngredients = ListOfIngredients;
                ((ShoppingListViewModel)BindingContext).ListOfIngredients.AddRange(CollectionIngredients);

            }
            
            ((ShoppingListViewModel)BindingContext).CheckedText = "0" + "/" + ListOfIngredients.Count;

        }

        //private async Task RemoveRecipeIngredients()
        //{
        //    // Get the list of recipe IDs from secure storage
        //    var recipeIdsString = await SecureStorage.GetAsync("recipeIds");
        //    var recipeIds = recipeIdsString.Split(',').Select(long.Parse).ToList();
        //    var ShoppingListText = PlainTextEntry.Text;

        //    // Fetch the corresponding recipes from the RecipeService
        //    var recipes = new List<Recipe>();
        //    foreach (var id in recipeIds)
        //    {
        //        var recipe = await RecipeService.GetRecipe(id);
        //        if (recipe != null)
        //        {
        //            recipes.Add(recipe);
        //        }
        //    }

        //    // Get all the recipe ingredient lines
        //    var recipeIngredientLines = new List<string>();
        //    foreach (var recipe in recipes)
        //    {
        //        foreach (var ingredient in recipe.Ingredients)
        //        {
        //            recipeIngredientLines.Add(ingredient.Name);
        //        }
        //    }

        //    // Parse shopping list into individual lines
        //    List<string> shoppingListLines = ShoppingListText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();

        //    // Remove any lines from shoppingListLines that are also in recipeIngredientsLines
        //    shoppingListLines.RemoveAll(line => recipeIngredientLines.Contains(line));

        //    // Join the remaining lines back together into a single string
        //    ShoppingListText = string.Join(Environment.NewLine, shoppingListLines);

        //    await SecureStorage.SetAsync("shoppingListText", ShoppingListText);
        //}



    }
}