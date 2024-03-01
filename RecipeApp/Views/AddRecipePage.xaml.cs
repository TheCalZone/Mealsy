using MvvmHelpers;
using RecipeApp.Models;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddRecipePage : ContentPage
    {
        public AddRecipePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            //((AddRecipeViewModel)BindingContext).ListOfIngredients= new ObservableRangeCollection<IngredientGroup> { new IngredientGroup { new Ingredient { Name = "" } } };
        }

        //void OnEntryFocused(object sender, FocusEventArgs e)
        //{
        //    // Scroll the ScrollView to keep the focused Entry visible
        //    AddRecipeScrollView.ScrollToAsync((View)sender, ScrollToPosition.MakeVisible, true);
        //}

        void OnEntryCompleted(object sender, EventArgs e)
        {
            // Set the focus to another element (e.g., the page itself)
            this.Unfocus();
        }


    }
}