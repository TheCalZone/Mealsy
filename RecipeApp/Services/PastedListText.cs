using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace RecipeApp.Services;

/// <summary>Remove clipboard list decoration without damaging food names or quantities.</summary>
public static class PastedListText
{
    public static IEnumerable<string> Lines(string text) => (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u2028', '\n').Replace('\u2029', '\n').Split('\n');

    public static string Clean(string raw, out bool collected)
    {
        var builder = new StringBuilder();
        foreach (var c in raw ?? "")
        {
            var category = char.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Format or UnicodeCategory.PrivateUse || c is '\uFE0E' or '\uFE0F') continue;
            builder.Append(char.IsWhiteSpace(c) ? ' ' : c);
        }
        var line = builder.ToString().Trim();
        collected = false;
        // Nested Markdown bullets/check boxes and Notes' Unicode checks can be copied together.
        while (line.Length > 0)
        {
            var check = Regex.Match(line, @"^\[\s*(?<state>[xX✓✔]?)\s*\]\s*");
            if (check.Success) { collected |= check.Groups["state"].Value.Length > 0; line = line[check.Length..].TrimStart(); continue; }
            if (line[0] == '\uFFFC') { line = line[1..].TrimStart(); continue; }
            if ("☐☑☒✓✔✅□▢⬜◻◽○◯◉●•‣◦▪▫".Contains(line[0]))
            {
                collected |= "☑☒✓✔✅◉".Contains(line[0]); line = line[1..].TrimStart(); continue;
            }
            var bullet = Regex.Match(line, @"^(?:[-–—+*]\s+|#{1,6}\s+|\d+[.)]\s+)");
            if (bullet.Success) { line = line[bullet.Length..].TrimStart(); continue; }
            break;
        }
        // Square brackets are decoration; retain their contents, parentheses, accents and fractions.
        line = line.Replace("[", "").Replace("]", "").Replace("**", "").Replace("__", "").Replace("`", "");
        return Regex.Replace(line, @"\s+", " ").Trim();
    }

    public static bool IsRecipeSection(string raw)
    {
        if (!IsBracketHeading(raw)) return false;
        var heading = Clean(raw, out _).TrimEnd(':').ToLowerInvariant();
        // Brackets alone aren't enough: [Milk] and [Eggs] must remain ingredients.
        return heading.EndsWith("ingredients") || heading.StartsWith("for the ") ||
            new[] { "ingredients", "sauce", "topping", "dressing", "marinade", "batter", "dough", "filling", "frosting", "icing", "garnish", "base", "to serve", "optional" }.Contains(heading);
    }

    public static bool IsBracketHeading(string raw)
    {
        var line = (raw ?? "").Trim();
        return line.StartsWith('[') && line.EndsWith(']') &&
            !Regex.IsMatch(line, @"^\[\s*[xX✓✔]?\s*\]$") &&
            !Regex.IsMatch(line, @"^\[\s*(?:\d|[¼½¾⅓⅔⅛⅜⅝⅞])");
    }
}
