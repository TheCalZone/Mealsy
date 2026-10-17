using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
[QueryProperty(nameof(RecipeId),nameof(RecipeId))]
public partial class RecipeDetailsPage : ContentPage
{
    public string RecipeId{get;set;}public Recipe Recipe{get;set;}
    private RecipeReadView read;private string account;private bool busy;private readonly ToolbarItem favourite;
    public RecipeDetailsPage()
    {
        InitializeComponent();ShoppingUi.Theme(this);favourite=new ToolbarItem("☆ Favourite",null,async()=>await Run(async()=>{
            var tags=RecipeBrowser.Tags(Recipe).ToList();if(tags.Contains("favourites",StringComparer.OrdinalIgnoreCase))tags.RemoveAll(t=>t.Equals("favourites",StringComparison.OrdinalIgnoreCase));else tags.Add("favourites");Recipe.Tags=string.Join(',',tags.Append("all"));await RecipeService.UpdateRecipeFavouriteState(Recipe);favourite.Text=tags.Contains("favourites")?"★ Saved favourite":"☆ Favourite";
            if(account!=null)try{await FirestoreService.UpdateRecipe(Recipe);}catch(Exception){await DisplayAlert("Saved locally","Your favourite is saved. Retry account sync when connected.","OK");}
        }));ToolbarItems.Add(favourite);
        ToolbarItems.Add(new ToolbarItem("Share to food community",null,async()=>await Run(()=>AppNavigation.Open(new FeedComposerPage(Recipe)))){Order=ToolbarItemOrder.Secondary});
        ToolbarItems.Add(new ToolbarItem("Copy recipe text",null,async()=>await Run(async()=>{await Clipboard.Default.SetTextAsync(RecipePresentation.PlainText(RecipeBrowser.ForServings(Recipe,read.CurrentServings)));await DisplayAlert("Copied","Recipe text copied. Private notes and source links are excluded.","OK");})){Order=ToolbarItemOrder.Secondary});
        ToolbarItems.Add(new ToolbarItem("Delete recipe",null,async()=>await Run(async()=>{
            if(!await DisplayAlert("Delete this recipe?","Remove it from your recipe book? Planned meals referring to it will need a replacement.","Delete","Keep"))return;
            AccountSession.Check(account);if(account!=null)await FirestoreService.DeleteRecipe(Recipe);AccountSession.Check(account);await RecipeService.RemoveRecipe(Recipe.Id);await AppNavigation.Back();
        })){Order=ToolbarItemOrder.Secondary});
    }
    private async Task Run(Func<Task> action)
    {
        if(busy||Recipe==null)return;busy=true;
        try{AccountSession.Check(account);if(!AccountSession.Owns(Recipe.FirebaseUID))throw new InvalidOperationException("This recipe belongs to a different account.");await action();}
        catch(Exception e){await DisplayAlert("Recipe",e is InvalidOperationException?e.Message:"That action couldn't finish. Check your connection and retry; no local recipe was removed.","OK");}finally{busy=false;}
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();account=AccountSession.UserId;Content=ShoppingUi.Text("Opening your recipe…");
        try
        {
            var recipe=long.TryParse(RecipeId,out var id)?await RecipeService.GetRecipe(id):null;AccountSession.Check(account);
            if(recipe==null||!AccountSession.Owns(recipe.FirebaseUID)){Recipe=null;await DisplayAlert("Recipe unavailable","This recipe isn't available in your current recipe book.","OK");await AppNavigation.Back();return;}
            Recipe=recipe;Title=recipe.RecipeName;favourite.Text=RecipeBrowser.Tags(recipe).Contains("favourites",StringComparer.OrdinalIgnoreCase)?"★ Saved favourite":"☆ Favourite";read=new(recipe);
            var footer=new Grid{Padding=new Thickness(16,8),ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};
            footer.Add(ShoppingUi.Button("Shop ingredients",()=>Run(()=>AppNavigation.Open(new ShoppingListMakerPage(new[]{new RecipeShoppingChoice(Recipe){Selected=true,Servings=read.CurrentServings.ToString()}})))),0);
            footer.Add(ShoppingUi.Button("Edit recipe",()=>Run(()=>AppNavigation.Open(new EditRecipePage{RecipeId=Recipe.Id.ToString()})),true),1);
            var layout=new Grid{RowDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)}};layout.Add(read,0,0);layout.Add(footer,0,1);Content=layout;read.StartCooking();
        }
        catch(Exception){Content=ShoppingUi.Text("This recipe couldn't be opened. Go back and try again.");}
    }
    protected override void OnDisappearing(){read?.StopCooking();base.OnDisappearing();}
}
