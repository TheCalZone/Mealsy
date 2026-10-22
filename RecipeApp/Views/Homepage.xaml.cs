using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class Homepage : ContentPage
{
    private ContentView householdQuick;
    private string quickAccount;
    private bool showing;
    public Homepage(){InitializeComponent();Title="Mealsy";ShoppingUi.Theme(this);ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Get started"),null,async()=>await AppNavigation.Open(new OnboardingPage())));ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Share Mealsy"),null,async()=>await OnboardingState.ShareApp()){Order=ToolbarItemOrder.Secondary});ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Settings"),null,async()=>await AppNavigation.Open(new SettingsPage())));ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Import recipe link"),null,async()=>await AppNavigation.Open(new RecipeWebsitePage())));}
    protected override async void OnAppearing()
    {
        base.OnAppearing();showing=true;HouseholdWarmup.Changed-=HouseholdChanged;HouseholdWarmup.Changed+=HouseholdChanged;var uid=AccountSession.UserId;Content=new LoadingIndicator("Getting your kitchen ready…");
        try{await RecipeService.GetRecipesForLocal();AccountSession.Check(uid);await Render(uid);}catch(Exception){Content=ShoppingUi.Text("Your local dashboard couldn't load. Open Shopping or Account from the tabs to continue.");}
    }
    protected override void OnDisappearing(){showing=false;HouseholdWarmup.Changed-=HouseholdChanged;base.OnDisappearing();}
    private void HouseholdChanged()=>MainThread.BeginInvokeOnMainThread(()=>{if(showing)RenderHouseholdQuick();});
    private void RenderHouseholdQuick()
    {
        if(householdQuick==null)return;
        if(quickAccount==null||quickAccount!=AccountSession.UserId){householdQuick.Content=null;return;}
        var view=HouseholdWarmup.Cached;
        if(view==null){householdQuick.Content=ShoppingUi.Button("What’s for dinner? · Household plan",()=>AppNavigation.Open(new HouseholdMealsPage()),true);return;}
        var body=new VerticalStackLayout{Spacing=9};
        body.Add(ShoppingUi.Text(view.Household.Name+" · Household",23,true));
        var today=SocialPolicy.DateKey(DateTime.Today);
        var meals=view.Week?.Meals.Where(meal=>meal.Date==today).OrderBy(meal=>meal.Slot switch{"breakfast"=>0,"lunch"=>1,_=>2}).ToList();
        body.Add(ShoppingUi.Text("What's for dinner?",17,true));
        if(meals==null)body.Add(ShoppingUi.Text("Checking this week's shared meals…",14));
        else if(meals.Count==0)body.Add(ShoppingUi.Text("No household meals planned for today yet.",14));
        else foreach(var meal in meals)body.Add(ShoppingUi.Text(char.ToUpper(meal.Slot[0])+meal.Slot[1..]+": "+meal.Title,15));
        if(view.ShoppingJson!=null)
        {
            try{var items=ShoppingListDocument.Deserialize(HouseholdWarmup.ShoppingDisplay(view)).Items;body.Add(ShoppingUi.Text($"{items.Count(item=>!item.InBasket)} items left to buy · {items.Count} on your household list",15,true));}
            catch(Exception){body.Add(ShoppingUi.Text("Open your household list to check its saved items.",14));}
        }
        else body.Add(ShoppingUi.Text("Checking your household shopping list…",14));
        if(view.ListChecked!=default)body.Add(ShoppingUi.Text("List last checked: "+view.ListChecked.ToLocalTime().ToString("g"),12));
        if(view.MealsChecked!=default)body.Add(ShoppingUi.Text("Meals last checked: "+view.MealsChecked.ToLocalTime().ToString("g"),12));
        body.Add(ShoppingUi.Text("Saved view · Updates quietly when you open Mealsy. Refresh for the latest changes.",12));
        body.Add(ShoppingUi.Button("Open household shopping list",()=>{AccountSession.Check(quickAccount);return AppNavigation.ShoppingList("//ShoppingListPage?OpenHouseholdId="+Uri.EscapeDataString(view.Household.HouseholdId));}));
        body.Add(ShoppingUi.Button("Open household meal plan",()=>{AccountSession.Check(quickAccount);return AppNavigation.Open(new HouseholdMealsPage());},true));
        body.Add(ShoppingUi.Button("Refresh household",async()=>await HouseholdWarmup.Start(true),true));
        householdQuick.Content=ShoppingUi.Card(body);
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
        quickAccount=account;householdQuick=new ContentView();body.Add(householdQuick);RenderHouseholdQuick();
        if(account!=null)_=HouseholdWarmup.Start();
        var summary="Your shopping list is ready when you are.";
        try{var json=Preferences.Get(ShoppingListStore.StateKey,"");if(json.Length>0){var items=ShoppingListDocument.Deserialize(json).Items;summary=$"{items.Count(i=>!i.InBasket)} items left to collect · {items.Count} on your personal list";}}catch(Exception){summary="Your saved list needs checking. Open Shopping for recovery options.";}
        body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Text(AppLocalization.Text("Your next shopping trip"),23,true),ShoppingUi.Text(summary,14),ShoppingUi.Button(AppLocalization.Text("Open personal shopping list"),()=>AppNavigation.ShoppingList("//ShoppingListPage?OpenPersonal=True")),ShoppingUi.Button(AppLocalization.Text("Quick add an item"),async()=>{
            var name=await DisplayPromptAsync("Quick add to personal list","What do you need?",maxLength:200);if(string.IsNullOrWhiteSpace(name))return;AccountSession.Check(account);ShoppingListStore.AppendIngredients(new[]{new Ingredient{Name=name.Trim()}});await DisplayAlert("Added","Added to your personal shopping list.",AppLocalization.Text("OK"));await Render(account);
        },true)}}));
        var shortcuts=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8,RowSpacing=8};
        shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("+ New recipe"),()=>AppNavigation.Open(new AddRecipePage())),0,0);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Plan a shop"),()=>AppNavigation.Open(new ShoppingListMakerPage()),true),1,0);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Recipe book"),()=>AppNavigation.Root("RecipePage"),true),0,1);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Weekly meals"),()=>AppNavigation.Root("MealPlanOverviewPage"),true),1,1);body.Add(shortcuts);
        body.Add(new PremiumBenefitsCard());
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
        body.Add(ShoppingUi.Button("Use what you have · Find a recipe",()=>AppNavigation.Open(new LeftoverRecipesPage()),true));if(ClientPlatform.HomeScreenWidgetAvailable)body.Add(ShoppingUi.Button("Home-screen shopping widget",()=>AppNavigation.Open(new WidgetHelpPage()),true));
        if(account==null)body.Add(ShoppingUi.Button(AppLocalization.Text("Sign in or create an account"),()=>AppNavigation.Root("AccountPage"),true));else body.Add(ShoppingUi.Text(AccountSession.Status,12));body.Add(new RecipeApp.Controls.AdBanner());Content=new ScrollView{Content=body};
    }
}
