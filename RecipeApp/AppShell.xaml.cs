using RecipeApp.Views;
using RecipeApp.Services;
namespace RecipeApp;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Navigating += (_, _) => AppNavigation.DismissKeyboard();
        // Tab roots are declared only in XAML. Registering them again creates ambiguous routes.
        var pages=new Dictionary<string,Type>{
            [nameof(SettingsPage)]=typeof(SettingsPage),
            [nameof(LoginPage)]=typeof(LoginPage),
            [nameof(SignupPage)]=typeof(SignupPage),
            [nameof(AddRecipePage)]=typeof(AddRecipePage),
            [nameof(EditRecipePage)]=typeof(EditRecipePage),
            [nameof(RecipeDetailsPage)]=typeof(RecipeDetailsPage),
            [nameof(ShoppingListMakerPage)]=typeof(ShoppingListMakerPage),
            [nameof(ShoppingListCreator)]=typeof(ShoppingListCreator),
            [nameof(MealPlannerPage)]=typeof(MealPlannerPage),
            [nameof(EditExtraPage)]=typeof(EditExtraPage),
            [nameof(EditdetailsPage)]=typeof(EditdetailsPage),
            [nameof(ChatPage)]=typeof(ChatPage),
            [nameof(FoodFeedPage)]=typeof(FoodFeedPage),
        };
        foreach(var route in NavigationCatalog.Details)Routing.RegisterRoute(route,pages[route]);
    }
}
