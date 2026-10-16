using RecipeApp.Models;
using System.Text;
using System.Text.Json;

namespace RecipeApp.Services;

/// <summary>The complete list, independent of filtered UI views and device storage.</summary>
public class ShoppingListDocument
{
    private readonly Stack<string> undo = new();
    private readonly Stack<string> redo = new();
    private List<ShoppingListItem> items = new();
    private string snapshot = "[]";
    public IReadOnlyList<ShoppingListItem> Items => items;
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public string Serialize() => JsonSerializer.Serialize(items);

    public static string CategoryName(string category) => (category ?? "").Trim().ToLowerInvariant() switch
    {
        "" or "." or "other" => "Other",
        "veg" or "vegetables" or "fruits" or "produce" => "Produce",
        "tins" or "tins & cans" or "pantry" => "Pantry",
        "beverages" or "drinks" => "Drinks",
        "meat" or "meat & poultry" => "Meat",
        "dairy" => "Dairy", "bakery" => "Bakery", "frozen" => "Frozen",
        _ => category.Trim()
    };

    public static ShoppingListDocument Deserialize(string json)
    {
        var document = new ShoppingListDocument();
        document.Restore(json);
        return document;
    }

    private void Restore(string json)
    {
        // Deserialize fully before replacing the live list. Malformed data stays recoverable.
        var restored = JsonSerializer.Deserialize<List<ShoppingListItem>>(json)
            ?? throw new JsonException("Missing shopping list.");
        var seen = new HashSet<Guid>();
        foreach (var item in restored.Where(i => i != null))
        {
            if (item.Id == Guid.Empty || !seen.Add(item.Id))
            {
                item.Id = Guid.NewGuid();
                seen.Add(item.Id);
            }
            item.Name = item.Name?.Trim() ?? "";
            item.Category = CategoryName(item.Category);
            item.Quantity ??= "";
            item.Note ??= "";
        }
        items = restored.Where(i => i != null && !string.IsNullOrWhiteSpace(i.Name)).ToList();
        snapshot = Serialize();
    }

    public void Commit()
    {
        var current = Serialize();
        if (current == snapshot) return;
        undo.Push(snapshot);
        // Bound history to the most recent 30 changes.
        if (undo.Count > 30)
        {
            var recent = undo.Take(30).Reverse().ToArray();
            undo.Clear();
            foreach (var entry in recent) undo.Push(entry);
        }
        redo.Clear();
        snapshot = current;
    }

    public void Add(ShoppingListItem item)
    {
        if (string.IsNullOrWhiteSpace(item?.Name)) return;
        item.Name = item.Name.Trim();
        item.Category = CategoryName(item.Category);
        items.Add(item);
        Commit();
    }

    public void Replace(IEnumerable<ShoppingListItem> replacement)
    {
        items = replacement.Where(i => i != null && !string.IsNullOrWhiteSpace(i.Name)).ToList();
        foreach (var item in items) item.Category = CategoryName(item.Category);
        Commit();
    }

    public void Remove(Guid id) { items.RemoveAll(i => i.Id == id); Commit(); }
    public void ClearCollected() { items.RemoveAll(i => i.InBasket); Commit(); }
    public void Clear() { items.Clear(); Commit(); }
    public void Undo()
    {
        if (!CanUndo) return;
        redo.Push(snapshot);
        Restore(undo.Pop());
    }
    public void Redo()
    {
        if (!CanRedo) return;
        undo.Push(snapshot);
        Restore(redo.Pop());
    }

    public IEnumerable<ShoppingListItem> Visible(string search, bool hideCollected)
    {
        var query = search?.Trim() ?? "";
        return items.Where(i => (!hideCollected || !i.InBasket) &&
            (query.Length == 0 || new[] { i.Name, i.Category, i.Quantity, i.Note }
                .Any(value => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)));
    }

    public static List<ShoppingListItem> ParseText(string text)
    {
        var result = new List<ShoppingListItem>();
        var category = "Other";
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                category = CategoryName(line[1..^1]);
                continue;
            }
            var collected = line.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase);
            if (collected || line.StartsWith("[ ] ")) line = line[4..].Trim();
            else if (line.StartsWith("- ") || line.StartsWith("• ")) line = line[2..].Trim();
            if (line.Length > 0) result.Add(new() { Name = line, Category = category, InBasket = collected });
        }
        return result;
    }

    public void AppendText(string text)
    {
        var additions = ParseText(text);
        foreach (var item in additions)
            if (!items.Any(existing => existing.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)
                && existing.Category.Equals(item.Category, StringComparison.OrdinalIgnoreCase))) items.Add(item);
        Commit();
    }

    public static List<ShoppingListItem> FromIngredients(IEnumerable<Ingredient> ingredients) => ingredients
        .Where(i => !string.IsNullOrWhiteSpace(i?.Name))
        .GroupBy(i => (i.Name.Trim().ToUpperInvariant(), CategoryName(i.Tag).ToUpperInvariant(),
            (i.Quantity ?? "").Trim().ToUpperInvariant(), (i.Note ?? "").Trim().ToUpperInvariant()))
        .Select(g =>
        {
            var first = g.First();
            var quantity = (first.Quantity ?? "").Trim();
            return new ShoppingListItem
            {
                Name = first.Name.Trim(), Category = CategoryName(first.Tag), Note = first.Note ?? "",
                Quantity = g.Count() > 1 ? (quantity.Length == 0 ? g.Count().ToString() : $"{g.Count()} × {quantity}") : quantity,
                InBasket = g.All(i => i.InBasket)
            };
        }).ToList();

    public void MergeLegacyText(string text)
    {
        var parsed = ParseText(text);
        foreach (var item in parsed)
        {
            var existing = items.FirstOrDefault(i => i.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)
                && i.Category.Equals(item.Category, StringComparison.OrdinalIgnoreCase));
            if (existing == null) continue;
            item.Id = existing.Id;
            item.InBasket = existing.InBasket;
            item.Quantity = existing.Quantity;
            item.Note = existing.Note;
        }
        Replace(parsed);
    }

    public string PlainText(bool includeDetails = true, bool remainingOnly = false)
    {
        var builder = new StringBuilder();
        foreach (var group in items.Where(i => !remainingOnly || !i.InBasket).GroupBy(i => i.Category).OrderBy(g => g.Key))
        {
            builder.AppendLine($"[{group.Key}]");
            foreach (var item in group)
            {
                var detail = includeDetails && item.HasQuantity ? $" — {item.Quantity}" : "";
                var note = includeDetails && item.HasNote ? $" ({item.Note})" : "";
                builder.AppendLine(item.Name + detail + note);
            }
            builder.AppendLine();
        }
        return builder.ToString().TrimEnd();
    }
}
