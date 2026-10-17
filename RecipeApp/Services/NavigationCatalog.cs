namespace RecipeApp.Services;
public static class NavigationCatalog
{
    public static readonly string[] Roots={"Homepage","RecipePage","ShoppingListPage","MealPlanOverviewPage","AccountPage"};
    public static readonly string[] Details=new[]{"SettingsPage","LoginPage","SignupPage","AddRecipePage","EditRecipePage","RecipeDetailsPage","ShoppingListMakerPage","ShoppingListCreator","MealPlannerPage","EditExtraPage","EditdetailsPage","ChatPage","FoodFeedPage"}.Where(ProductFeatures.PageAvailable).ToArray();
}
