using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public partial class MealPlanOverviewPage : ContentPage
{
    private MealPlan plan;private List<Recipe> recipes=new();private string account;private bool busy;
    public MealPlanOverviewPage(){InitializeComponent();ShoppingUi.Theme(this);ToolbarItems.Add(new ToolbarItem("Household plan",null,async()=>await AppNavigation.Open(new HouseholdMealsPage())));ToolbarItems.Add(new ToolbarItem("Weekly templates · Premium",null,async()=>await PremiumAccess.OpenWeeks()));ToolbarItems.Add(new ToolbarItem("Plan a batch in my personal week",null,async()=>await Run(PlanBatch)));}
    private async Task PlanBatch()
    {
        Check();if(recipes.Count==0)throw new InvalidOperationException("Save a recipe first.");
        var labels=recipes.Select((r,i)=>$"{i+1}. {r.RecipeName} · {r.Serving} portions").ToArray();
        var selected=await DisplayActionSheet("Choose a batch recipe","Cancel",null,labels);var index=Array.IndexOf(labels,selected);if(index<0)return;
        var people=2;
        if(account!=null){try{var info=await HouseholdService.StatusAsync();people=Math.Max(1,info.Members?.Count??2);}catch(HouseholdException){}}
        Check();var answer=await DisplayPromptAsync("Members eating this meal","How many people? The recipe's portions will be spread over consecutive days, with shopping counted once across the batch.",initialValue:people.ToString(),keyboard:Keyboard.Numeric,maxLength:4);if(answer==null)return;
        if(!int.TryParse(answer,out people)||people<1||people>1000)throw new InvalidOperationException("Choose 1–1,000 people.");
        var dates=plan.Days.OrderBy(d=>d.MealDayIndex).Select(d=>plan.WeekStart.AddDays(d.MealDayIndex).ToString("dddd d MMM")).ToArray();
        var date=await DisplayActionSheet("Cook on","Cancel",null,dates);var start=Array.IndexOf(dates,date);if(start<0)return;
        var meal=await DisplayActionSheet("Meal","Cancel",null,MealPlanning.Slots);var slot=Array.IndexOf(MealPlanning.Slots,meal);if(slot<0)return;
        Check();var copies=MealPlanning.PlanBatch(plan.Days,recipes[index],people,start,slot);var count=(int)Math.Ceiling((double)recipes[index].Serving/people);
        if(!await DisplayAlert("Review batch",$"{recipes[index].RecipeName}: {recipes[index].Serving} portions for {people} people across {count} meals. Ingredients total one recipe batch. All chosen meal slots must be empty.","Add batch",AppLocalization.Text("Cancel")))return;
        Check();await RecipeService.SaveMealPlanDays(copies,plan.Id);await Load();
    }
    protected override async void OnAppearing(){base.OnAppearing();await Run(Load);}
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;
        try{await action();}catch(Exception e){await DisplayAlert(AppLocalization.Text("Meal planner"),e is InvalidOperationException ? e.Message : "Your plan couldn't be updated. Please try again.",AppLocalization.Text("OK"));}finally{busy=false;}
    }
    private void Check(){if(account!=HouseholdService.UserId)throw new InvalidOperationException("Your account changed. Reopen the meal planner.");}
    private async Task Load()
    {
        if(account!=HouseholdService.UserId)Content=new LoadingIndicator("Loading your plan…");
        account=HouseholdService.UserId;await RecipeService.CreateMealPlan(account);plan=await RecipeService.GetMealPlan(account);await RecipeService.GetRecipesForLocal();Check();
        recipes=RecipeService.Recipes.Where(r=>account==null ? string.IsNullOrEmpty(r.FirebaseUID) : r.FirebaseUID==account).ToList();Render();
    }
    private string SlotText(MealPlanDay day,int slot)=>string.Join(" · ",MealPlanning.Ids(day,slot).Select(id=>{
        var recipe=recipes.FirstOrDefault(r=>r.Id==id);return recipe==null ? "Recipe unavailable" : $"{recipe.RecipeName} ({MealPlanning.Servings(day,slot,recipe)} servings)";
    }).Concat(new[]{MealPlanning.SimpleMeal(day,slot)}).Where(text=>!string.IsNullOrWhiteSpace(text)));
    private void Render()
    {
        var body=new VerticalStackLayout {Padding=16,Spacing=16};body.Add(new MembershipPrompt(true));body.Add(ShoppingUi.Text(AppLocalization.Text("A week worth looking forward to"),28,true));
        var filled=plan.Days.Sum(d=>Enumerable.Range(0,3).Count(s=>MealPlanning.Ids(d,s).Count>0||MealPlanning.Notes(d,s).Count>0||MealPlanning.SimpleMeal(d,s).Length>0));
        body.Add(ShoppingUi.Text($"{filled} of 21 meals planned · Personal plan",14));body.Add(new ProgressBar {Progress=filled/21d,ProgressColor=Color.FromArgb("#D0A34F")});
        body.Add(ShoppingUi.Text(AppLocalization.Text("Week starts"),12,true));var start=new DatePicker {Date=plan.WeekStart.Date,Format="ddd d MMM yyyy"};start.SetDynamicResource(DatePicker.TextColorProperty, "ThemeText");
        start.DateSelected+=async(_,e)=>await Run(async()=>{Check();plan.WeekStart=(e.NewDate??plan.WeekStart).Date;await RecipeService.SaveMealPlanHeader(plan);Render();});body.Add(start);
        body.Add(ShoppingUi.Text("Changing the week date keeps your meals, so you can reuse the plan. Choose a day to edit meals and portions.",13));
        var scroll=new ScrollView {Content=body};var anchors=new Dictionary<long,View>();var strip=new HorizontalStackLayout {Spacing=8};
        foreach(var jump in plan.Days){var date=plan.WeekStart.AddDays(jump.MealDayIndex);strip.Add(ShoppingUi.Button((date.Date==DateTime.Today?"Today · ":"")+date.ToString("ddd d"),async()=>{if(anchors.TryGetValue(jump.MealDayIndex,out var anchor))await scroll.ScrollToAsync(anchor,ScrollToPosition.Start,true);},true));}
        body.Add(new ScrollView {Orientation=ScrollOrientation.Horizontal,Content=strip});
        foreach(var day in plan.Days)
        {
            var date=plan.WeekStart.AddDays(day.MealDayIndex);var card=new VerticalStackLayout {Spacing=9};card.Add(ShoppingUi.Text(date.ToString("dddd · d MMM"),21,true));
            for(var slot=0;slot<3;slot++)
            {
                card.Add(ShoppingUi.Text(MealPlanning.Slots[slot],12,true));var text=SlotText(day,slot);if(text.Length>0||MealPlanning.Notes(day,slot).Count==0)card.Add(ShoppingUi.Text(text.Length==0 ? "Leave open or add a meal" : text,15));
                foreach(var note in MealPlanning.Notes(day,slot))card.Add(ShoppingUi.Text("↳ "+note,12));
            }
            card.Add(ShoppingUi.Button(AppLocalization.Text("Plan this day"),async()=>{Check();await Navigation.PushAsync(new MealPlannerPage(day.MealDayIndex,account));}));
            var actions=new Grid {ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};
            actions.Add(ShoppingUi.Button("Copy to…",()=>Run(async()=>{
                Check();var targets=plan.Days.Where(d=>d.Id!=day.Id).ToList();var labels=targets.Select(d=>plan.WeekStart.AddDays(d.MealDayIndex).ToString("dddd d MMM")).ToArray();
                var choice=await DisplayActionSheet("Copy this day's meals and notes","Cancel",null,labels);var index=Array.IndexOf(labels,choice);if(index<0)return;
                if(!await DisplayAlert("Replace that day's meals?","Its current meals, notes and portions will be replaced.","Copy day",AppLocalization.Text("Keep")))return;Check();
                var copy=MealPlanning.Copy(day,MealPlanning.Copy(targets[index]));await RecipeService.SaveMealPlanDay(copy,plan.Id,copy.MealDayIndex);await Load();
            }),true),0);
            actions.Add(ShoppingUi.Button("Clear day",()=>Run(async()=>{
                if(!await DisplayAlert("Clear this day?","Remove its meals and notes?","Clear",AppLocalization.Text("Keep")))return;Check();var copy=MealPlanning.Copy(day);MealPlanning.Clear(copy);await RecipeService.SaveMealPlanDay(copy,plan.Id,copy.MealDayIndex);await Load();
            }),true),1);card.Add(actions);var anchor=ShoppingUi.Card(card);anchors[day.MealDayIndex]=anchor;body.Add(anchor);
        }
        body.Add(ShoppingUi.Button("Review shopping for this week",()=>Run(async()=>{Check();var choices=MealPlanning.ShoppingChoices(plan.Days,recipes);await Navigation.PushAsync(new ShoppingListMakerPage(choices,MealPlanning.TypedShoppingMeals(plan.Days)));})));
        body.Add(ShoppingUi.Text("Shopping uses your total planned servings for saved recipes. For typed meals, review matching essentials before adding. Amounts are yours to check; ingredients are never added automatically.",13));
        body.Add(ShoppingUi.Button("Copy weekly plan",async()=>{Check();await Clipboard.Default.SetTextAsync(string.Join("\n\n",plan.Days.Select(d=>plan.WeekStart.AddDays(d.MealDayIndex).ToString("dddd d MMM")+"\n"+string.Join("\n",Enumerable.Range(0,3).Select(s=>$"{MealPlanning.Slots[s]}: {SlotText(d,s)}"+string.Join("",MealPlanning.Notes(d,s).Select(n=>" · "+n)))))));await DisplayAlert("Copied","Your weekly plan is on the clipboard.",AppLocalization.Text("OK"));},true));
        if(account!=null)body.Add(ShoppingUi.Button("Copy previous device plan…",()=>Run(async()=>{
            Check();var legacy=await RecipeService.GetMealPlan();if(legacy==null)throw new InvalidOperationException("There is no previous device plan to copy.");
            if(!await DisplayAlert("Copy device plan into this account?","This copies the device plan's meals and notes, replacing this account's current weekly plan. The device plan is kept.","Copy and replace",AppLocalization.Text("Cancel")))return;Check();
            var copies=plan.Days.Select(d=>MealPlanning.Copy(d)).ToList();foreach(var target in copies){var source=legacy.Days.FirstOrDefault(d=>d.MealDayIndex==target.MealDayIndex);if(source!=null)MealPlanning.Copy(source,target);}
            await RecipeService.SaveMealPlanDays(copies,plan.Id);await Load();
        }),true));
        body.Add(ShoppingUi.Button("Clear all seven days…",()=>Run(async()=>{
            if(!await DisplayAlert("Clear the weekly plan?","All meals and notes in this personal plan will be removed.","Clear week",AppLocalization.Text("Keep")))return;Check();var copies=plan.Days.Select(d=>MealPlanning.Copy(d)).ToList();foreach(var copy in copies)MealPlanning.Clear(copy);await RecipeService.SaveMealPlanDays(copies,plan.Id);await Load();
        }),true));Content=scroll;
    }
}
