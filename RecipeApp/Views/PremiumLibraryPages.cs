using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class SavedWeeksPage:ContentPage
{
    private readonly string account=AccountSession.UserId;
    private bool busy;
    public SavedWeeksPage(){Title="Weekly templates · Premium";ShoppingUi.Theme(this);}
    protected override async void OnAppearing(){base.OnAppearing();if(account!=AccountSession.UserId)Content=ShoppingUi.Text("Your account changed. Reopen this library from your account.");await Run(Render);}
    private async Task Run(Func<Task> action){if(busy)return;busy=true;try{AccountSession.Check(account);await action();}catch(Exception e){if(account!=AccountSession.UserId)Content=ShoppingUi.Text("Your account changed. Reopen this library.");await DisplayAlert("Weekly templates",e is InvalidOperationException?e.Message:"Your templates couldn't be updated. Your current plan has been kept.",AppLocalization.Text("OK"));}finally{busy=false;}}
    private async Task Render()
    {
        var entries=await PremiumLibrary.Load<SavedWeekTemplate>(account,"weeks");AccountSession.Check(account);
        var body=new VerticalStackLayout{Padding=16,Spacing=14};body.Add(ShoppingUi.Text("Your favourite weeks, ready again",26,true));body.Add(ShoppingUi.Text("Save this week's meals, portions and notes as a reusable template. Each template is private to your account on this device; recipes must remain in your recipe book.",14));
        body.Add(ShoppingUi.Button("Save current week",()=>Run(async()=>{
            if(!await PremiumAccess.Require("Reusable weekly meal-plan templates"))return;AccountSession.Check(account);
            var plan=await RecipeService.GetMealPlan(account);if(plan==null)throw new InvalidOperationException("Open the meal planner and create a week first.");
            var name=await DisplayPromptAsync("Name this week","e.g. Quick work nights or My favourites",maxLength:80);if(name==null)return;AccountSession.Check(account);
            await PremiumLibrary.Add(account,"weeks",SavedWeekTemplate.Create(name,plan.Days));await Render();
        })));
        if(entries.Count==0)body.Add(ShoppingUi.Text("No saved weeks yet. Plan some meals, then save your first template."));
        foreach(var entry in entries)
        {
            var card=new VerticalStackLayout{Spacing=8};card.Add(ShoppingUi.Text(entry.Name,21,true));card.Add(ShoppingUi.Text("Saved "+entry.SavedAt.LocalDateTime.ToString("d"),12));
            card.Add(ShoppingUi.Button("Use this week…",()=>Run(async()=>{
                if(!await PremiumAccess.Require("Reusable weekly meal-plan templates"))return;AccountSession.Check(account);
                await RecipeService.GetRecipesForLocal();var plan=await RecipeService.GetMealPlan(account);AccountSession.Check(account);if(plan==null)throw new InvalidOperationException("Open the meal planner first.");
                var copies=entry.Apply(plan.Days,RecipeBrowser.Visible(RecipeService.Recipes,account).Select(r=>r.Id));
                if(!await DisplayAlert("Replace this week's meals?",entry.Name+" will replace all seven days' meals, notes and portions. The week date and your saved template stay unchanged.","Use template","Keep current week"))return;
                AccountSession.Check(account);await RecipeService.SaveMealPlanDays(copies,plan.Id);await DisplayAlert("Week ready","Your template is now in your personal meal plan.",AppLocalization.Text("OK"));await AppNavigation.Back();
            })));
            card.Add(ShoppingUi.Button("Delete template…",()=>Run(async()=>{if(!await DisplayAlert("Delete template?","Your current meal plan is kept.",AppLocalization.Text("Delete"),AppLocalization.Text("Keep")))return;AccountSession.Check(account);await PremiumLibrary.Remove<SavedWeekTemplate>(account,"weeks",e=>e.Id==entry.Id);await Render();}),true));body.Add(ShoppingUi.Card(card));
        }
        Content=new ScrollView{Content=body};
    }
}

