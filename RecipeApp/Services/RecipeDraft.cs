using MvvmHelpers;
using RecipeApp.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RecipeApp.Services;

public class RecipeStep : ObservableObject
{
    private string text = "";
    public string Text { get => text; set => SetProperty(ref text, value); }
}
public class RecipeSection : ObservableObject
{
    private string name = "Ingredients";
    public string Name { get => name; set => SetProperty(ref name, value); }
    public ObservableCollection<Ingredient> Items { get; set; } = new();
}

/// <summary>A detached working copy. Editing and draft recovery never modify saved recipes.</summary>
public class RecipeDraft
{
    public Recipe Recipe { get; set; } = new();
    public string Servings { get; set; } = "2";
    public string PrepMinutes { get; set; } = "";
    public string CookMinutes { get; set; } = "";
    public string Rating { get; set; } = "";
    public ObservableCollection<RecipeSection> Sections { get; set; } = new() { new() };
    public ObservableCollection<RecipeStep> Steps { get; set; } = new() { new() };
    public DateTimeOffset OriginalUpdatedAt { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public string PendingPaste { get; set; } = "";
    public int IngredientCount => Sections.Sum(s => s.Items.Count(i => !string.IsNullOrWhiteSpace(i.Name)));

    public static RecipeDraft FromRecipe(Recipe source)
    {
        var draft = new RecipeDraft { Recipe = Copy(source), OriginalUpdatedAt = source.LastUpdated,
            Servings = Math.Max(1, source.Serving).ToString(), PrepMinutes = source.PrepTime == 0 ? "" : source.PrepTime.ToString(),
            CookMinutes = source.CookTime == 0 ? "" : source.CookTime.ToString(), Rating = source.Rating == 0 ? "" : source.Rating.ToString(),
            Steps = new(ParseSteps(source.RecipeMethod).Select(s => new RecipeStep { Text = s })), Sections = new() };
        foreach (var group in (source.Ingredients ?? new()).GroupBy(i => string.IsNullOrWhiteSpace(i.IngredientSection) ? "Ingredients" : i.IngredientSection))
            draft.Sections.Add(new() { Name = group.Key, Items = new(group.Select(CopyIngredient)) });
        if (draft.Sections.Count == 0) draft.Sections.Add(new());
        if (draft.Steps.Count == 0) draft.Steps.Add(new());
        draft.Recipe.Tags = string.Join(", ", (source.Tags ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Where(t => !t.Equals("all", StringComparison.OrdinalIgnoreCase)));
        return draft;
    }
    public static Recipe Copy(Recipe r) => new() { Id = r.Id, FirestoreID = r.FirestoreID, FirebaseUID = r.FirebaseUID, LastUpdated = r.LastUpdated,
        RecipeName = r.RecipeName, RecipeMethod = r.RecipeMethod, RecipeImage = r.RecipeImage, Description = r.Description,
        CookTime = r.CookTime, SourceUrl = r.SourceUrl, Notes = r.Notes, Serving = r.Serving, PrepTime = r.PrepTime, Rating = r.Rating, Tags = r.Tags,
        Ingredients = (r.Ingredients ?? new()).Select(CopyIngredient).ToList() };
    public static Ingredient CopyIngredient(Ingredient i) => new() { Id = i.Id, RecipeId = i.RecipeId, FirestoreId = i.FirestoreId,
        Name = i.Name, Quantity = i.Quantity, Tag = i.Tag, Note = i.Note, IngredientSection = i.IngredientSection, MainIngredient = i.MainIngredient, InBasket = i.InBasket };

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Recipe.RecipeName) || Recipe.RecipeName.Trim().Length > 160) errors.Add("Give the recipe a name (up to 160 characters).");
        CheckNumber(Servings, 1, 1000, "Servings", false, errors);
        CheckNumber(PrepMinutes, 0, 10000, "Prep time", true, errors); CheckNumber(CookMinutes, 0, 10000, "Cook time", true, errors);
        CheckNumber(Rating, 0, 5, "Rating", true, errors);
        if ((Recipe.Description ?? "").Length > 500) errors.Add("Keep the introduction within 500 characters.");
        if ((Recipe.Notes ?? "").Length > 5000) errors.Add("Keep private notes within 5,000 characters.");
        if (!string.IsNullOrWhiteSpace(Recipe.SourceUrl) && (!Uri.TryCreate(Recipe.SourceUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || Recipe.SourceUrl.Length > 2000)) errors.Add("Use a complete http/https source link without embedded login details.");
        if (IngredientCount == 0) errors.Add("Add at least one ingredient.");
        if (Sections.Sum(s => s.Items.Count) > 100) errors.Add("Use no more than 100 ingredient rows.");
        if (Sections.Select(s => s.Name?.Trim() ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count() != Sections.Count) errors.Add("Give each ingredient section a different name.");
        foreach (var section in Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Name) || section.Name.Length > 100) errors.Add("Give each ingredient section a name (up to 100 characters).");
            foreach (var i in section.Items)
            {
                if (string.IsNullOrWhiteSpace(i.Name)) errors.Add("Name or remove each empty ingredient row.");
                if ((i.Name ?? "").Length > 200 || (i.Quantity ?? "").Length > 200 || (i.Tag ?? "").Length > 100 || (i.Note ?? "").Length > 500) errors.Add("Shorten the ingredient name, quantity, category or note.");
            }
        }
        if (!Steps.Any(s => !string.IsNullOrWhiteSpace(s.Text))) errors.Add("Add at least one method step.");
        if (Steps.Count > 100 || Method().Length > 15000) errors.Add("Keep the method within 100 steps and 15,000 characters.");
        return errors.Distinct().ToList();
    }
    private static void CheckNumber(string text, int min, int max, string label, bool optional, List<string> errors)
    {
        if (optional && string.IsNullOrWhiteSpace(text)) return;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < min || n > max) errors.Add($"{label} must be a whole number from {min} to {max}.");
    }
    public string Method() => string.Join("\n\n", Steps.Where(s => !string.IsNullOrWhiteSpace(s.Text)).Select((s, n) => $"{n + 1}. {s.Text.Trim()}"));
    public Recipe Build()
    {
        var errors = Validate(); if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        var result = Copy(Recipe);
        result.RecipeName = result.RecipeName.Trim(); result.Description = result.Description?.Trim() ?? "";
        result.SourceUrl = result.SourceUrl?.Trim() ?? ""; result.Notes = result.Notes?.Trim() ?? "";
        result.Serving = int.Parse(Servings); result.PrepTime = int.TryParse(PrepMinutes, out var p) ? p : 0;
        result.CookTime = int.TryParse(CookMinutes, out var c) ? c : 0; result.Rating = int.TryParse(Rating, out var r) ? r : 0;
        result.Tags = NormalizeTags(result.Tags); result.RecipeMethod = Method();
        result.Ingredients = Sections.SelectMany(section => section.Items.Select(i => {
            var copy = CopyIngredient(i); copy.Name = copy.Name.Trim(); copy.Quantity = copy.Quantity?.Trim() ?? "";
            copy.Tag = ShoppingListDocument.CategoryName(copy.Tag); copy.IngredientSection = section.Name.Trim(); copy.Note = copy.Note?.Trim() ?? ""; return copy;
        })).ToList();
        result.FirebaseIngredients = result.Ingredients;
        return result;
    }
    public static string NormalizeTags(string text) => string.Join(",", (text ?? "").Split(new[] { ',', ';', '\n', '#' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Append("all").Distinct(StringComparer.OrdinalIgnoreCase));
    public static List<string> ParseSteps(string text) => Regex.Split((text ?? "").Replace("\r", ""), @"\n\s*\n|\n(?=\s*\d+[.)]\s)")
        .SelectMany(s => s.Contains('\n') ? s.Split('\n') : new[] { s })
        .Select(s => Regex.Replace(s.Trim(), @"^(?:step\s+)?\d+[.)\:]\s*", "", RegexOptions.IgnoreCase)).Where(s => s.Length > 0).ToList();

    public static List<RecipeSection> ParseIngredients(string text)
    {
        var result = new List<RecipeSection>(); var section = new RecipeSection(); result.Add(section);
        var number = @"(?:\d+\s*[¼½¾⅓⅔⅛⅜⅝⅞]|\d+\s+\d+/\d+|\d+/\d+|\d+(?:[.,]\d+)?|[¼½¾⅓⅔⅛⅜⅝⅞])";
        var pattern = @"^(?<qty>" + number + @"(?:\s*[x×]\s*" + number + @")?\s*(?:(?:tablespoons?|teaspoons?|cups?|pounds?|ounces?|tbsp|tsp|lbs?|oz|kg|mg|g|ml|litres?|liters?|l)(?=\s|$))?)\s+(?<name>.+)$";
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var line = Regex.Replace(raw.Trim(), @"^(?:[-•*]\s+|\[[ xX]\]\s+)", ""); if (line.Length == 0) continue;
            if (line.StartsWith('[') && line.EndsWith(']') || line.EndsWith(':') && line.Length < 101)
            {
                var name = line.Trim('[', ']', ':').Trim();
                if (name.Length > 0) { section = new() { Name = name }; result.Add(section); } continue;
            }
            var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
            var nameAndNote = match.Success ? match.Groups["name"].Value : line;
            var parts = nameAndNote.Split(',', 2);
            section.Items.Add(new() { Name = parts[0].Trim(), Quantity = match.Success ? match.Groups["qty"].Value.Trim() : "",
                Note = parts.Length > 1 ? parts[1].Trim() : "", MainIngredient = true, Tag = "Other", IngredientSection = section.Name });
        }
        return result.Where(s => s.Items.Count > 0).ToList();
    }
}
