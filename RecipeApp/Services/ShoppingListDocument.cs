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
        foreach (var raw in PastedListText.Lines(text))
        {
            var line = PastedListText.Clean(raw, out var collected);
            if (line.Length == 0) continue;
            // Recognize exported category headings, but don't drop a bracket-wrapped item.
            if ((PastedListText.IsBracketHeading(raw) && new[] { "Produce", "Pantry", "Drinks", "Meat", "Dairy", "Bakery", "Frozen" }.Contains(CategoryName(line))) ||
                raw.Trim().Equals("[Other]", StringComparison.OrdinalIgnoreCase) || line.EndsWith(':'))
            {
                category = CategoryName(line.TrimEnd(':'));
                continue;
            }
            if (line.Length > 0)
            {
                var item=new ShoppingListItem { Name=line, Category=category, InBasket=collected };
                var separator=line.IndexOf(" — ",StringComparison.Ordinal);
                if (separator>0)
                {
                    item.Name=line[..separator].Trim(); var detail=line[(separator+3)..].Trim();
                    var note=System.Text.RegularExpressions.Regex.Match(detail,@"^(.*?) \((.*)\)$");
                    item.Quantity=note.Success ? note.Groups[1].Value : detail;
                    item.Note=note.Success ? note.Groups[2].Value : "";
                }
                result.Add(item);
            }
        }
        return result;
    }

    public void AppendText(string text)
    {
        var additions = ParseText(text);
        foreach (var item in additions) MergeItem(items,item);
        Commit();
    }

    public static List<ShoppingListItem> FromIngredients(IEnumerable<Ingredient> ingredients)
    {
        var rows = new List<(ShoppingListItem Item, ShoppingQuantity.Amount Amount)>();
        foreach (var i in ingredients ?? Enumerable.Empty<Ingredient>())
        {
            if (string.IsNullOrWhiteSpace(i?.Name)) continue;
            var item = new ShoppingListItem { Name=i.Name.Trim(), Category=CategoryName(i.Tag), Quantity=i.Quantity?.Trim() ?? "", Note=i.Note?.Trim() ?? "", InBasket=i.InBasket };
            ShoppingQuantity.TryParse(item.Quantity,out var amount);
            var merged=false;
            for (var index=0; index<rows.Count; index++)
            {
                var existing=rows[index];
                if (!SameItem(existing.Item,item) || existing.Item.InBasket!=item.InBasket) continue;
                if (amount!=null && existing.Amount?.Unit==amount.Unit)
                {
                    try { rows[index]=(existing.Item,existing.Amount with { Value=checked(existing.Amount.Value+amount.Value) }); merged=true; break; }
                    catch (OverflowException) { }
                }
                if (string.IsNullOrWhiteSpace(existing.Item.Quantity) && string.IsNullOrWhiteSpace(item.Quantity)) { merged=true; break; }
            }
            if (!merged) rows.Add((item,amount));
        }
        // Round only after the full import has been added, not once per ingredient.
        foreach (var row in rows) if (row.Amount!=null) row.Item.Quantity=ShoppingQuantity.Format(row.Amount);
        return rows.Select(r => r.Item).ToList();
    }

    private static bool SameItem(ShoppingListItem a, ShoppingListItem b) =>
        ShoppingQuantity.Identity(a.Name) == ShoppingQuantity.Identity(b.Name) &&
        ShoppingQuantity.Identity(a.Category) == ShoppingQuantity.Identity(b.Category) &&
        ShoppingQuantity.Identity(a.Note) == ShoppingQuantity.Identity(b.Note);
    private static void MergeItem(List<ShoppingListItem> target, ShoppingListItem item)
    {
        // Collected items belong to a previous purchase; never increase their amount.
        foreach (var existing in target.Where(e => SameItem(e, item) && e.InBasket == item.InBasket))
        {
            if (ShoppingQuantity.TryAdd(existing.Quantity, item.Quantity, out var total)) { existing.Quantity = total; return; }
            if (string.IsNullOrWhiteSpace(existing.Quantity) && string.IsNullOrWhiteSpace(item.Quantity)) return;
        }
        target.Add(item);
    }
    public void AppendIngredients(IEnumerable<Ingredient> ingredients)
    {
        foreach (var item in FromIngredients(ingredients)) { item.InBasket = false; MergeItem(items, item); }
        Commit();
    }

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
