using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class Homepage : ContentPage
{
    private readonly ToolbarItem setupItem;
    private ContentView householdQuick;
    private string quickAccount;
    private bool showing;
    private readonly HashSet<string> expandedSections = new();
    private string sectionAccount;
    private View Foldout(string key, string title, View details, string summary = null)
    {
        var open=expandedSections.Contains(key);
        var panel = new ContentView { Content = details, IsVisible = open, IsClippedToBounds=true,
            Padding=new Thickness(0,10,0,0), InputTransparent=!open };
        var header = new Button { HorizontalOptions = LayoutOptions.Fill, MinimumHeightRequest = 52,
            FontSize = 18, FontAttributes = FontAttributes.Bold, CornerRadius = 12,
            Padding = new Thickness(12, 8), LineBreakMode = LineBreakMode.WordWrap };
        header.SetDynamicResource(Button.BackgroundColorProperty, "ThemeSubtle");
        header.SetDynamicResource(Button.TextColorProperty, "ThemeText");
        void UpdateHeader()
        {
            header.Text = title + (open ? "  ▴" : "  ▾");
            SemanticProperties.SetDescription(header, title + (open ? ", expanded" : ", collapsed"));
            SemanticProperties.SetHint(header, open ? "Tap to hide details" : "Tap to show details");
        }
        void Settle()
        {
            panel.IsVisible=open;panel.HeightRequest=-1;panel.Opacity=1;panel.InputTransparent=!open;
        }
        UpdateHeader();
        var animating=false;
        header.Clicked += async (_, _) => {
            if(animating)return;
            animating=true;
            try
            {
                var from=open?panel.Height:0;
                open=!open;
                if(open)expandedSections.Add(key);else expandedSections.Remove(key);
                UpdateHeader();
                var width=panel.Width>0?panel.Width:header.Width;
                if(!DisclosureMotionEnabled()||width<=0){Settle();return;}
                var natural=details.Measure(width,double.PositiveInfinity,MeasureFlags.IncludeMargins).Request.Height+panel.Padding.VerticalThickness;
                if(!double.IsFinite(natural)||natural<=0){Settle();return;}
                var to=open?natural:0;
                if(!double.IsFinite(from)||from<0)from=natural;
                panel.HeightRequest=from;panel.IsVisible=true;panel.InputTransparent=true;
                var completion=new TaskCompletionSource<bool>();
                new Animation(height=>panel.HeightRequest=height,from,to)
                    .Commit(panel,"HomeDisclosure",16,180,Easing.CubicOut,(_,cancelled)=>completion.TrySetResult(!cancelled));
                await completion.Task;
                Settle();
            }
            catch{Settle();}
            finally{Settle();animating=false;}
        };
        panel.Unloaded+=(_,_)=>panel.AbortAnimation("HomeDisclosure");
        var section = new VerticalStackLayout { Spacing = 0 };
        section.Add(header);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            var preview=ShoppingUi.Text(summary,14);preview.Margin=new Thickness(0,10,0,0);section.Add(preview);
        }
        section.Add(panel);
        return ShoppingUi.Card(section);
    }
    private static bool DisclosureMotionEnabled()
    {
        try
        {
#if ANDROID
            return global::Android.Provider.Settings.Global.GetFloat(global::Android.App.Application.Context.ContentResolver,"animator_duration_scale",1f)>0;
#elif IOS
            return !UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
            return true;
#endif
        }
        catch{return false;}
    }
    public Homepage(){InitializeComponent();Title="Mealsy";ShoppingUi.Theme(this);setupItem=new ToolbarItem(AppLocalization.Text("Get started"),null,async()=>await AppNavigation.Open(new OnboardingPage())){Order=ToolbarItemOrder.Secondary};if(!OnboardingState.Completed)ToolbarItems.Add(setupItem);ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Share Mealsy"),null,async()=>await OnboardingState.ShareApp()){Order=ToolbarItemOrder.Secondary});ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Settings"),null,async()=>await AppNavigation.Open(new SettingsPage())));ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Import recipe link"),null,async()=>await AppNavigation.Open(new RecipeWebsitePage())));}
    protected override async void OnAppearing()
    {
        if(OnboardingState.Completed)ToolbarItems.Remove(setupItem);
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
        if(view==null){householdQuick.Content=ShoppingUi.Button("What’s for dinner? · Household plan",()=>OpenHouseholdFeature(true,quickAccount),true);return;}
        var body=new VerticalStackLayout{Spacing=9};

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
        body.Add(ShoppingUi.Button("Open household shopping list",()=>OpenHouseholdFeature(false,quickAccount)));
        body.Add(ShoppingUi.Button("Open household meal plan",()=>OpenHouseholdFeature(true,quickAccount),true));
        if(view.Household.PermissionsVersion>=1)body.Add(ShoppingUi.Button($"Household requests · {view.Household.PendingRequests} pending",()=>AppNavigation.Open(new HouseholdRequestsPage()),true));
        body.Add(ShoppingUi.Button("Refresh household",async()=>await HouseholdWarmup.Start(true),true));
        var dinner=meals?.FirstOrDefault(meal=>meal.Slot=="dinner");
        householdQuick.Content=Foldout("household",view.Household.Name+" · Household",body,
            dinner==null?"Shared shopping & meals":"Tonight: "+dinner.Title);
    }
    private async Task OpenHouseholdFeature(bool meals, string account)
    {
        AccountSession.Check(account);
        HouseholdInfo household = null;
        if(account != null)
        {
            // Check household coverage, including members who do not pay themselves.
            // Connection errors must not be presented as an expired subscription.
            household = await HouseholdService.StatusAsync();
            AccountSession.Check(account);
            if(household.FamilyEnabled && household.HouseholdId != null)
            {
                if(meals) await AppNavigation.Open(new HouseholdMealsPage());
                else await AppNavigation.ShoppingList("//ShoppingListPage?OpenHouseholdId="+Uri.EscapeDataString(household.HouseholdId));
                return;
            }
            if(household.HouseholdId != null && household.Owner != account)
            {
                await DisplayAlert("Household sharing is paused",
                    "Your household owner's Premium plan covers everyone. Ask them to renew or restore it to use the shared shopping list and meal plan. You don't need a separate subscription.",AppLocalization.Text("OK"));
                return;
            }
            if(household.HouseholdId == null)
            {
                var plan = await PlanService.RefreshAsync();
                AccountSession.Check(account);
                if(PremiumEntitlement.IsActive(plan,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
                {
                    await AppNavigation.Open(new HouseholdPage());
                    return;
                }
            }
        }
        var benefit = meals
            ? "Plan dinner together, add portions and notes, and let everyone check what's cooking. Your personal planner stays separate."
            : "One list for your household: everyone can add what they need and tick things off at the shops. Keep your personal list separate.";
        var message = benefit + "\n\nThe household owner's Premium membership covers up to four people, including the owner. Each person uses their own account. Your personal shopping and meal planning stay free.";
        if(account == null) message += " Creating an account is free and does not start a subscription.";
        if(ClientPlatform.StorePurchasesAvailable) message += " Check subscription options for an eligible 7-day trial; renewal price and cancellation terms are shown before checkout.";
        if(await DisplayAlert(meals ? "Plan meals together with Premium" : "Share your shopping with Premium",message,"Explore Premium","Not now"))
        {
            AccountSession.Check(account);
            await AppNavigation.Open(new UpgradeFeaturesPage());
        }
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
        if(sectionAccount!=account){expandedSections.Clear();sectionAccount=account;}
        var recipes=RecipeBrowser.Visible(RecipeService.Recipes,account).ToList();var body=new VerticalStackLayout{Padding=16,Spacing=12};var first=AccountSession.CachedProfile?.FirstName;
        body.Add(ShoppingUi.Text(string.IsNullOrWhiteSpace(first)?"Good food starts here":"Welcome back, "+first,29,true));
        if(!OnboardingState.Completed||OnboardingState.PendingCode.Length>0)body.Add(Foldout("welcome",OnboardingState.PendingCode.Length>0?"Your invitation":"Get started",new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text("A quick guide to your account, meals and shopping. Join a household if you have a code, or get started on your own."),ShoppingUi.Button(AppLocalization.Text("Get started"),()=>AppNavigation.Open(new OnboardingPage(startAt:OnboardingState.PendingCode.Length>0?2:0)))}}));
        quickAccount=account;householdQuick=new ContentView();body.Add(householdQuick);RenderHouseholdQuick();
        if(account!=null)_=HouseholdWarmup.Start();
        var summary="Your shopping list is ready when you are.";
        try{var json=Preferences.Get(ShoppingListStore.StateKey,"");if(json.Length>0){var items=ShoppingListDocument.Deserialize(json).Items;summary=$"{items.Count(i=>!i.InBasket)} items left to buy";}}catch(Exception){summary="Your saved list needs checking. Open Shopping for recovery options.";}
        body.Add(Foldout("shopping",AppLocalization.Text("Your next shopping trip"),new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Button(AppLocalization.Text("Open personal shopping list"),()=>AppNavigation.ShoppingList("//ShoppingListPage?OpenPersonal=True")),ShoppingUi.Button(AppLocalization.Text("Quick add an item"),async()=>{
            var name=await DisplayPromptAsync("Quick add to personal list","What do you need?",maxLength:200);if(string.IsNullOrWhiteSpace(name))return;AccountSession.Check(account);ShoppingListStore.AppendIngredients(new[]{new Ingredient{Name=name.Trim()}});await DisplayAlert("Added","Added to your personal shopping list.",AppLocalization.Text("OK"));await Render(account);
        },true),ShoppingUi.Button("Show household shopping list",()=>OpenHouseholdFeature(false,account),true)}},summary));
        var shortcuts=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8,RowSpacing=8};
        shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("+ New recipe"),()=>AppNavigation.Open(new AddRecipePage())),0,0);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Plan a shop"),()=>AppNavigation.Open(new ShoppingListMakerPage()),true),1,0);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Recipe book"),()=>AppNavigation.Root("RecipePage"),true),0,1);shortcuts.Add(ShoppingUi.Button(AppLocalization.Text("Weekly meals"),()=>AppNavigation.Root("MealPlanOverviewPage"),true),1,1);body.Insert(1,shortcuts);

        var plan=await RecipeService.GetMealPlan(account);AccountSession.Check(account);var today=plan?.Days.FirstOrDefault(d=>plan.WeekStart.AddDays(d.MealDayIndex).Date==DateTime.Today);
        var meals=new VerticalStackLayout{Spacing=9};var planned=false;
        if(today!=null)for(var slot=0;slot<3;slot++)
        {
            var names=MealPlanning.MealNames(today,slot,recipes).Concat(MealPlanning.Notes(today,slot)).ToList();if(names.Count==0)continue;planned=true;meals.Add(ShoppingUi.Text(MealPlanning.Slots[slot],12,true));meals.Add(ShoppingUi.Text(string.Join(" · ",names),15));
        }
        if(!planned)meals.Add(ShoppingUi.Text("Leave room for inspiration, leftovers, or something you've been meaning to cook.",14));meals.Add(ShoppingUi.Button(AppLocalization.Text("Plan your week"),()=>AppNavigation.Root("MealPlanOverviewPage"),true));meals.Add(ShoppingUi.Button("Show household meal plan",()=>OpenHouseholdFeature(true,account),true));body.Add(Foldout("meals",AppLocalization.Text("On the menu today"),meals,planned?"Your meals are planned for today":"Nothing planned yet"));
        var favourites=new VerticalStackLayout{Spacing=10};foreach(var recipe in recipes.OrderByDescending(r=>RecipeBrowser.Tags(r).Contains("favourites")).ThenByDescending(r=>r.LastUpdated).Take(2))favourites.Add(RecipeCardView.Card(recipe,()=>RecipeAction(recipe.Id,account,false),()=>RecipeAction(recipe.Id,account,true)));
        if(recipes.Count==0)favourites.Add(ShoppingUi.Text("Save your first recipe to build your collection.",14));
        body.Add(Foldout("recipes",AppLocalization.Text("Back to your favourites"),favourites));
        if(ProductFeatures.SocialEnabled)body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=10,Children={ShoppingUi.Text("Pull up a chair",23,true),ShoppingUi.Text("Discover what other Mealsy cooks are making, save a recipe, or share your own kitchen win.",14),ShoppingUi.Button("Visit the food community",()=>AppNavigation.Open(new FoodFeedPage()))}}));
        var tools=new VerticalStackLayout{Spacing=8};tools.Add(ShoppingUi.Button("Use what you have · Find a recipe",()=>AppNavigation.Open(new LeftoverRecipesPage()),true));if(ClientPlatform.HomeScreenWidgetAvailable)tools.Add(ShoppingUi.Button("Home-screen shopping widget",()=>AppNavigation.Open(new WidgetHelpPage()),true));
        body.Add(Foldout("tools","Kitchen tools",tools));
        body.Add(Foldout("premium","Explore Premium",new VerticalStackLayout{Spacing=8,Children={
            ShoppingUi.Text("More personal shopping lists, recipe-link imports, reusable meal plans and household sharing.",14),
            ShoppingUi.Button("Plans & benefits",()=>AppNavigation.Open(new UpgradeFeaturesPage()),true)}}));
        if(account==null)body.Add(ShoppingUi.Button(AppLocalization.Text("Sign in or create an account"),()=>AppNavigation.Root("AccountPage"),true));body.Add(new RecipeApp.Controls.AdBanner());Content=new ScrollView{Content=body};
    }
}
