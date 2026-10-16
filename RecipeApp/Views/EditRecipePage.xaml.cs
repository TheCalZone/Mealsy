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
            if (!long.TryParse(RecipeId, out var result)) return;

            var recipe = await RecipeService.GetRecipe(result);
            if (recipe == null)
            {
                await DisplayAlert("Recipe unavailable", "This recipe no longer exists.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            BindingContext = new EditRecipeViewModel(recipe);
        }
    }
}