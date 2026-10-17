using RecipeApp.Services;
namespace RecipeApp.Views;
public class RecipeWebsitePage:ContentPage
{
    private readonly Entry link=new(){Placeholder="https://…",Keyboard=Keyboard.Url,MaxLength=2000};
    private readonly Label status=ShoppingUi.Text("");private bool busy;private readonly string account=AccountSession.UserId;
    public RecipeWebsitePage(string shared=null){Title="Import a recipe link";ShoppingUi.Theme(this);link.Text=shared;Content=new ScrollView{Content=new VerticalStackLayout{Padding=20,Spacing=14,Children={ShoppingUi.Text("From the web to your recipe book",25,true),ShoppingUi.Text("Paste a recipe link from a supported website. You'll review and edit it before saving privately. Some sites block imports; you can paste their ingredients manually."),link,ShoppingUi.Button("Import & review",Import),status}}};}
    private async Task Import(){if(busy)return;busy=true;status.Text="Reading recipe…";try{if(await RecipeDraftStore.Load(account,0)!=null){status.Text="Finish or discard your existing new-recipe draft before importing another recipe. Your shared link is kept here.";await Navigation.PushAsync(new AddRecipePage());return;}var recipe=await WebsiteRecipe.Fetch(link.Text);AccountSession.Check(account);await Navigation.PushAsync(new AddRecipePage(recipe));status.Text="Review the title, ingredients, portions and method before saving.";}catch(Exception e){status.Text=e is InvalidOperationException?e.Message:"This website couldn't be read. Check the link or paste the recipe manually.";}finally{busy=false;}}
}
