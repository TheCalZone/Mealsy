using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class Homepage : ContentPage
{
    public Homepage(){InitializeComponent();Title="Mealsy";ShoppingUi.Theme(this);ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Get started"),null,async()=>await AppNavigation.Open(new OnboardingPage())));ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Share Mealsy"),null,async()=>await OnboardingState.ShareApp()){Order=ToolbarItemOrder.Secondary});ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Settings"),null,async()=>await AppNavigation.Open(new SettingsPage())));ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Import recipe link"),null,async()=>await AppNavigation.Open(new RecipeWebsitePage())));}
    protected override async void OnAppearing()
    {
        base.OnAppearing();var uid=AccountSession.UserId;Content=new LoadingIndicator("Getting your kitchen ready…");
        try{await RecipeService.GetRecipesForLocal();AccountSession.Check(uid);await Render(uid);}catch(Exception){Content=ShoppingUi.Text("Your local dashboard couldn't load. Open Shopping or Account from the tabs to continue.");}
    }
    private bool recipeBusy;
    private async Task RecipeAction(long id,string account,bool options)
    {
        if(recipeBusy)return;recipeBusy=true;
        try
        {
            AccountSession.Check(account);
            var recipe=await RecipeService.GetRecipe(id);AccountSession.Check(account);
            if(recipe==null||!AccountSession.Owns(recipe.FirebaseUID))throw new InvalidOperationException("This recipe is no longer available in your account.");
            var action=options?await DisplayActionSheet(recipe.RecipeName,AppLocalization.Text("Cancel"),"Delete recipe","View recipe","Edit recipe","Shop ingredients"):"View recipe";
            AccountSession.Check(account);
            switch(action)
            {
                case "View recipe": await AppNavigation.Recipe(id);break;
                case "Edit recipe": await AppNavigation.Open(new EditRecipePage{RecipeId=id.ToString()});break;
                case "Shop ingredients": await AppNavigation.Open(new ShoppingListMakerPage(new[]{new RecipeShoppingChoice(recipe){Selected=true}}));break;
                case "Delete recipe":
                    if(!await DisplayAlert("Delete this recipe?","Remove it from your recipe book? Planned meals referring to it will need a replacement.",AppLocalization.Text("Delete"),AppLocalization.Text("Keep")))return;
                    AccountSession.Check(account);
                    if(account!=null)await FirestoreService.DeleteRecipe(recipe);
                    AccountSession.Check(account);await RecipeService.RemoveRecipe(id);
                    await RecipeService.GetRecipesForLocal();AccountSession.Check(account);await Render(account);break;
            }
        }
        catch(Exception error){await DisplayAlert("Recipe",error is InvalidOperationException?error.Message:"That action couldn't finish. Check your connection and try again. Reopen your recipe book to check its latest state.",AppLocalization.Text("OK"));}
        finally{recipeBusy=false;}
    }
    private async Task Render(string account)
    {
        var recipes=RecipeBrowser.Visible(RecipeService.Recipes,account).ToList();var body=new VerticalStackLayout{Padding=16,Spacing=18};var first=AccountSession.CachedProfile?.FirstName;
        body.Add(ShoppingUi.Text(string.IsNullOrWhiteSpace(first)?"Good food starts here":"Welcome back, "+first,29,true));body.Add(ShoppingUi.Text(AppLocalization.Text("A calmer kitchen, one meal at a time."),16));
        if(!OnboardingState.Completed||OnboardingState.PendingCode.Length>0)body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text(OnboardingState.PendingCode.Length>0?"Your household invitation is ready":"New here? Make Mealsy yours",22,true),ShoppingUi.Text("A quick guide to your account, meals and shopping. Join a household if you have a code, or get started on your own."),ShoppingUi.Button(AppLocalization.Text("Get started"),()=>AppNavigation.Open(new OnboardingPage(startAt:OnboardingState.PendingCode.Length>0?2:0)))}}));
        if(account!=null)body.Add(ShoppingUi.Button("What’s for dinner? · Household plan",()=>AppNavigation.Open(new HouseholdMealsPage()),true));
        var summary="Your shopping list is ready when you are.";
        try{var json=Preferences.Get(ShoppingListStore.StateKey,"");if(json.Length>0){var items=ShoppingListDocument.Deserialize(json).Items;summary=$"{items.Count(i=>!i.InBasket)} items left to collect · {items.Count} on your personal list";}}catch(Exception){summary="Your saved list needs checking. Open Shopping for recovery options.";}
        body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Text(AppLocalization.Text("Your next shopping trip"),23,true),ShoppingUi.Text(summary,14),ShoppingUi.Button(AppLocalization.Text("Open personal shopping list"),()=>AppNavigation.ShoppingList("//ShoppingListPage?OpenPersonal=True")),ShoppingUi.Button(AppLocalization.Text("Quick add an item"),async()=>{
            var name=await DisplayPromptAsync("Quick add to personal list","What do you need?",maxLength:200);if(string.IsNullOrWhiteSpace(name))return;AccountSession.Check(account);ShoppingListStore.AppendIngredients(new[]{new Ingredient{Name=name.Trim()}});await DisplayAlert("Added","Added to your personal shopping list.",AppLocalization.Text("OK"));await Render(account);
        },true)}}));
        var shortcuts=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8,RowSpacing=8};
        shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("+ New recipe"),()=>AppNavigation.Open(new AddRecipePage())),0,0);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Plan a shop"),()=>AppNavigation.Open(new ShoppingListMakerPage()),true),1,0);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Recipe book"),()=>AppNavigation.Root("RecipePage"),true),0,1);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Weekly meals"),()=>AppNavigation.Root("MealPlanOverviewPage"),true),1,1);body.Add(shortcuts);
        var plan=await RecipeService.GetMealPlan(account);AccountSession.Check(account);var today=plan?.Days.FirstOrDefault(d=>plan.WeekStart.AddDays(d.MealDayIndex).Date==DateTime.Today);
        var meals=new VerticalStackLayout{Spacing=9};meals.Add(ShoppingUi.Text(AppLocalization.Text("On the menu today"),23,true));var planned=false;
        if(today!=null)for(var slot=0;slot<3;slot++)
        {
            var names=MealPlanning.MealNames(today,slot,recipes).Concat(MealPlanning.Notes(today,slot)).ToList();if(names.Count==0)continue;planned=true;meals.Add(ShoppingUi.Text(MealPlanning.Slots[slot],12,true));meals.Add(ShoppingUi.Text(string.Join(" · ",names),15));
        }
        if(!planned)meals.Add(ShoppingUi.Text("Leave room for inspiration, leftovers, or something you've been meaning to cook.",14));meals.Add(ShoppingUi.Button(AppLocalization.Text("Plan your week"),()=>AppNavigation.Root("MealPlanOverviewPage"),true));body.Add(ShoppingUi.Card(meals));
        body.Add(ShoppingUi.Text(AppLocalization.Text("Back to your favourites"),23,true));body.Add(ShoppingUi.Text("Tap a recipe to open it. Hold for quick options, or use ⋯ Options.",12));foreach(var recipe in recipes.OrderByDescending(r=>RecipeBrowser.Tags(r).Contains("favourites")).ThenByDescending(r=>r.LastUpdated).Take(2))body.Add(RecipeCardView.Card(recipe,()=>RecipeAction(recipe.Id,account,false),()=>RecipeAction(recipe.Id,account,true)));
        if(recipes.Count==0)body.Add(ShoppingUi.Text("Save your first recipe to build a collection that's yours.",14));
        if(ProductFeatures.SocialEnabled)body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Text("Pull up a chair",23,true),ShoppingUi.Text("Discover what other Mealsy cooks are making, save a recipe, or share your own kitchen win.",14),ShoppingUi.Button("Visit the food community",()=>AppNavigation.Open(new FoodFeedPage()))}}));
        body.Add(ShoppingUi.Button("AI recipe ideas · Your AI service key",()=>AppNavigation.Open(new ChatPage()),true));if(ClientPlatform.HomeScreenWidgetAvailable)body.Add(ShoppingUi.Button("Home-screen shopping widget",()=>AppNavigation.Open(new WidgetHelpPage()),true));
        if(account==null)body.Add(ShoppingUi.Button(AppLocalization.Text("Sign in or create an account"),()=>AppNavigation.Root("AccountPage"),true));else body.Add(ShoppingUi.Text(AccountSession.Status,12));body.Add(new RecipeApp.Controls.AdBanner());Content=new ScrollView{Content=body};
    }
}
