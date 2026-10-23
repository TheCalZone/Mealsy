using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class HouseholdMealsPage:SocialPage
{
    private DateTime weekStart=DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek+6)%7));
    private HouseholdWeek week;private HouseholdInfo household;
    public HouseholdMealsPage():base("Household meal plan"){Body.Add(ShoppingUi.Text("What’s for dinner?",28,true));Body.Add(ShoppingUi.Text("Share meals, portions, recipe snapshots and notes with your household. Your personal plan stays separate."));Body.Add(new MembershipPrompt(true));Body.Add(ShoppingUi.Button("Sign in or manage my household",()=>Account==null?AppNavigation.Root("AccountPage"):AppNavigation.Open(new HouseholdPage()),true));Body.Add(ShoppingUi.Button("Use my personal meal planner",()=>AppNavigation.Root("MealPlanOverviewPage"),true));ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Run(()=>Load(true))));}
    protected override async void OnAppearing(){base.OnAppearing();await Run(()=>Load());}
    private async Task Load(bool force=false)
    {
        household=await HouseholdService.StatusAsync(force);Check();
        if(household.HouseholdId==null)throw new InvalidOperationException("Join or create a household from Account first. Your personal meal plan stays separate.");
        if(!household.FamilyEnabled)throw new InvalidOperationException("Your household owner's Premium plan needs to be active to use the shared planner.");
        var cached=HouseholdWarmup.Cached;
        if(!force&&cached?.Week?.WeekStart==SocialPolicy.DateKey(weekStart)){week=cached.Week;Render("Saved on this device · "+cached.MealsChecked.ToLocalTime().ToString("g"));}
        week=await HouseholdMealsService.Call<HouseholdWeek>(new{action="readWeek",householdId=household.HouseholdId,weekStart=SocialPolicy.DateKey(weekStart)},force);Check();
        if(week.HouseholdId!=household.HouseholdId)throw new InvalidOperationException("This household plan could not be verified.");
        HouseholdWarmup.RecordWeek(Account,week);
        Render("Current view · Shared reads are reused for up to a minute. Use Refresh for the latest changes.");
    }
    private void Render(string freshness)
    {
        Body.Clear();Body.Add(ShoppingUi.Text("What's for dinner? Sorted.",28,true));
        Body.Add(ShoppingUi.Button($"Requests · {household.PendingRequests} pending",()=>AppNavigation.Open(new HouseholdRequestsPage()),true));
        if(household.Restricted)Body.Add(ShoppingUi.Text("You can view meals and send suggestions. An adult approves changes to the shared plan.",14));
        Body.Add(ShoppingUi.Text(freshness,12));Body.Add(ShoppingUi.Text(household.Name+" · Shared by everyone in your household",14));
        Body.Add(ShoppingUi.Text("Meals here are separate from your personal planner. Changes are saved to the household; refresh to see another person's latest edits.",13));
        var picker=new DatePicker{Date=weekStart,Format="ddd d MMM yyyy"};picker.DateSelected+=async(_,e)=>{weekStart=e.NewDate.Date;await Run(()=>Load());};Body.Add(ShoppingUi.Text("Week starts",12,true));Body.Add(picker);
        var jump=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};jump.Add(ShoppingUi.Button("Previous week",()=>Run(async()=>{weekStart=weekStart.AddDays(-7);await Load();}),true),0);jump.Add(ShoppingUi.Button("Next week",()=>Run(async()=>{weekStart=weekStart.AddDays(7);await Load();}),true),1);Body.Add(jump);
        for(var day=0;day<7;day++)
        {
            var date=weekStart.AddDays(day);var key=SocialPolicy.DateKey(date);var card=new VerticalStackLayout{Spacing=9};card.Add(ShoppingUi.Text((date.Date==DateTime.Today?"Today · ":"")+date.ToString("dddd d MMM"),22,true));
            foreach(var slot in new[]{"breakfast","lunch","dinner"})
            {
                card.Add(ShoppingUi.Text(char.ToUpper(slot[0])+slot[1..],13,true));var entries=week.Meals.Where(m=>m.Date==key&&m.Slot==slot).ToList();
                if(entries.Count==0)card.Add(ShoppingUi.Text("No meal planned yet",14));
                foreach(var meal in entries){card.Add(ShoppingUi.Text($"{meal.Title} · {meal.Servings} portions",18,true));if(!string.IsNullOrWhiteSpace(meal.Note))card.Add(ShoppingUi.Text(meal.Note,13));
                    if(meal.Recipe!=null)card.Add(ShoppingUi.Button("Cook this meal",()=>{var recipe=RecipeBrowser.ForServings(meal.Recipe.ToRecipe(""),meal.Servings);return AppNavigation.Open(new RecipePreviewPage(RecipeDraft.FromRecipe(recipe)));},true));
                    if(!household.Restricted)card.Add(ShoppingUi.Button("Edit meal",()=>AppNavigation.Open(new HouseholdMealEditorPage(household.HouseholdId,weekStart,meal)),true));
                    if(!household.Restricted)card.Add(ShoppingUi.Button("Remove meal…",()=>Run(async()=>{if(!await DisplayAlert("Remove shared meal?","This removes it from everyone's household plan. Your original recipe is kept.",AppLocalization.Text("Remove"),AppLocalization.Text("Keep")))return;Check();await HouseholdMealsService.Call<object>(new{action="deleteMeal",householdId=household.HouseholdId,weekStart=SocialPolicy.DateKey(weekStart),mealId=meal.Id,expectedVersion=meal.Version});await Load();}),true));
                }
            }
            card.Add(ShoppingUi.Button(household.Restricted?"Request a meal":"+ Plan a meal",()=>AppNavigation.Open(new HouseholdMealEditorPage(household.HouseholdId,weekStart,date:date,requestOnly:household.Restricted))));Body.Add(ShoppingUi.Card(card));
        }
    }
}
public class HouseholdMealEditorPage:SocialPage
{
    private readonly string householdId,id;private readonly DateTime weekStart;private long version;
    private readonly Entry title,servings;private readonly Editor note;private readonly Picker slot;private readonly DatePicker date;
    private readonly Label attached=ShoppingUi.Text("No recipe attached",13);private FeedRecipe recipe;private bool saved;
    public HouseholdMealEditorPage(string household,DateTime start,HouseholdMeal original=null,DateTime? date=null,bool requestOnly=false):base(requestOnly?"Request a meal":original==null?"Plan household meal":"Edit household meal")
    {
        householdId=household;weekStart=start;id=original?.Id??Guid.NewGuid().ToString("N");version=original?.Version??0;recipe=original?.Recipe;
        title=ShoppingUi.Entry(original?.Title??"","e.g. Pasta, leftovers, eating out",_=>{},160);servings=ShoppingUi.Entry((original?.Servings??2).ToString(),"Portions",_=>{},4,true);note=new Editor{Text=original?.Note??"",Placeholder="e.g. I'll cook, home late, use leftovers",MaxLength=500,HeightRequest=100};
        slot=new Picker{Title="Meal",ItemsSource=new[]{"breakfast","lunch","dinner"},SelectedItem=original?.Slot??"dinner",HeightRequest=48};
        this.date=new DatePicker{MinimumDate=start,MaximumDate=start.AddDays(6),Date=original==null?date??start:DateTime.ParseExact(original.Date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture)};
        Body.Add(ShoppingUi.Text(requestOnly?"Suggest something to cook":"Everyone knows what's cooking",26,true));Body.Add(ShoppingUi.Text("This meal is shared with current household members. A recipe attachment shares a snapshot of its ingredients and method, never your private recipe notes or source link.",14));
        Body.Add(ShoppingUi.Text("Meal name",12,true));Body.Add(title);Body.Add(this.date);Body.Add(slot);Body.Add(ShoppingUi.Text("Portions",12,true));Body.Add(servings);Body.Add(note);attached.Text=recipe?.Title??"No recipe attached";Body.Add(attached);
        Body.Add(ShoppingUi.Button("Attach one of my recipes",()=>Run(async()=>{
            await RecipeService.GetRecipesForLocal();Check();var recipes=RecipeBrowser.Visible(RecipeService.Recipes,Account).ToList();if(recipes.Count==0)throw new InvalidOperationException("Save a personal recipe first, or type the meal name without a recipe.");
            var labels=recipes.Select((r,i)=>$"{i+1}. {r.RecipeName}").ToArray();var choice=await DisplayActionSheet("Choose a recipe","Cancel",null,labels);var index=Array.IndexOf(labels,choice);if(index<0)return;
            if(!await DisplayAlert("Share a recipe snapshot?","Its ingredients and method will be visible to your household. Private notes and source links stay in your recipe book.","Attach snapshot",AppLocalization.Text("Cancel")))return;
            Check();recipe=FeedRecipe.FromRecipe(recipes[index]);attached.Text=recipe.Title;if(string.IsNullOrWhiteSpace(title.Text))title.Text=recipe.Title;
        }),true));Body.Add(ShoppingUi.Button("Remove attachment",()=>{recipe=null;attached.Text="No recipe attached";return Task.CompletedTask;},true));
        Body.Add(ShoppingUi.Button("Import recipe from link",async()=>{
            Check();
            await Navigation.PushAsync(new RecipeWebsitePage(saved: async imported => {
                Check();
                if(!await DisplayAlert("Share a recipe snapshot?","Its ingredients and method will be visible to your household. Private notes and source links stay in your recipe book.","Attach snapshot","Keep private"))return;
                Check();recipe=FeedRecipe.FromRecipe(imported);attached.Text=recipe.Title;
                if(string.IsNullOrWhiteSpace(title.Text))title.Text=recipe.Title;
            }));
        },true));
        Body.Add(ShoppingUi.Button("Copy my meal draft",async()=>{Check();await Clipboard.Default.SetTextAsync($"{title.Text} · {this.date.Date:d} · {slot.SelectedItem} · {servings.Text} portions\n{note.Text}");await DisplayAlert("Draft copied","Your meal text is on the clipboard.",AppLocalization.Text("OK"));},true));
        if(!requestOnly)Body.Add(ShoppingUi.Button("Review latest server version",()=>Run(async()=>{
            await HouseholdMealsService.Require();var latest=await HouseholdMealsService.Call<HouseholdWeek>(new{action="readWeek",householdId,weekStart=SocialPolicy.DateKey(weekStart)},true);Check();var current=latest.Meals.FirstOrDefault(m=>m.Id==id);
            if(current==null){await DisplayAlert("No current meal","This meal has not been saved, or someone removed it. Your draft remains open. Reopen the week before creating a replacement.",AppLocalization.Text("OK"));return;}
            if(await DisplayAlert("Review latest meal",$"{current.Title} · {current.Servings} portions\n{current.Note}\n\nKeep your form's draft and use it to replace this version on the next Save? Another edit will still cause a conflict.","Use my draft","Keep reviewing"))version=current.Version;
        }),true));
        Body.Add(ShoppingUi.Button(requestOnly?"Send meal request":AppLocalization.Text("Save to household"),()=>Run(async()=>{
            if(saved)return;if(!int.TryParse(servings.Text,out var portions))throw new InvalidOperationException("Enter whole-number portions.");var meal=new HouseholdMeal(id,SocialPolicy.DateKey(this.date.Date),slot.SelectedItem?.ToString(),title.Text?.Trim(),portions,note.Text??"",recipe,version);SocialPolicy.ValidateMeal(meal);
            await HouseholdMealsService.Require();Check();var result=await HouseholdMealsService.Call<HouseholdMealSaveResult>(new{action=requestOnly?"requestMeal":"upsertMeal",householdId,weekStart=SocialPolicy.DateKey(weekStart),meal,expectedVersion=version});Check();if(result.Requested)await DisplayAlert("Request sent","An adult can approve this meal from Household requests. It is not in the meal plan yet.","OK");saved=true;await AppNavigation.Back();
        })));Body.Add(ShoppingUi.Text(requestOnly?"This suggestion waits for an owner or adult to approve it. Ingredients are not automatically added to shopping.":"If another person edits this meal first, the server must reject the conflict and keep your form open. Reopen the meal to review their latest version before replacing it.",13));
    }
}
