using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public partial class RecipePage : ContentPage
{
    private List<Recipe> all=new();private string account,search="",tag="All",sort="Recent";private bool favourites;private CollectionView list;private Label count;
    public RecipePage(){InitializeComponent();Title="Your recipes";ShoppingUi.Theme(this);}
    protected override async void OnAppearing()
    {
        base.OnAppearing();sort=AppSettings.Current.RecipeSort;var uid=AccountSession.UserId;Content=new LoadingIndicator("Opening your recipe book…");
        try{await RecipeService.GetRecipesForLocal();AccountSession.Check(uid);if(account!=uid){search="";tag="All";favourites=false;}account=uid;all=RecipeBrowser.Visible(RecipeService.Recipes,uid).ToList();Render();}
        catch(Exception){Content=ShoppingUi.Button("Couldn't open recipes · Retry",async()=>{await RecipeService.GetRecipesForLocal();account=AccountSession.UserId;all=RecipeBrowser.Visible(RecipeService.Recipes,account).ToList();Render();});}
    }
    private void Render()
    {
        var header=new VerticalStackLayout{Padding=16,Spacing=10};header.Add(ShoppingUi.Text("Keep the meals you love",28,true));header.Add(ShoppingUi.Text(account==null?"Your device recipe book. Sign in for a separate synced collection.":"Your private recipe book · Save, plan and cook your favourites.",14));
        var actions=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};var create=ShoppingUi.Button("+ Create recipe",()=>AppNavigation.Open(new AddRecipePage()));actions.Add(create,0);if(!ProductFeatures.SocialEnabled)actions.Add(ShoppingUi.Button("Import recipe link",()=>AppNavigation.Open(new RecipeWebsitePage()),true),1);if(ProductFeatures.SocialEnabled)actions.Add(ShoppingUi.Button("Food community",()=>AppNavigation.Open(new FoodFeedPage()),true),1);header.Add(actions);
        var find=new SearchBar{Placeholder="Search names, descriptions or tags",Text=search};find.TextChanged+=(_,e)=>{search=e.NewTextValue??"";Filter();};header.Add(find);
        var filters=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=10};var tags=new List<string>{"All"};tags.AddRange(all.SelectMany(RecipeBrowser.Tags).Distinct(StringComparer.OrdinalIgnoreCase).Order());if(!tags.Contains(tag))tag="All";
        var tagPicker=new Picker{Title="Tags",ItemsSource=tags,SelectedItem=tag,HeightRequest=48};var sortPicker=new Picker{Title="Sort",ItemsSource=new[]{"Recent","Name","Quickest","Top rated"},SelectedItem=sort,HeightRequest=48};
        foreach(var picker in new[]{tagPicker,sortPicker})picker.SetDynamicResource(Picker.TextColorProperty, "ThemeText");tagPicker.SelectedIndexChanged+=(_,_)=>{tag=tagPicker.SelectedItem?.ToString()??"All";Filter();};sortPicker.SelectedIndexChanged+=(_,_)=>{sort=sortPicker.SelectedItem?.ToString()??"Recent";Filter();};filters.Add(tagPicker,0);filters.Add(sortPicker,1);header.Add(filters);
        header.Add(new HorizontalStackLayout{Children={ShoppingUi.Check(favourites,v=>{favourites=v;Filter();}),ShoppingUi.Text("Favourites only",14)}});count=ShoppingUi.Text("",12);header.Add(count);
        list=new CollectionView{SelectionMode=SelectionMode.None,Header=header,ItemTemplate=new DataTemplate(()=>{
            var holder=new ContentView{Padding=new Thickness(16,0,16,16)};holder.BindingContextChanged+=(_,_)=>{holder.Content=holder.BindingContext is Recipe recipe?RecipeCardView.Card(recipe,async()=>{AccountSession.Check(account);await AppNavigation.Recipe(recipe.Id);}):null;};return holder;
        }),EmptyView=new VerticalStackLayout{Padding=24,Spacing=12,Children={ShoppingUi.Text("Make room for a new favourite",23,true),ShoppingUi.Text("No recipes match these filters. Clear them or save your first recipe."),ShoppingUi.Button("Clear filters",()=>{search="";tag="All";favourites=false;Render();return Task.CompletedTask;},true)}}};Content=list;Filter();
    }
    private void Filter(){if(list==null||count==null)return;var visible=RecipeBrowser.Visible(all,account,search,tag,favourites,sort).ToList();count.Text=$"{visible.Count} of {all.Count} recipes";list.ItemsSource=visible;}
}
