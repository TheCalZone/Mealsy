using RecipeApp.Models;
using System.Text.Json;

namespace RecipeApp.Services;

public record ShoppingItemChange(ShoppingListItem before, ShoppingListItem after);
public record ShoppingSyncResult(List<ShoppingListItem> Items,int RequestedCount=0);
public class ShoppingSyncConflictException : Exception { }

/// <summary>Computes changes against the last acknowledged server state, including deletions.</summary>
public static class ShoppingListSync
{
    // Attribution belongs to the server and is not a user-editable shopping field.
    private static bool SameContent(ShoppingListItem a,ShoppingListItem b) => a?.Id==b?.Id && a?.Name==b?.Name && a?.Category==b?.Category && a?.Quantity==b?.Quantity && a?.Note==b?.Note && a?.InBasket==b?.InBasket;
    public static List<ShoppingItemChange> Changes(string baseline, string current)
    {
        var before = ShoppingListDocument.Deserialize(baseline).Items.ToDictionary(i => i.Id);
        var after = ShoppingListDocument.Deserialize(current).Items.ToDictionary(i => i.Id);
        return before.Keys.Union(after.Keys).Select(id => new ShoppingItemChange(before.GetValueOrDefault(id), after.GetValueOrDefault(id)))
            .Where(c => !SameContent(c.before,c.after)).ToList();
    }

    // While the request was in flight, the user may have continued typing. Reapply only
    // those newer edits onto the acknowledged response, preserving other members' additions.
    public static ShoppingListDocument Reapply(string sent, string latest, string server)
    {
        var result = ShoppingListDocument.Deserialize(server).Items.ToDictionary(i => i.Id);
        foreach (var change in Changes(sent, latest))
        {
            var id = (change.after ?? change.before).Id;
            result.TryGetValue(id, out var remote);
            if (change.after == null)
            {
                if (remote != null && !SameContent(remote,change.before)) throw new ShoppingSyncConflictException();
                result.Remove(id); continue;
            }
            if (change.before == null)
            {
                if (remote != null && !SameContent(remote,change.after)) throw new ShoppingSyncConflictException();
                result[id] = remote ?? change.after; continue;
            }
            if (remote == null) throw new ShoppingSyncConflictException();
            static bool Conflicts<T>(T before, T after, T remote) => !Equals(before, after) && !Equals(remote, before) && !Equals(remote, after);
            if (Conflicts(change.before.Name, change.after.Name, remote.Name)
                || Conflicts(change.before.Category, change.after.Category, remote.Category)
                || Conflicts(change.before.Quantity, change.after.Quantity, remote.Quantity)
                || Conflicts(change.before.Note, change.after.Note, remote.Note)
                || Conflicts(change.before.InBasket, change.after.InBasket, remote.InBasket)) throw new ShoppingSyncConflictException();
            if (change.after.Name != change.before.Name) remote.Name = change.after.Name;
            if (change.after.Category != change.before.Category) remote.Category = change.after.Category;
            if (change.after.Quantity != change.before.Quantity) remote.Quantity = change.after.Quantity;
            if (change.after.Note != change.before.Note) remote.Note = change.after.Note;
            if (change.after.InBasket != change.before.InBasket) remote.InBasket = change.after.InBasket;
        }
        return ShoppingListDocument.Deserialize(JsonSerializer.Serialize(result.Values));
    }
}
