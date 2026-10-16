using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

[QueryProperty(nameof(SelectedDayIndex),nameof(SelectedDayIndex))]
public partial class MealPlannerPage : ContentPage
{
    public long SelectedDayIndex{get;set;}
    private string account;private bool loaded,busy,completed,leaving;private MealPlan plan;private MealPlanDay day;private string initial;private List<Recipe> recipes;
    public MealPlannerPage(){InitializeComponent();ShoppingUi.Theme(this);account=HouseholdService.UserId;
        ToolbarItems.Add(new ToolbarItem("Save",null,async()=>await Run(SaveDay)));
        Shell.SetBackButtonBehavior(this,new BackButtonBehavior {Command=new Command(async()=>await Cancel())});
    }
    public MealPlannerPage(long index,string owner):this(){SelectedDayIndex=index;account=owner;}
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if(account!=HouseholdService.UserId){Content=ShoppingUi.Text("Your account changed. Reopen your plan.");return;}
        if(loaded)return;
        await Run(async()=>{Check();await RecipeService.CreateMealPlan(account);plan=await RecipeService.GetMealPlan(account);Check();var source=plan.Days.FirstOrDefault(d=>d.MealDayIndex==SelectedDayIndex)??throw new InvalidOperationException("That day is unavailable.");
            day=MealPlanning.Copy(source);recipes=(await RecipeService.GetAllRecipes()).Where(r=>account==null ? string.IsNullOrEmpty(r.FirebaseUID) : r.FirebaseUID==account).ToList();initial=MealPlanning.State(day);loaded=true;Render();});
    }
    private void Check(){if(account!=HouseholdService.UserId)throw new InvalidOperationException("Your account changed. Close this day and reopen your own plan.");}
    private async Task Run(Func<Task> action){if(busy)return;busy=true;try{await action();}catch(Exception e){await DisplayAlert("Meal planner",e is InvalidOperationException ? e.Message : "That change couldn't be saved. Your edits remain open.","OK");}finally{busy=false;}}
    private void Render()
    {
        Title=plan.WeekStart.AddDays(day.MealDayIndex).ToString("dddd d MMM");var body=new VerticalStackLayout {Padding=16,Spacing=16};
        body.Add(ShoppingUi.Text("Make space for good food",27,true));body.Add(ShoppingUi.Text("Choose recipes, adjust portions, or add a note for leftovers, eating out or a flexible meal. Save when you're ready.",14));
        for(var slot=0;slot<3;slot++)
        {
            var current=slot;var card=new VerticalStackLayout {Spacing=10};card.Add(ShoppingUi.Text(MealPlanning.Slots[slot],23,true));
            foreach(var id in MealPlanning.Ids(day,slot).ToList())
            {
                var recipe=recipes.FirstOrDefault(r=>r.Id==id);card.Add(ShoppingUi.Text(recipe?.RecipeName??"Recipe unavailable — remove or replace",18,true));
                if(recipe!=null){card.Add(ShoppingUi.Text(RecipePresentation.TimeText(recipe),12));card.Add(ShoppingUi.Button($"{MealPlanning.Servings(day,slot,recipe)} servings · Change",()=>Run(async()=>{Check();var text=await DisplayPromptAsync("Servings for this meal","How many portions of this recipe?",initialValue:MealPlanning.Servings(day,current,recipe).ToString(),keyboard:Keyboard.Numeric,maxLength:4);if(text==null)return;if(!int.TryParse(text,out var portions))throw new InvalidOperationException("Enter a whole number of servings.");MealPlanning.SetServings(day,current,id,portions);Render();}),true));}
                card.Add(ShoppingUi.Button("Remove recipe",()=>{MealPlanning.Ids(day,current).Remove(id);Render();return Task.CompletedTask;},true));
            }
            foreach(var note in MealPlanning.Notes(day,slot).ToList())
            {
                card.Add(ShoppingUi.Text("↳ "+note));card.Add(ShoppingUi.Button("Edit / remove note",async()=>{Check();var text=await DisplayPromptAsync("Meal note","Clear the text to remove this note.",initialValue:note,maxLength:500);if(text==null)return;var notes=MealPlanning.Notes(day,current);var index=notes.IndexOf(note);if(index>=0){if(string.IsNullOrWhiteSpace(text))notes.RemoveAt(index);else notes[index]=text.Trim();}Render();},true));
            }
            card.Add(ShoppingUi.Button("+ Choose recipes",async()=>{Check();await Navigation.PushAsync(new MealRecipePickerPage(recipes,MealPlanning.Ids(day,current),ids=>{Check();var target=MealPlanning.Ids(day,current);target.Clear();target.AddRange(ids);Render();}));}));
            card.Add(ShoppingUi.Button("+ Add note / leftovers",async()=>{Check();var text=await DisplayPromptAsync("Meal note","e.g. Leftovers, packed lunch or eating out",maxLength:500);if(!string.IsNullOrWhiteSpace(text)){MealPlanning.Notes(day,current).Add(text.Trim());Render();}},true));body.Add(ShoppingUi.Card(card));
        }
        body.Add(ShoppingUi.Button("Save day",()=>Run(SaveDay)));
        body.Add(ShoppingUi.Button("Cancel changes",Cancel,true));Content=new ScrollView {Content=body};
    }
    private async Task SaveDay()
    {
        if(!loaded)throw new InvalidOperationException("Wait for the day to load.");
        Check();MealPlanning.Serialize(day);await RecipeService.SaveMealPlanDay(day,plan.Id,day.MealDayIndex);completed=true;await Navigation.PopAsync();
    }
    private async Task Cancel()
    {
        if(busy||leaving)return;leaving=true;
        try{if(loaded&&!completed&&MealPlanning.State(day)!=initial&&!await DisplayAlert("Discard changes?","Your saved day will be kept.","Discard","Keep editing"))return;completed=true;await Navigation.PopAsync();}
        finally{leaving=false;}
    }
    protected override bool OnBackButtonPressed(){_=Cancel();return true;}
}

internal class MealRecipePickerPage : ContentPage
{
    public MealRecipePickerPage(List<Recipe> recipes,IEnumerable<long> current,Action<List<long>> selected)
    {
        Title="Choose recipes";ShoppingUi.Theme(this);var ids=current.ToHashSet();var body=new VerticalStackLayout {Spacing=10,Padding=16};var search=new SearchBar {Placeholder="Search your recipes"};var rows=new VerticalStackLayout {Spacing=10};
        void Render(){rows.Clear();foreach(var recipe in recipes.Where(r=>(r.RecipeName??"").Contains(search.Text??"",StringComparison.OrdinalIgnoreCase))){var line=new Grid {ColumnDefinitions=new(){new(new GridLength(44)),new(GridLength.Star)}};line.Add(ShoppingUi.Check(ids.Contains(recipe.Id),v=>{if(v)ids.Add(recipe.Id);else ids.Remove(recipe.Id);}),0);line.Add(new VerticalStackLayout {Children={ShoppingUi.Text(recipe.RecipeName,17,true),ShoppingUi.Text(RecipePresentation.TimeText(recipe),12)}},1);rows.Add(ShoppingUi.Card(line));}if(rows.Count==0)rows.Add(ShoppingUi.Text("No matching recipes. Add a recipe in your recipe book first."));}
        search.TextChanged+=(_,_)=>Render();body.Add(search);body.Add(rows);Render();var layout=new Grid {RowDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)}};layout.Add(new ScrollView {Content=body},0,0);layout.Add(ShoppingUi.Button("Use selected recipes",async()=>{selected(ids.ToList());await Navigation.PopAsync();}),0,1);Content=layout;
    }
}
