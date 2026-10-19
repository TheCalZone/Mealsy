using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
public class StepIngredientsTests
{
    private static List<Ingredient> Items(params string[] names)=>names.Select(name=>new Ingredient{Name=name}).ToList();
    [Fact]
    public void FindsIngredientNamesIncludingPreparationAndPluralVariants()
    {
        var ingredients=Items("Red onions","500g minced beef","Olive oil","Tomatoes","Garlic cloves");
        Assert.Equal(new[]{0,2,4},StepIngredients.Match("Soften the onion and garlic in the oil.",ingredients));
        Assert.Equal(new[]{1,3},StepIngredients.Match("Add beef and chopped tomato.",ingredients));
    }
    [Fact]
    public void CompoundNamesWinOverContainedIngredientNames()
    {
        var ingredients=Items("Chicken","Chicken stock","Cream","Ice cream");
        Assert.Equal(new[]{1},StepIngredients.Match("Pour in the chicken stock.",ingredients));
        Assert.Equal(new[]{0,1},StepIngredients.Match("Brown the chicken, then add chicken stock.",ingredients));
        Assert.Equal(new[]{3},StepIngredients.Match("Serve with ice cream.",ingredients));
    }
    [Fact]
    public void AmbiguousShortReferencesAndSubstringsAreNotGuessed()
    {
        Assert.Empty(StepIngredients.Match("Add oil.",Items("Olive oil","Sesame oil")));
        Assert.Empty(StepIngredients.Match("Roast cauliflower and eggplant.",Items("Flour","Eggs")));
        Assert.Empty(StepIngredients.Match("Add the remaining ingredients.",Items("Salt","Rice")));
        Assert.Empty(StepIngredients.Match("Pour in red wine.",Items("Red onions")));
    }
    [Fact]
    public void RepeatedUsesAndDuplicateSectionIngredientsRemainDistinct()
    {
        var ingredients=Items("Butter","Butter");ingredients[0].Quantity="50 g";ingredients[0].IngredientSection="Cake";ingredients[1].IngredientSection="Icing";
        Assert.Equal(new[]{0,1},StepIngredients.Match("Beat the butter.",ingredients));
        Assert.Equal(new[]{0,1},StepIngredients.Match("Add remaining butter.",ingredients));Assert.Equal("50 g",ingredients[0].Quantity);
    }
    [Fact]
    public void AccentsAndPunctuationAreNormalizedWithoutInventingAmounts()
    {
        Assert.Equal(new[]{0,1},StepIngredients.Match("Add creme fraiche, then the bay leaf.",Items("Crème fraîche","Bay leaves")));
        Assert.Empty(StepIngredients.Match("",Items("Salt")));
    }
}
