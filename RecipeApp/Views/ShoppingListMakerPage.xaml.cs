using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views;

public partial class ShoppingListMakerPage : ContentPage
{
    private readonly ShoppingListMakerViewModel model=new();
    private readonly ContentView pane=new();
    private readonly Label summary=ShoppingUi.Text("Loading your recipe book…",12);
    private readonly Label heading=ShoppingUi.Text("",25,true);
    private readonly ProgressBar progress=new() { ProgressColor=Color.FromArgb("#D0A34F"), HeightRequest=6 };
    private readonly VerticalStackLayout footer=new() { Padding=new Thickness(16,8), Spacing=6 };
    private List<RecipeShoppingChoice> seed;
    private int stage;
    private string recipeSearch="", librarySearch="", recipeTag="All";
    private bool librarySnacks;
    public ShoppingListMakerPage() : this((IEnumerable<RecipeShoppingChoice>)null) { }
    public ShoppingListMakerPage(bool snacks) : this() { stage=1; librarySnacks=snacks; }
    public ShoppingListMakerPage(IEnumerable<RecipeShoppingChoice> recipes)
    {
        InitializeComponent(); ShoppingUi.Theme(this); seed=recipes?.ToList();
        var header=new VerticalStackLayout { Padding=new Thickness(16,12), Spacing=8, Children={heading,summary,progress} };
        var layout=new Grid { RowDefinitions=new() { new(GridLength.Auto),new(GridLength.Star),new(GridLength.Auto) } };
        layout.Add(header,0,0); layout.Add(pane,0,1); layout.Add(footer,0,2); Content=layout;
        model.Changed += UpdateSummary;
        ToolbarItems.Add(new ToolbarItem("Open current shopping list",null,async()=>await AppNavigation.ShoppingList()));
        ToolbarItems.Add(new ToolbarItem("Cupboard ingredients",null,async()=>await Navigation.PushAsync(new CupboardIngredientsPage())));
        ToolbarItems.Add(new ToolbarItem("Start over",null,async () => {
            if (!await DisplayAlert("Start a fresh plan?","Your saved shopping list and reusable checklists will be kept.","Start over","Keep planning")) return;
            model.Reset(); stage=0; Render();
        }) { Order=ToolbarItemOrder.Secondary });
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var account=model.Account;
            if (seed!=null) { await model.UseRecipes(seed); seed=null; stage=1; }
            else await model.Load();
            if (account!=model.Account) stage=0;
            if (stage>0) model.BuildReview(); Render();
        }
        catch (Exception e) { await DisplayAlert("Creator unavailable",e is InvalidOperationException ? e.Message : "Your recipes and checklists couldn't be loaded. Please reopen this screen.",AppLocalization.Text("OK")); }
    }
    private void UpdateSummary() => summary.Text=model.Summary + (stage==2 ? $" · {model.Review.Count(r => r.Included)} items to buy" : "");
    private void Render()
    {
        heading.Text=stage switch {0=>AppLocalization.Text("1  Choose your meals"),1=>AppLocalization.Text("2  Check the cupboard"),_=>AppLocalization.Text("3  Ready for your shop")}; progress.Progress=(stage+1)/3d; UpdateSummary(); footer.Clear();
        if (stage==0) RenderRecipes(); else if (stage==1) RenderChecks(); else RenderReview();
        if (stage>0) footer.Add(ShoppingUi.Button("← Back",() => { stage--; Render(); return Task.CompletedTask; },true));
        if (stage<2) footer.Add(ShoppingUi.Button(stage==0 ? "Check ingredients & extras →" : "Review my shopping list →",() => {
            model.BuildReview(); stage++; Render(); return Task.CompletedTask;
        }));
        else
        {
            footer.Add(ShoppingUi.Button("Add to my current list",async () => await model.WriteList(false)));
            footer.Add(ShoppingUi.Button("Replace my personal list…",async () => {
                if (await DisplayAlert("Replace your personal shopping list?","Every existing item and collected checkmark in your personal list will be replaced. Your household list won't change.","Replace","Keep current list")) await model.WriteList(true);
            },true));
        }
    }
    private void RenderRecipes()
    {
        var search=new SearchBar { Text=recipeSearch, Placeholder="Find a recipe" };
        var tags=new List<string> {"All"}; tags.AddRange(model.Recipes.SelectMany(r => (r.Recipe.Tags ?? "").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries)).Where(t => !t.Equals("all",StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).Order());
        if (!tags.Contains(recipeTag)) recipeTag="All";
        var picker=new Picker { Title="Recipe tags",ItemsSource=tags,SelectedItem=recipeTag,HeightRequest=48 };
        picker.SetDynamicResource(Picker.TextColorProperty, "ThemeText");
        var list=new CollectionView { SelectionMode=SelectionMode.None, ItemTemplate=new DataTemplate(() => new RecipeChoiceRow()), EmptyView=ShoppingUi.Text("No matching recipes. Add a recipe or continue to shop from your checklists.",15) };
        void Filter() => list.ItemsSource=model.Recipes.Where(r => (r.Recipe.RecipeName ?? "").Contains(recipeSearch,StringComparison.OrdinalIgnoreCase) && (recipeTag=="All" || (r.Recipe.Tags ?? "").Split(',',StringSplitOptions.TrimEntries).Contains(recipeTag,StringComparer.OrdinalIgnoreCase))).ToList();
        search.TextChanged += (_,e) => { recipeSearch=e.NewTextValue ?? ""; Filter(); };
        picker.SelectedIndexChanged += (_,_) => { recipeTag=picker.SelectedItem?.ToString() ?? "All"; Filter(); }; Filter();
        var top=new VerticalStackLayout { Padding=new Thickness(16,0), Spacing=4, Children={ShoppingUi.Text("Choose servings per batch and how many times you'll cook. Snacks and essentials can be checked even without a recipe.",14),search,picker} };
        var grid=new Grid { RowDefinitions=new() {new(GridLength.Auto),new(GridLength.Star)} }; grid.Add(top,0,0); grid.Add(list,0,1); pane.Content=grid;
    }
    private void RenderChecks()
    {
        var body=new VerticalStackLayout { Padding=16, Spacing=14 };
        body.Add(ShoppingUi.Text(AppLocalization.Text("Already in the cupboard?"),22,true));
        body.Add(ShoppingUi.Text("These include non-main ingredients, common herbs, spices and oils, and items saved as essentials. Tick only what you need to buy. Required ingredients can be checked on the next screen.",14));
        var optional=model.Review.Where(r => r.CheckCupboard).ToList();
        if (optional.Count==0) body.Add(ShoppingUi.Text(AppLocalization.Text("No cupboard checks from these recipes. You can still add snacks or essentials below."),14));
        if (optional.Count>0)
        {
            var bulk=new Grid {ColumnDefinitions=new() {new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};
            bulk.Add(ShoppingUi.Button("Need all",() => { foreach (var row in optional) row.Included=true; return Task.CompletedTask; },true),0);
            bulk.Add(ShoppingUi.Button("Have all",() => { foreach (var row in optional) row.Included=false; return Task.CompletedTask; },true),1); body.Add(bulk);
        }
        foreach (var row in optional) body.Add(ReviewCard(row,false));
        body.Add(ShoppingUi.Text(AppLocalization.Text("Your reusable checklists"),22,true));
        body.Add(ShoppingUi.Text(AppLocalization.Text("Nothing here is added automatically. Tick what's running low for this trip."),14));
        var tabs=new Grid { ColumnDefinitions=new() {new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8 };
        tabs.Add(ShoppingUi.Button(librarySnacks ? "Essentials" : "✓ Essentials",() => { librarySnacks=false; RenderChecks(); return Task.CompletedTask; },true),0);
        tabs.Add(ShoppingUi.Button(librarySnacks ? "✓ Snacks" : "Snacks",() => { librarySnacks=true; RenderChecks(); return Task.CompletedTask; },true),1); body.Add(tabs);
        var search=new SearchBar { Text=librarySearch,Placeholder="Search this checklist" };
        var entries=new VerticalStackLayout { Spacing=10 };
        void Filter()
        {
            entries.Clear(); var visible=model.Library.Where(i => i.IsSnack==librarySnacks && (i.Name ?? "").Contains(librarySearch,StringComparison.OrdinalIgnoreCase)).ToList();
            if (visible.Count==0) entries.Add(ShoppingUi.Text(AppLocalization.Text("Nothing here yet. Save items you like to check before a shop."),14));
            foreach (var item in visible)
            {
                var content=new VerticalStackLayout {Spacing=6};
                var line=new Grid {ColumnDefinitions=new() {new(new GridLength(44)),new(GridLength.Star)}};
                var check=ShoppingUi.Check(item.Selected,v => item.Selected=v); check.BindingContext=item; check.SetBinding(CheckBox.IsCheckedProperty,nameof(item.Selected),BindingMode.TwoWay); line.Add(check,0);
                var info=new VerticalStackLayout {Spacing=3,Children={ShoppingUi.Text(item.Name,17,true),ShoppingUi.Text(string.Join(" · ",new[] {item.Quantity,item.Category}.Where(s => !string.IsNullOrWhiteSpace(s))),13)}};
                if (!string.IsNullOrWhiteSpace(item.Note)) info.Add(ShoppingUi.Text(item.Note,12)); line.Add(info,1); content.Add(line);
                content.Add(ShoppingUi.Button("Edit item",async () => await Navigation.PushAsync(new ShoppingExtraEditorPage(model,item)),true)); entries.Add(ShoppingUi.Card(content));
            }
        }
        search.TextChanged += (_,e) => {librarySearch=e.NewTextValue ?? ""; Filter();}; body.Add(search);
        body.Add(ShoppingUi.Button("+ Save a " + (librarySnacks ? "snack" : "cupboard essential"),async () => await Navigation.PushAsync(new ShoppingExtraEditorPage(model,snack:librarySnacks)),true));
        body.Add(entries); Filter(); pane.Content=new ScrollView {Content=body};
    }
    private void RenderReview()
    {
        var body=new VerticalStackLayout {Padding=16,Spacing=12};
        body.Add(ShoppingUi.Text(AppLocalization.Text("Tick what you need; adjust amounts before adding. Compatible quantities combine. Different units or preparation notes stay separate."),14));
        if (model.Review.Count==0) body.Add(ShoppingUi.Text(AppLocalization.Text("Your plan is empty. Go back to choose meals, snacks or essentials."),17,true));
        foreach (var group in model.Review.GroupBy(r => r.Item.Category).OrderBy(g => g.Key))
        {
            body.Add(ShoppingUi.Text(group.Key,21,true)); foreach (var row in group) body.Add(ReviewCard(row,true));
        }
        pane.Content=new ScrollView {Content=body};
    }
    private View ReviewCard(ShoppingReviewItem row, bool edit)
    {
        var body=new VerticalStackLayout {Spacing=7}; var line=new Grid {ColumnDefinitions=new() {new(new GridLength(44)),new(GridLength.Star)}};
        var check=ShoppingUi.Check(row.Included,v => row.Included=v); check.BindingContext=row; check.SetBinding(CheckBox.IsCheckedProperty,nameof(row.Included),BindingMode.TwoWay); line.Add(check,0);
        var info=new VerticalStackLayout {Spacing=3,Children={ShoppingUi.Text(row.Item.Name,17,true),ShoppingUi.Text(row.Item.Quantity,15,true),ShoppingUi.Text(row.Sources,12)}}; line.Add(info,1); body.Add(line);
        if (row.AmountNeedsReview) body.Add(ShoppingUi.Text("Check the amount · no safe numeric total available",12,true));
        if (edit)
        {
            body.Add(ShoppingUi.Text("Quantity to buy · optional",12,true)); body.Add(ShoppingUi.Entry(row.Item.Quantity,"e.g. 500 g or 1 jar",s => row.Item.Quantity=s));
            body.Add(ShoppingUi.Text("Shopping note · optional",12,true)); body.Add(ShoppingUi.Entry(row.Item.Note,"Brand, substitution or preparation",s => row.Item.Note=s,500));
        }
        else if (!string.IsNullOrWhiteSpace(row.Item.Note)) body.Add(ShoppingUi.Text(row.Item.Note,12));
        if (!model.Library.Any(i => !i.IsSnack && ShoppingQuantity.Identity(i.Name)==ShoppingQuantity.Identity(row.Item.Name)))
            body.Add(ShoppingUi.Button("Remember as an essential",async () => await Navigation.PushAsync(new ShoppingExtraEditorPage(model,suggestedName:row.Item.Name,suggestedCategory:row.Item.Category)),true));
        return ShoppingUi.Card(body);
    }
    private sealed class RecipeChoiceRow : ContentView
    {
        public RecipeChoiceRow() { BindingContextChanged += (_,_) => Build(); }
        private void Build()
        {
            if (BindingContext is not RecipeShoppingChoice choice) return;
            Padding=new Thickness(16,6); var body=new VerticalStackLayout {Spacing=8};
            var title=new Grid {ColumnDefinitions=new() {new(new GridLength(44)),new(GridLength.Star)}};
            var check=ShoppingUi.Check(choice.Selected,v => choice.Selected=v); check.SetBinding(CheckBox.IsCheckedProperty,nameof(choice.Selected),BindingMode.TwoWay); title.Add(check,0);
            title.Add(ShoppingUi.Text(choice.Recipe.RecipeName,18,true),1); body.Add(title);
            body.Add(ShoppingUi.Text(RecipePresentation.TimeText(choice.Recipe),12));
            var amounts=new Grid {ColumnDefinitions=new() {new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=10};
            amounts.SetBinding(IsVisibleProperty,nameof(choice.Selected));
            var portions=new VerticalStackLayout {Children={ShoppingUi.Text(AppLocalization.Text("Servings per batch"),12,true),ShoppingUi.Entry(choice.Servings,"Servings",s => choice.Servings=s,4,true)}};
            var batches=new VerticalStackLayout {Children={ShoppingUi.Text("Number of batches",12,true),ShoppingUi.Entry(choice.Batches,"Batches",s => choice.Batches=s,3,true)}};
            amounts.Add(portions,0); amounts.Add(batches,1); body.Add(amounts); Content=ShoppingUi.Card(body);
        }
    }
}
