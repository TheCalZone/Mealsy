using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class RecipeWebsitePage:ContentPage
{
    private readonly Entry link=new(){Placeholder="https://…",Keyboard=Keyboard.Url,MaxLength=2000};
    private readonly CheckBox savePhoto=new(){IsChecked=true};
    private readonly Label status=ShoppingUi.Text("");
    private readonly ActivityIndicator loading=new();
    private readonly Button import;
    private readonly Func<Recipe,Task> saved;
    private bool busy;
    private readonly string account=AccountSession.UserId;
    public RecipeWebsitePage(string shared=null,Func<Recipe,Task> saved=null)
    {
        this.saved=saved;Title=AppLocalization.Text("Import a recipe link");ShoppingUi.Theme(this);link.Text=shared;
        var photoOption=new Grid{ColumnDefinitions=new(){new(GridLength.Auto),new(GridLength.Star)}};
        photoOption.Add(savePhoto,0);photoOption.Add(ShoppingUi.Text(AppLocalization.Text("Save recipe photo, if available"),15),1);
        SemanticProperties.SetDescription(savePhoto,"Save recipe photo, if available");
        import=ShoppingUi.Button(AppLocalization.Text("Import & review"),Import);
        Content=new ScrollView{Content=new VerticalStackLayout{Padding=20,Spacing=14,Children={
            ShoppingUi.Text(AppLocalization.Text("From the web to your recipe book"),25,true),
            ShoppingUi.Text(saved==null?"Paste a supported recipe link, then review it before saving privately.":"Import and review a recipe, then save it to attach it to this meal. Save your meal afterwards to keep the plan."),
            ShoppingUi.Text("Premium feature · Some sites block imports; you can paste their ingredients manually."),
            ShoppingUi.Text("From your browser: open a recipe, choose Share, then Save recipe to Mealsy (or Mealsy). Review the shared link here, then tap Import & review. Only supported recipe websites can be imported.",13),
            link,photoOption,ShoppingUi.Text("Photos are optional. If a photo isn't available, the recipe will still import.",12),import,loading,status}}};
    }
    private async Task Import()
    {
        if(busy)return;busy=true;import.IsEnabled=false;link.IsEnabled=false;savePhoto.IsEnabled=false;loading.IsRunning=true;status.Text=AppLocalization.Text("Reading recipe…");
        try
        {
            AccountSession.Check(account);
            if(await RecipeDraftStore.Load(account,0)!=null)
            {
                status.Text="Finish or discard your existing new-recipe draft before importing another recipe. Your link is kept here.";
                // An unrelated recovered draft must never be attached to the selected meal.
                if(saved==null)await Navigation.PushAsync(new AddRecipePage());
                return;
            }
            if(!await PremiumAccess.Require("Recipe-link importing")){status.Text="Your link is kept here. You can still paste ingredients into a recipe for free.";return;}
            AccountSession.Check(account);
            var includePhoto=savePhoto.IsChecked;
            var recipe=await WebsiteRecipe.Fetch(link.Text,includePhoto?WebsiteRecipePhoto.Load:null);
            AccountSession.Check(account);
            var message=includePhoto?(string.IsNullOrWhiteSpace(recipe.RecipeImage)?"No photo could be imported. You can add one in the editor.":"Recipe photo included. You can remove it in the editor."):"Imported without a photo.";
            status.Text=message+" Review the recipe before saving.";
            await Navigation.PushAsync(new AddRecipePage(recipe,saved==null?null:async result=>{
                AccountSession.Check(account);await saved(result);await Navigation.PopAsync();
            }));
        }
        catch(Exception e){status.Text=e is InvalidOperationException?e.Message:"This website couldn't be read. Check the link or paste the recipe manually.";}
        finally{busy=false;import.IsEnabled=true;link.IsEnabled=true;savePhoto.IsEnabled=true;loading.IsRunning=false;}
    }
}
