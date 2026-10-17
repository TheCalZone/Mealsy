using RecipeApp.Models;
namespace RecipeApp.Services;

public class BusinessPlanChoice
{
    public FoodPost Post{get;set;}public bool Selected{get;set;}=true;public int Day{get;set;}public int Slot{get;set;}public string Servings{get;set;}
}
public static class CreatorPlanSelection
{
    public static List<BusinessPlanChoice> Validate(IEnumerable<BusinessPlanChoice> choices)
    {
        var selected=choices.Where(c=>c.Selected).ToList();if(selected.Count==0)throw new InvalidOperationException("Choose at least one meal.");
        if(selected.Any(c=>c.Post?.Recipe==null||c.Day<0||c.Day>6||c.Slot<0||c.Slot>2||!int.TryParse(c.Servings,out var n)||n<1||n>1000))throw new InvalidOperationException("Choose a recipe, day, meal slot and 1–1,000 servings for every meal.");
        if(selected.Select(c=>$"{c.Day}:{c.Slot}:{c.Post.Id}").Distinct().Count()!=selected.Count)throw new InvalidOperationException("Choose each recipe only once per meal slot.");return selected;
    }
    public static Dictionary<string,int> ShoppingServings(IEnumerable<BusinessPlanChoice> choices)
    {
        var totals=Validate(choices).GroupBy(c=>c.Post.Id).ToDictionary(g=>g.Key,g=>g.Sum(c=>int.Parse(c.Servings)));
        if(totals.Values.Any(n=>n>1000))throw new InvalidOperationException("Shop at most 1,000 total portions of each recipe at a time. Select fewer meals for this shop.");
        return totals;
    }
}