public class SavedShoppingTripsPage:ContentPage
{
    private readonly string account=AccountSession.UserId;private bool busy;
    public SavedShoppingTripsPage(){Title="Saved shopping trips · Premium";ShoppingUi.Theme(this);}
    protected override async void OnAppearing(){base.OnAppearing();if(account!=AccountSession.UserId)Content=ShoppingUi.Text("Your account changed. Reopen this library from your account.");await Run(Render);}
    private async Task Run(Func<Task> action){if(busy)return;busy=true;try{AccountSession.Check(account);await action();}catch(Exception e){if(account!=AccountSession.UserId)Content=ShoppingUi.Text("Your account changed. Reopen this library.");await DisplayAlert("Saved shopping trips",e is InvalidOperationException?e.Message:"Your saved trips couldn't be updated. Your shopping list has been kept.",AppLocalization.Text("OK"));}finally{busy=false;}}
    private async Task Render()
    {
        var entries=await PremiumLibrary.Load<SavedShoppingTrip>(account,"shopping");AccountSession.Check(account);
        var body=new VerticalStackLayout{Padding=16,Spacing=14};body.Add(ShoppingUi.Text("Shop your favourites again",26,true));body.Add(ShoppingUi.Text("Choose a personal or household list to save before clearing it. Reuse a whole trip with quantities and notes, with every item reset to uncollected. These snapshots stay private on this device, even when copied from a household list.",14));
        body.Add(ShoppingUi.Button("Save a shopping list…",()=>Run(async()=>{
            if(!await PremiumAccess.Require("Saved shopping trips"))return;AccountSession.Check(account);
            var source=await ShoppingListDestinations.ChooseAsync("Save which shopping list?");AccountSession.Check(account);if(source==null)return;var list=await ShoppingListDestinations.ReadAsync(source);if(list.Items.Count==0)throw new InvalidOperationException("This shopping list is empty.");
            var name=await DisplayPromptAsync("Name this shopping trip","e.g. Weekly basics or Pasta night",maxLength:80);if(name==null)return;AccountSession.Check(account);
            await PremiumLibrary.Add(account,"shopping",SavedShoppingTrip.Create(name,list));await Render();
        })));
        if(entries.Count==0)body.Add(ShoppingUi.Text("No saved trips yet. Save a list here to reuse it next time."));
        foreach(var entry in entries)
        {
            var items=entry.ReusableItems();var card=new VerticalStackLayout{Spacing=8};card.Add(ShoppingUi.Text(entry.Name,21,true));card.Add(ShoppingUi.Text($"{items.Count} items · {entry.SavedAt.LocalDateTime:d}",12));card.Add(ShoppingUi.Text(string.Join(" · ",items.Take(5).Select(i=>i.Name))+(items.Count>5?"…":""),14));
            card.Add(ShoppingUi.Button("Add trip to a list…",()=>Run(async()=>{
                if(!await PremiumAccess.Require("Saved shopping trips"))return;AccountSession.Check(account);
                var target=await ShoppingListDestinations.ChooseAsync("Add saved trip to which list?");AccountSession.Check(account);if(target==null)return;
                if(!await DisplayAlert("Add these items?",$"Add {items.Count} uncollected items from {entry.Name} to {target.Name}? Existing items are kept; compatible quantities combine.","Add items",AppLocalization.Text("Cancel")))return;
                AccountSession.Check(account);await ShoppingListDestinations.AddAsync(target,ShoppingListDestinations.Ingredients(entry.ReusableItems()));
            })));
            card.Add(ShoppingUi.Button("Delete saved trip…",()=>Run(async()=>{if(!await DisplayAlert("Delete saved trip?","Your current shopping list is kept.",AppLocalization.Text("Delete"),AppLocalization.Text("Keep")))return;AccountSession.Check(account);await PremiumLibrary.Remove<SavedShoppingTrip>(account,"shopping",e=>e.Id==entry.Id);await Render();}),true));body.Add(ShoppingUi.Card(card));
        }
        Content=new ScrollView{Content=body};
    }
}
