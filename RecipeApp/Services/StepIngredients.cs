using RecipeApp.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace RecipeApp.Services;

/// <summary>Local, conservative name matching. Does not infer amounts or implicit additions.</summary>
public static class StepIngredients
{
    private static readonly HashSet<string> Modifiers=new("fresh freshly dried finely roughly chopped minced crushed grated diced sliced peeled skinless boneless large medium small optional extra virgin unsalted salted divided".Split(' '));
    private static string Singular(string value)=>value switch{
        "tomatoes"=>"tomato","potatoes"=>"potato","leaves"=>"leaf","cloves"=>"clove",
        _=>value.EndsWith("ies",StringComparison.Ordinal)&&value.Length>4?value[..^3]+"y":value.EndsWith('s')&&!value.EndsWith("ss",StringComparison.Ordinal)&&value.Length>3?value[..^1]:value
    };
    private static List<string> Words(string text)
    {
        var normalized=(text??"").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        normalized=new string(normalized.Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Matches(normalized,@"[\p{L}]+").Select(match=>Singular(match.Value)).ToList();
    }
    public static List<int> Match(string step,IReadOnlyList<Ingredient> ingredients)
    {
        var text=Words(step);var cores=ingredients.Select(item=>Words(Regex.Replace(ShoppingItemInput.Parse(item?.Name,"").Name.Split(',')[0],@"\([^)]*\)"," ")).Where(word=>!Modifiers.Contains(word)).ToList()).ToList();
        var exact=new List<(int Ingredient,int Start,int Length)>();
        for(var index=0;index<cores.Count;index++){
            var core=cores[index];if(core.Count==0)continue;
            for(var start=0;start+core.Count<=text.Count;start++)if(core.SequenceEqual(text.Skip(start).Take(core.Count)))exact.Add((index,start,core.Count));
        }
        bool Covered(int index,int start,int length)=>exact.Any(other=>other.Ingredient!=index&&other.Length>length&&start>=other.Start&&start+length<=other.Start+other.Length);
        var result=exact.Where(match=>!Covered(match.Ingredient,match.Start,match.Length)).Select(match=>match.Ingredient).ToHashSet();
        // Short references such as "oil" or "onions" are safe only when one
        // ingredient owns that word; never guess between two oils or onions.
        for(var index=0;index<cores.Count;index++){
            if(result.Contains(index))continue;
            foreach(var word in cores[index].Distinct()){
                if(word.Length<3||word is "and" or "with" or "for" or "taste" or "needed" or "powder" or "paste" or "breast" or "fillet" or "clove" or "red" or "green" or "white" or "black" or "brown" or "yellow" or "sweet")continue;
                if(cores.Count(core=>core.Contains(word))!=1)continue;
                if(text.Select((token,start)=>(token,start)).Any(match=>match.token==word&&!Covered(index,match.start,1))){result.Add(index);break;}
            }
        }
        return result.Order().ToList();
    }
}
