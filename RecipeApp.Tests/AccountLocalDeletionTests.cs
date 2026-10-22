using RecipeApp.Services;
using RecipeApp.Models;
using Xunit;

public class AccountLocalDeletionTests
{
    [Fact]
    public async Task AccountPurgeRemovesOnlyItsRecipesIngredientsPlannerAndDrafts()
    {
        var account="delete-"+Guid.NewGuid();var other="keep-"+Guid.NewGuid();
        var own=new Recipe{RecipeName="Own",FirebaseUID=account,Ingredients=new(){new Ingredient{Name="private"}}};
        var keep=new Recipe{RecipeName="Other",FirebaseUID=other,Ingredients=new(){new Ingredient{Name="keep"}}};
        var guest=new Recipe{RecipeName="Device",Ingredients=new(){new Ingredient{Name="device"}}};
        await RecipeService.AddRecipe(own);await RecipeService.AddRecipe(keep);await RecipeService.AddRecipe(guest);
        await RecipeService.CreateMealPlan(account);await RecipeService.CreateMealPlan(other);
        await RecipeDraftStore.Save(account,RecipeDraft.FromRecipe(own));
        await RecipeService.DeleteAccountLocal(account);
        Assert.Null(await RecipeService.GetRecipe(own.Id));Assert.NotNull(await RecipeService.GetRecipe(keep.Id));Assert.NotNull(await RecipeService.GetRecipe(guest.Id));
        Assert.Null(await RecipeService.GetMealPlan(account));Assert.NotNull(await RecipeService.GetMealPlan(other));Assert.False(File.Exists(RecipeDraftStore.PathFor(account,own.Id)));
    }
}
