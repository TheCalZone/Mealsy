using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class Homepage : ContentPage
{
    public Homepage(){InitializeComponent();Title="Mealsy";ShoppingUi.Theme(this);ToolbarItems.Add(new ToolbarItem("Settings",null,async()=>await AppNavigation.Open(new SettingsPage())));}
    protected override async void OnAppearing()
    {
        base.OnAppearing();var uid=AccountSession.UserId;Content=ShoppingUi.Text("Getting your kitchen ready…");
        try{await RecipeService.GetRecipesForLocal();AccountSession.Check(uid);await Render(uid);}catch(Exception){Content=ShoppingUi.Text("Your local dashboard couldn't load. Open Shopping or Account from the tabs to continue.");}
    }
    private async Task Render(string account)
    {
        var recipes=RecipeBrowser.Visible(RecipeService.Recipes,account).ToList();var body=new VerticalStackLayout{Padding=16,Spacing=18};var first=AccountSession.CachedProfile?.FirstName;
        body.Add(ShoppingUi.Text(string.IsNullOrWhiteSpace(first)?"Good food starts here":"Welcome back, "+first,29,true));body.Add(ShoppingUi.Text("A calmer kitchen, one meal at a time.",16));
        var summary="Your shopping list is ready when you are.";
        try{var json=Preferences.Get(ShoppingListStore.StateKey,"");if(json.Length>0){var items=ShoppingListDocument.Deserialize(json).Items;summary=$"{items.Count(i=>!i.InBasket)} items left to collect · {items.Count} on your personal list";}}catch(Exception){summary="Your saved list needs checking. Open Shopping for recovery options.";}
        body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Text("Your next shopping trip",23,true),ShoppingUi.Text(summary,14),ShoppingUi.Button("Open personal shopping list",()=>Shell.Current.GoToAsync("//ShoppingListPage?OpenPersonal=True")),ShoppingUi.Button("Quick add an item",async()=>{
            var name=await DisplayPromptAsync("Quick add to personal list","What do you need?",maxLength:200);if(string.IsNullOrWhiteSpace(name))return;AccountSession.Check(account);ShoppingListStore.AppendIngredients(new[]{new Ingredient{Name=name.Trim()}});await DisplayAlert("Added","Added to your personal shopping list.","OK");await Render(account);
        },true)}}));
        var shortcuts=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8,RowSpacing=8};
        shortcuts.Add(ShoppingUi.Button("+ New recipe",()=>AppNavigation.Open(new AddRecipePage())),0,0);shortcuts.Add(ShoppingUi.Button("Plan a shop",()=>AppNavigation.Open(new ShoppingListMakerPage()),true),1,0);shortcuts.Add(ShoppingUi.Button("Recipe book",()=>AppNavigation.Root("RecipePage"),true),0,1);shortcuts.Add(ShoppingUi.Button("Weekly meals",()=>AppNavigation.Root("MealPlanOverviewPage"),true),1,1);body.Add(shortcuts);
        var plan=await RecipeService.GetMealPlan(account);AccountSession.Check(account);var today=plan?.Days.FirstOrDefault(d=>plan.WeekStart.AddDays(d.MealDayIndex).Date==DateTime.Today);
        var meals=new VerticalStackLayout{Spacing=9};meals.Add(ShoppingUi.Text("On the menu today",23,true));var planned=false;
        if(today!=null)for(var slot=0;slot<3;slot++)
        {
            var names=MealPlanning.Ids(today,slot).Select(id=>recipes.FirstOrDefault(r=>r.Id==id)?.RecipeName??"Recipe unavailable").Concat(MealPlanning.Notes(today,slot)).ToList();if(names.Count==0)continue;planned=true;meals.Add(ShoppingUi.Text(MealPlanning.Slots[slot],12,true));meals.Add(ShoppingUi.Text(string.Join(" · ",names),15));
        }
        if(!planned)meals.Add(ShoppingUi.Text("Leave room for inspiration, leftovers, or something you've been meaning to cook.",14));meals.Add(ShoppingUi.Button("Plan your week",()=>AppNavigation.Root("MealPlanOverviewPage"),true));body.Add(ShoppingUi.Card(meals));
        body.Add(ShoppingUi.Text("Back to your favourites",23,true));foreach(var recipe in recipes.OrderByDescending(r=>RecipeBrowser.Tags(r).Contains("favourites")).ThenByDescending(r=>r.LastUpdated).Take(2))body.Add(RecipeCardView.Card(recipe,()=>AppNavigation.Recipe(recipe.Id)));
        if(recipes.Count==0)body.Add(ShoppingUi.Text("Save your first recipe to build a collection that's yours.",14));
        body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Text("Pull up a chair",23,true),ShoppingUi.Text("Discover what other Mealsy cooks are making, save a recipe, or share your own kitchen win.",14),ShoppingUi.Button("Visit the food community",()=>AppNavigation.Open(new FoodFeedPage()))}}));
        body.Add(ShoppingUi.Button("AI recipe ideas · Your OpenAI key",()=>AppNavigation.Open(new ChatPage()),true));body.Add(ShoppingUi.Button("Home-screen shopping widget",()=>AppNavigation.Open(new WidgetHelpPage()),true));
        if(account==null)body.Add(ShoppingUi.Button("Sign in or create an account",()=>AppNavigation.Root("AccountPage"),true));else body.Add(ShoppingUi.Text(AccountSession.Status,12));body.Add(new RecipeApp.Controls.AdBanner());Content=new ScrollView{Content=body};
    }
}
