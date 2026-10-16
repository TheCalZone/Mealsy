using System.Globalization;
using System.Text.RegularExpressions;

namespace RecipeApp.Services;

/// <summary>Conservative arithmetic: never infer ingredient weights from volumes or package sizes.</summary>
public static class ShoppingQuantity
{
    public record Amount(decimal Value, string Unit);
    private static readonly Dictionary<char, string> fractions = new() { ['½']="1/2", ['¼']="1/4", ['¾']="3/4", ['⅓']="1/3", ['⅔']="2/3", ['⅛']="1/8", ['⅜']="3/8", ['⅝']="5/8", ['⅞']="7/8" };
    private static readonly Dictionary<string, (string Unit, decimal Factor)> units = new(StringComparer.OrdinalIgnoreCase)
    {
        [""]=("",1), ["g"]=("g",1), ["gram"]=("g",1), ["grams"]=("g",1), ["kg"]=("g",1000), ["kilogram"]=("g",1000), ["kilograms"]=("g",1000),
        ["ml"]=("ml",1), ["millilitre"]=("ml",1), ["millilitres"]=("ml",1), ["milliliter"]=("ml",1), ["milliliters"]=("ml",1), ["l"]=("ml",1000), ["litre"]=("ml",1000), ["litres"]=("ml",1000), ["liter"]=("ml",1000), ["liters"]=("ml",1000),
        ["tsp"]=("tsp",1), ["teaspoon"]=("tsp",1), ["teaspoons"]=("tsp",1), ["tbsp"]=("tbsp",1), ["tablespoon"]=("tbsp",1), ["tablespoons"]=("tbsp",1),
        ["cup"]=("cups",1), ["cups"]=("cups",1), ["oz"]=("oz",1), ["ounce"]=("oz",1), ["ounces"]=("oz",1), ["lb"]=("lb",1), ["lbs"]=("lb",1),
        ["clove"]=("cloves",1), ["cloves"]=("cloves",1), ["tin"]=("tins",1), ["tins"]=("tins",1), ["can"]=("cans",1), ["cans"]=("cans",1), ["pack"]=("packs",1), ["packs"]=("packs",1), ["bunch"]=("bunches",1), ["bunches"]=("bunches",1), ["pinch"]=("pinches",1), ["pinches"]=("pinches",1), ["jar"]=("jars",1), ["jars"]=("jars",1), ["bottle"]=("bottles",1), ["bottles"]=("bottles",1), ["bag"]=("bags",1), ["bags"]=("bags",1), ["packet"]=("packets",1), ["packets"]=("packets",1)
    };
    public static string Identity(string value) => Regex.Replace((value ?? "").Trim(), @"\s+", " ").ToUpperInvariant();
    public static bool TryParse(string text, out Amount result)
    {
        result = null; var input = (text ?? "").Trim();
        foreach (var fraction in fractions) input = Regex.Replace(input, @"(?<=\d)" + fraction.Key, " " + fraction.Value).Replace(fraction.Key.ToString(), fraction.Value);
        // A comma can be a decimal or a thousands separator. Leave ambiguous values for review.
        if (Regex.IsMatch(input, @"\d,[0-9]{3}(?:\D|$)")) return false;
        input = input.Replace(',', '.');
        var match = Regex.Match(input, @"^(?<n>\d+\s+\d+/\d+|\d+/\d+|\d+(?:\.\d+)?)\s*(?<u>.*)$");
        if (!match.Success || !Number(match.Groups["n"].Value, out var n)) return false;
        var unit = match.Groups["u"].Value.Trim().TrimEnd('.');
        if (unit.StartsWith('x') || unit.StartsWith('X') || unit.StartsWith('×'))
        {
            if (!TryParse(unit[1..].Trim(), out var size) || size.Value <= 0 || size.Unit.StartsWith("×")) return false;
            result = new(n, "× " + Format(size,true)); return true;
        }
        if (!units.TryGetValue(unit, out var definition)) return false;
        try { result = new(checked(n * definition.Factor), definition.Unit); return true; } catch (OverflowException) { return false; }
    }
    private static bool Number(string text, out decimal value)
    {
        value = 0;
        try
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var fraction = part.Split('/');
                if (!decimal.TryParse(fraction[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n)) return false;
                if (fraction.Length > 1) { if (!decimal.TryParse(fraction[1], NumberStyles.None, CultureInfo.InvariantCulture, out var d) || d == 0) return false; n /= d; }
                value = checked(value + n);
            }
            return true;
        }
        catch (OverflowException) { return false; }
    }
    private static string DecimalText(decimal n) => n.ToString(n > 0 && n < 0.001m ? "0.############################" : "0.###", CultureInfo.InvariantCulture);
    public static string Format(Amount amount, bool precise=false)
    {
        var (n, unit) = amount;
        if (unit == "g" && n >= 1000) { n /= 1000; unit = "kg"; }
        if (unit == "ml" && n >= 1000) { n /= 1000; unit = "L"; }
        var number=precise ? n.ToString("0.############################",CultureInfo.InvariantCulture) : DecimalText(n);
        if (number=="1") unit=unit switch { "cups"=>"cup", "cloves"=>"clove", "tins"=>"tin", "cans"=>"can", "packs"=>"pack", "bunches"=>"bunch", "pinches"=>"pinch", "jars"=>"jar", "bottles"=>"bottle", "bags"=>"bag", "packets"=>"packet", _=>unit };
        return number + (unit.Length == 0 ? "" : " " + unit);
    }
    public static string Normalize(string text) => TryParse(text, out var amount) ? Format(amount) : (text ?? "").Trim();
    public static string Scale(string text, decimal factor)
    {
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        if (string.IsNullOrWhiteSpace(text)) return "";
        if (TryParse(text, out var amount)) return Format(amount with { Value = checked(amount.Value * factor) },true);
        return factor == 1 ? text.Trim() : $"{DecimalText(factor)} × ({text.Trim()})";
    }
    public static bool TryAdd(string left, string right, out string combined)
    {
        combined = null;
        if (!TryParse(left, out var a) || !TryParse(right, out var b) || a.Unit != b.Unit) return false;
        try { combined = Format(a with { Value = checked(a.Value + b.Value) }); return true; } catch (OverflowException) { return false; }
    }
}
