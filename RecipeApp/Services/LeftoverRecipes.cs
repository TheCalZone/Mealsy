using RecipeApp.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RecipeApp.Services;

/// <summary>Offline ingredient-name matching; never invents recipes or assumes pantry contents.</summary>
public static class LeftoverRecipes
{
    private static readonly HashSet<string> Filler=new("ve am only need want can make what could cook something find me suggest i we have has got some any leftover leftovers left remaining over to use up please recipes recipe with using for my our a an the of fresh frozen chopped diced sliced grated finely roughly grams gram g kg ml litre litres pound pounds lb oz".Split(' '));
    private static readonly HashSet<string> Forms=new("stock broth powder paste puree juice sauce extract flour oil".Split(' '));
    private static string[] Words(string value)
    {
        var decomposed=(value??"").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var text=new string(decomposed.Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
        text=Regex.Replace(text,@"\b(minced?|ground)\s+(beef|lamb|pork|turkey|chicken)\b","$2 mince");
        return Regex.Matches(text,@"[\p{L}]+").Select(m=>m.Value).Where(w=>!Filler.Contains(w)).Select(w=>w switch {
            "tomatoes"=>"tomato","potatoes"=>"potato","leaves"=>"leaf",
            _ when w.EndsWith('s')&&!w.EndsWith("ss")&&w.Length>3=>w[..^1], _=>w
        }).ToArray();
    }
    public static List<string> Ingredients(string input)
    {
        if((input??"").Length>1000)throw new InvalidOperationException("Keep your ingredient search within 1,000 characters.");
        var ingredients=Regex.Split(input??"",@"[,;\r\n]+|\band\b|&",RegexOptions.IgnoreCase)
            .Select(part=>string.Join(' ',Words(part))).Where(part=>part.Length>0).Distinct().ToList();
        if(ingredients.Count>10)throw new InvalidOperationException("Search for up to 10 ingredients at a time.");
        return ingredients;
    }
    public record Match(Recipe Recipe,IReadOnlyList<string> Matched,IReadOnlyList<string> Missing,IReadOnlyList<string> IngredientNames);
    public static List<Match> Find(IEnumerable<Recipe> recipes,string account,string input)
    {
        var query=Ingredients(input);
        if(query.Count==0)return new();
        return recipes.Where(r=>account==null?string.IsNullOrEmpty(r.FirebaseUID):r.FirebaseUID==account).Select(recipe=>{
            var ingredients=(recipe.Ingredients??new()).Where(i=>!string.IsNullOrWhiteSpace(i.Name)).Select(i=>new {i.Name,Tokens=Words(i.Name).ToHashSet()}).ToList();
            bool Contains(string requested,HashSet<string> tokens)
            {
                var wanted=requested.Split(' ').ToHashSet();
                return wanted.All(tokens.Contains) && !tokens.Any(token=>Forms.Contains(token)&&!wanted.Contains(token));
            }
            var matched=query.Where(q=>ingredients.Any(i=>Contains(q,i.Tokens))).ToList();
            var names=ingredients.Where(i=>matched.Any(q=>Contains(q,i.Tokens))).Select(i=>i.Name).Distinct().ToList();
            return new Match(recipe,matched,query.Except(matched).ToList(),names);
        }).Where(m=>m.Matched.Count>0).OrderByDescending(m=>m.Matched.Count)
            .ThenBy(m=>m.Recipe.RecipeName,StringComparer.OrdinalIgnoreCase).ThenBy(m=>m.Recipe.Id).ToList();
    }
}
