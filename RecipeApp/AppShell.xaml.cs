using RecipeApp.Helpers;
using RecipeApp.Views;

namespace RecipeApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(Homepage), typeof(Homepage));

        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));

        Routing.RegisterRoute(nameof(AccountPage), typeof(AccountPage));

        Routing.RegisterRoute(nameof(RecipePage), typeof(RecipePage));

        Routing.RegisterRoute(nameof(ShoppingListMakerPage), typeof(ShoppingListMakerPage));

        Routing.RegisterRoute(nameof(AddRecipePage), typeof(AddRecipePage));

        Routing.RegisterRoute(nameof(RecipeDetailsPage), typeof(RecipeDetailsPage));

        Routing.RegisterRoute(nameof(ShoppingListPage), typeof(ShoppingListPage));

        Routing.RegisterRoute(nameof(EditRecipePage), typeof(EditRecipePage));

        Routing.RegisterRoute(nameof(MealPlannerPage), typeof(MealPlannerPage));

        Routing.RegisterRoute(nameof(MealPlanOverviewPage), typeof(MealPlanOverviewPage));

        Routing.RegisterRoute(nameof(EditExtraPage), typeof(EditExtraPage));

        Routing.RegisterRoute(nameof(ShoppingListCreator), typeof(ShoppingListCreator));

        Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));

        Routing.RegisterRoute(nameof(SignupPage), typeof(SignupPage));

        Routing.RegisterRoute(nameof(EditdetailsPage),typeof(EditdetailsPage));


    }

}
