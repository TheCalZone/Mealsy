using RecipeApp.Services;

namespace RecipeApp.Views;

public class CupboardIngredientsPage : ContentPage
{
    private readonly string account = AccountSession.UserId;
    private readonly Action changed;
    private List<string> rules = new();
    private readonly VerticalStackLayout rows = new() { Spacing = 8 };
    private readonly Label status = ShoppingUi.Text("");
    private readonly Entry name = new() { Placeholder = "e.g. nutmeg or sesame oil", MaxLength = 100 };
    private readonly SearchBar search = new() { Placeholder = "Search cupboard ingredients" };
    public CupboardIngredientsPage(Action changed = null)
    {
        this.changed = changed;Title = "Cupboard ingredients";ShoppingUi.Theme(this);
        var body = new VerticalStackLayout { Padding = 20, Spacing = 14 };
        body.Add(ShoppingUi.Text("Check before you buy",26,true));
        body.Add(ShoppingUi.Text("These ingredients start in cupboard checks when you plan a shop. Tick anything you're running low on. Recipe quantities are kept, and nothing is removed from the recipe.",14));
        body.Add(ShoppingUi.Text("Recognition handles descriptions such as dried, fresh, chopped and freshly ground. Add a specific ingredient name, without a quantity. Use Buy each time in a recipe to override recognition.",13));
        body.Add(ShoppingUi.Text("This recognition list is saved on this device, separately for each account and device mode. Your existing Essentials checklist also supplies cupboard suggestions.",13));
        body.Add(name);body.Add(ShoppingUi.Button("Add cupboard ingredient",async()=>{
            Check();var values=CupboardRules.Validate(rules.Append(name.Text??""));
            if(values.Count==rules.Count){await DisplayAlert("Already recognised","That ingredient is already on your recognition list.",AppLocalization.Text("OK"));return;}
            CupboardStore.Save(account,values);rules=values;name.Text="";changed?.Invoke();Render();
        }));
        body.Add(search);search.TextChanged+=(_,_)=>Render();body.Add(status);body.Add(rows);
        body.Add(ShoppingUi.Button("Restore suggested ingredients",async()=>{
            Check();if(!await DisplayAlert("Restore suggestions?","Replace this recognition list with Mealsy's suggested herbs, spices, oils and staples? Your recipes and Essentials checklist are kept.","Restore",AppLocalization.Text("Cancel")))return;
            Check();CupboardStore.Reset(account);rules=CupboardStore.Load(account);changed?.Invoke();Render();
        },true));
        Content=new ScrollView{Content=body};
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        try{Check();rules=CupboardStore.Load(account);Render();}
        catch(Exception e){rows.Clear();status.Text=e.Message;name.IsEnabled=false;}
    }
    private void Check(){if(account!=AccountSession.UserId)throw new InvalidOperationException("Your account changed. Reopen Cupboard ingredients from your account.");}
    private void Render()
    {
        rows.Clear();status.Text=$"{rules.Count} recognised ingredients";
        foreach(var rule in rules.Where(r=>r.Contains(search.Text??"",StringComparison.OrdinalIgnoreCase)).OrderBy(r=>r)) {
            var grid=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)},ColumnSpacing=8};
            grid.Add(ShoppingUi.Text(rule,16),0);grid.Add(ShoppingUi.Button(AppLocalization.Text("Remove"),()=>{
                Check();var values=rules.Where(r=>r!=rule).ToList();CupboardStore.Save(account,values);rules=values;changed?.Invoke();Render();return Task.CompletedTask;
            },true),1);rows.Add(grid);
        }
        name.IsEnabled=true;
    }
}
