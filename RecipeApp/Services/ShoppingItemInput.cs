using System.Text.RegularExpressions;
namespace RecipeApp.Services;

public static class ShoppingItemInput
{
    public record Parsed(string Name,string Quantity);
    public static Parsed Parse(string name,string quantity)
    {
        name=(name??"").Trim();quantity=(quantity??"").Trim();
        if(ShoppingQuantity.TryParse(name,out _))return new(name,quantity);
        // Prefer the longest recognized amount, including package sizes such as
        // "2 x 400 g". Only split at whitespace; never strip digits from 7UP etc.
        if(Regex.IsMatch(name,@"^(?:5|7)\s+spice\b",RegexOptions.IgnoreCase))return new(name,quantity);
        var matches=Regex.Matches(name,@"\s+");
        for(var index=matches.Count-1;index>=0;index--)
        {
            var split=matches[index];var prefix=name[..split.Index].Trim();var remainder=name[(split.Index+split.Length)..].Trim();
            if(remainder.Length==0||!ShoppingQuantity.TryParse(prefix,out _))continue;
            // Unknown numeric descriptions (e.g. 7 spice mix, 2% milk) remain intact.
            // A plain count is allowed when followed by a normal item name.
            if(Regex.IsMatch(remainder,@"^(?:%|[x×]\b|[\d/.,])",RegexOptions.IgnoreCase))continue;
            return new(remainder,string.IsNullOrWhiteSpace(quantity)?ShoppingQuantity.Normalize(prefix):quantity);
        }
        return new(name,quantity);
    }
}
