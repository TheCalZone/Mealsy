using RecipeApp.Models;
namespace RecipeApp.Services;
public static class RecipeBrowser
{
    public static IEnumerable<Recipe> Visible(IEnumerable<Recipe> recipes,string account,string search="",string tag="All",bool favourites=false,string sort="Recent")
    {
        var result=recipes.Where(r=>account==null?string.IsNullOrEmpty(r.FirebaseUID):r.FirebaseUID==account)
            .Where(r=>string.IsNullOrWhiteSpace(search)||new[]{r.RecipeName,r.Description,r.Tags}.Any(s=>s?.Contains(search.Trim(),StringComparison.OrdinalIgnoreCase)==true))
            .Where(r=>tag=="All"||Tags(r).Contains(tag,StringComparer.OrdinalIgnoreCase)).Where(r=>!favourites||Tags(r).Contains("favourites",StringComparer.OrdinalIgnoreCase));
        return sort switch{"Name"=>result.OrderBy(r=>r.RecipeName,StringComparer.OrdinalIgnoreCase),"Quickest"=>result.OrderBy(r=>r.TotalTime==0?long.MaxValue:r.TotalTime),"Top rated"=>result.OrderByDescending(r=>r.Rating),_=>result.OrderByDescending(r=>r.LastUpdated).ThenByDescending(r=>r.Id)};
    }
    public static string[] Tags(Recipe recipe)=>(recipe.Tags??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Where(s=>!s.Equals("all",StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    public static Recipe ForServings(Recipe source,int servings)
    {
        if(servings<1||servings>1000)throw new InvalidOperationException("Choose 1–1,000 servings.");var copy=RecipeDraft.Copy(source);var factor=(decimal)servings/Math.Max(1,source.Serving);copy.Serving=servings;
        foreach(var item in copy.Ingredients)item.Quantity=ShoppingQuantity.Scale(item.Quantity,factor);return copy;
    }
}
