using RecipeApp.Models;
using System.Text;

namespace RecipeApp.Services;

public static class RecipePresentation
{
    public static string TimeText(Recipe r) => $"Serves {r.Serving}" + (r.PrepTime > 0 ? $" · Prep {r.PrepTime} min" : "") + (r.CookTime > 0 ? $" · Cook {r.CookTime} min" : "") + (r.PrepTime > 0 && r.CookTime > 0 ? $" · Total {r.TotalTime} min" : "");
    public static string PlainText(Recipe r)
    {
        var text = new StringBuilder().AppendLine(r.RecipeName).AppendLine(r.Description).AppendLine(TimeText(r)).AppendLine();
        foreach (var section in r.Ingredients.GroupBy(i => string.IsNullOrWhiteSpace(i.IngredientSection) ? "Ingredients" : i.IngredientSection))
        {
            text.AppendLine($"[{section.Key}]");
            foreach (var i in section) text.AppendLine($"• {i.Quantity} {i.Name}" + (string.IsNullOrWhiteSpace(i.Note) ? "" : $" — {i.Note}"));
            text.AppendLine();
        }
        text.AppendLine(r.RecipeMethod); return text.ToString().Trim();
    }
    public static Recipe Preview(RecipeDraft draft)
    {
        var recipe = RecipeDraft.Copy(draft.Recipe);
        recipe.RecipeName = string.IsNullOrWhiteSpace(recipe.RecipeName) ? "Untitled recipe" : recipe.RecipeName;
        recipe.Serving = int.TryParse(draft.Servings, out var s) ? s : 0; recipe.PrepTime = int.TryParse(draft.PrepMinutes, out var p) ? p : 0;
        recipe.CookTime = int.TryParse(draft.CookMinutes, out var c) ? c : 0;
        recipe.RecipeMethod = draft.Method(); recipe.Ingredients = draft.Sections.SelectMany(section => section.Items.Select(i => {
            var copy = RecipeDraft.CopyIngredient(i); copy.IngredientSection = section.Name; return copy;
        })).ToList(); return recipe;
    }
}
