using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [QueryProperty(nameof(RecipeId), nameof(RecipeId))]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditRecipePage : ContentPage
    {

        public string RecipeId { get; set; }

        public EditRecipePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            int.TryParse(RecipeId, out var result);

            var recipe = await RecipeService.GetRecipe(result);
            BindingContext = new EditRecipeViewModel(recipe);
        }
    }
}