using RecipeApp.Services;
using Xunit;
public class IngredientSuggestionsTests
{
    [Fact]
    public void MincedShowsUsefulCompletionsWithoutNetworkOrPersonalData()
    {
        Assert.Equal(new[]{"Minced beef","Minced lamb","Minced pork","Minced garlic","Minced ginger"},IngredientSuggestions.Suggest("minced",null,null));
        Assert.Equal("Minced beef",IngredientSuggestions.Suggest("500g min",null,null)[0]);
        Assert.Empty(IngredientSuggestions.Suggest("m",null,null));Assert.Empty(IngredientSuggestions.Suggest("",null,null));
    }
    [Fact]
    public void WholeWordPrefixesAvoidUnrelatedSubstringMatches()
    {
        var matches=IngredientSuggestions.Suggest("ha",new[]{"Shampoo","Smoked ham"},null);
        Assert.DoesNotContain("Shampoo",matches);Assert.Contains("Ham",matches);Assert.Contains("Smoked ham",matches);
        Assert.Contains("Minced beef",IngredientSuggestions.Suggest("beef min",null,null));
    }
    [Fact]
    public void OwnRecipesChecklistAndHistoryNamesRankAboveGenericAlternatives()
    {
        var history=new Dictionary<string,int>{{"Minced pork",6},{"Minced beef",2}};
        Assert.Equal("Minced pork",IngredientSuggestions.Suggest("minced",new[]{"Minced venison"},history)[0]);
        Assert.Equal("Minced venison",IngredientSuggestions.Suggest("minced",new[]{"Minced venison"},null)[0]);
        Assert.Single(IngredientSuggestions.Suggest("minced be",new[]{"minced beef","500 g minced beef"},null));
    }
    [Fact]
    public void SourcesNeverIncludeOtherAccountsOrGuestDataAfterSignIn()
    {
        var sources=new[]{new IngredientSuggestions.Source("Alice ingredient","alice"),new IngredientSuggestions.Source("Bob ingredient","bob"),new IngredientSuggestions.Source("Guest ingredient",null)};
        Assert.Equal(new[]{"Alice ingredient"},IngredientSuggestions.ForAccount(sources,"alice"));Assert.Equal(new[]{"Guest ingredient"},IngredientSuggestions.ForAccount(sources,null));
    }
    [Fact]
    public void HistoryIsBoundedAndMalformedHistoryCanBeIgnored()
    {
        Assert.Empty(IngredientSuggestions.ReadHistory("broken"));Assert.Empty(IngredientSuggestions.ReadHistory("null"));
        var previous=Enumerable.Range(0,200).ToDictionary(i=>"Ingredient "+i,_=>1);
        var saved=IngredientSuggestions.Remember(previous,"500 g minced beef");Assert.Equal(200,saved.Count);Assert.Contains("minced beef",saved.Keys);
        saved=IngredientSuggestions.Remember(saved,"Minced beef");Assert.Equal(2,saved["Minced beef"]);Assert.Equal(200,saved.Count);
        Assert.DoesNotContain("minced beef",previous.Keys);
    }
    [Fact]
    public void SuggestionQueryAmountsPreserveExplicitQuantityWhenSelected()
    {
        Assert.Equal("750 g",ShoppingItemInput.Parse("500g minced","750 g").Quantity);
        Assert.Equal("500 g",ShoppingItemInput.Parse("500g minced","").Quantity);
    }
}
