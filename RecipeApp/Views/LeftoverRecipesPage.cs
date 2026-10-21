using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

public class LeftoverRecipesPage : ContentPage
{
    private readonly Editor input=new(){Placeholder="e.g. I have minced beef left, carrots and onions",AutoSize=EditorAutoSizeOption.TextChanges,MinimumHeightRequest=84,MaxLength=1000};
    private readonly Label status=ShoppingUi.Text("",14);
    private readonly CollectionView results=new(){SelectionMode=SelectionMode.None};
    private string account;
    private bool loaded;
    public LeftoverRecipesPage()
    {
        Title="Use what you have";ShoppingUi.Theme(this);
        input.SetDynamicResource(Editor.TextColorProperty,"ThemeText");input.SetDynamicResource(Editor.BackgroundColorProperty,"ThemeSurface");
        var header=new VerticalStackLayout{Padding=16,Spacing=12,Children={ShoppingUi.Text("What needs using up?",27,true),ShoppingUi.Text("Find meals in your saved recipes by ingredient. Works offline, with no AI or service key. Separate ingredients with commas or “and”.",14),input,
            ShoppingUi.Button("Find recipes",Search),ShoppingUi.Button("Clear",()=>{input.Text="";results.ItemsSource=null;status.Text="Enter an ingredient to find recipes in your book.";return Task.CompletedTask;},true),status}};
        results.Header=header;
        results.ItemTemplate=new DataTemplate(()=>{
            var holder=new ContentView{Padding=new Thickness(16,0,16,16)};
            holder.BindingContextChanged+=(_,_)=>{
                if(holder.BindingContext is not LeftoverRecipes.Match match){holder.Content=null;return;}
                var owner=account;
                var body=new VerticalStackLayout{Spacing=8};
                body.Add(ShoppingUi.Text($"Matches {match.Matched.Count} of {match.Matched.Count+match.Missing.Count} ingredients you entered",14,true));
                body.Add(ShoppingUi.Text("Uses: "+string.Join(" · ",match.IngredientNames),13));
                if(match.Missing.Count>0)body.Add(ShoppingUi.Text("Doesn't use: "+string.Join(", ",match.Missing),13));
                body.Add(RecipeCardView.Card(match.Recipe,async()=>{AccountSession.Check(owner);await AppNavigation.Recipe(match.Recipe.Id);}));
                body.Add(ShoppingUi.Button("Review shopping ingredients",async()=>{AccountSession.Check(owner);await AppNavigation.Open(new ShoppingListMakerPage(new[]{new RecipeShoppingChoice(match.Recipe){Selected=true}}));},true));
                holder.Content=body;
            };return holder;
        });
        Content=results;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();var uid=AccountSession.UserId;
        if(uid!=account){input.Text="";results.ItemsSource=null;}
        account=uid;loaded=false;status.Text="Loading your saved recipes…";
        try{await RecipeService.GetRecipesForLocal();AccountSession.Check(uid);loaded=true;if(!string.IsNullOrWhiteSpace(input.Text))await Search();else status.Text="Enter an ingredient to find recipes in your book.";}
        catch(Exception){results.ItemsSource=null;status.Text="Couldn't load your recipe book. Reopen this screen to try again.";}
    }
    private Task Search()
    {
        try
        {
            AccountSession.Check(account);if(!loaded)return Task.CompletedTask;
            AppNavigation.DismissKeyboard();
            var query=LeftoverRecipes.Ingredients(input.Text);
            var matches=LeftoverRecipes.Find(RecipeService.Recipes,account,input.Text);results.ItemsSource=matches;
            status.Text=query.Count==0?"Enter an ingredient, such as minced beef.":matches.Count==0?"No saved recipes use those ingredients. Try a simpler name, or save a recipe from the recipe book.":$"{matches.Count} matching recipes · Best matches first. Check the full recipe for other ingredients you need; amounts and substitutions aren't inferred.";
        }
        catch(Exception error){results.ItemsSource=null;status.Text=error is InvalidOperationException?error.Message:"Search couldn't finish. Please try again.";}
        return Task.CompletedTask;
    }
}
