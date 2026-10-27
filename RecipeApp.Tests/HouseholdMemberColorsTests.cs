using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
public class HouseholdMemberColorsTests
{
    [Fact]
    public void ChosenColourOverridesStableFallbackAndUntrustedColoursAreIgnored()
    {
        var fallback=HouseholdMemberColors.Resolve("alex");
        Assert.Equal(fallback,new ShoppingListItem{AddedBy="alex"}.ContributorColor);
        foreach(var colour in HouseholdMemberColors.Values)Assert.Equal(colour,new ShoppingListItem{AddedBy="alex",AddedByColor=colour}.ContributorColor);
        Assert.Equal(fallback,HouseholdMemberColors.Resolve("alex","#FFFFFF"));
        Assert.Equal(HouseholdMemberColors.Names.Length,HouseholdMemberColors.Values.Length);
    }
}
