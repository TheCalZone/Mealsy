using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;

public class ShoppingListSyncTests
{
    private static ShoppingListDocument List(params ShoppingListItem[] items)
    {
        var result = new ShoppingListDocument(); foreach (var item in items) result.Add(item); return result;
    }
    [Fact]
    public void ChangesIncludeDeletionWithoutTreatingFilteredItemsAsRemoved()
    {
        var first = new ShoppingListItem { Name = "Milk" };
        var second = new ShoppingListItem { Name = "Rice" };
        var baseline = List(first, second).Serialize();
        var after = ShoppingListDocument.Deserialize(baseline);
        _ = after.Visible("Milk", true).ToArray();
        Assert.Empty(ShoppingListSync.Changes(baseline, after.Serialize()));
        after.Remove(first.Id);
        var deletion = Assert.Single(ShoppingListSync.Changes(baseline, after.Serialize()));
        Assert.Equal(first.Id, deletion.before.Id); Assert.Null(deletion.after);
    }
    [Fact]
    public void EditsDuringRequestPreserveServerAdditionsAndOtherFields()
    {
        var milk = new ShoppingListItem { Name = "Milk", Quantity = "1 L" };
        var sent = List(milk).Serialize();
        var latest = ShoppingListDocument.Deserialize(sent); latest.Items[0].Note = "Oat";
        var server = ShoppingListDocument.Deserialize(sent); server.Items[0].InBasket = true;
        server.Add(new() { Name = "Rice" });
        var result = ShoppingListSync.Reapply(sent, latest.Serialize(), server.Serialize());
        Assert.Equal(2, result.Items.Count);
        var merged = Assert.Single(result.Items, i => i.Id == milk.Id);
        Assert.Equal("Oat", merged.Note); Assert.True(merged.InBasket);
    }
    [Fact]
    public void EditsAndDeletionsDuringRequestRemainPendingAgainstAcknowledgedState()
    {
        var milk = new ShoppingListItem { Name = "Milk" };
        var sent = List(milk).Serialize();
        var latest = List(new ShoppingListItem { Name = "Bread" }).Serialize();
        var server = ShoppingListDocument.Deserialize(sent); server.Add(new() { Name = "Rice" });
        var merged = ShoppingListSync.Reapply(sent, latest, server.Serialize());
        Assert.Equal(new[] { "Bread", "Rice" }, merged.Items.Select(i => i.Name).OrderBy(n => n));
        Assert.Equal(2, ShoppingListSync.Changes(server.Serialize(), merged.Serialize()).Count);
    }
    [Fact]
    public void EditDuringRequestCannotResurrectAnItemAnotherMemberDeleted()
    {
        var sent = List(new ShoppingListItem { Name = "Milk" }).Serialize();
        var latest = ShoppingListDocument.Deserialize(sent); latest.Items[0].Quantity = "2 L";
        Assert.Throws<ShoppingSyncConflictException>(() => ShoppingListSync.Reapply(sent, latest.Serialize(), "[]"));
    }
    [Fact]
    public void DeleteDuringRequestCannotEraseAnItemAnotherMemberEdited()
    {
        var sent = List(new ShoppingListItem { Name = "Milk" }).Serialize();
        var server = ShoppingListDocument.Deserialize(sent); server.Items[0].Note = "Oat";
        Assert.Throws<ShoppingSyncConflictException>(() => ShoppingListSync.Reapply(sent, "[]", server.Serialize()));
    }
    [Fact]
    public void CompetingEditsDuringRequestAreKeptForExplicitResolution()
    {
        var sent = List(new ShoppingListItem { Name = "Milk", Quantity = "1 L" }).Serialize();
        var latest = ShoppingListDocument.Deserialize(sent); latest.Items[0].Quantity = "2 L";
        var server = ShoppingListDocument.Deserialize(sent); server.Items[0].Quantity = "3 L";
        Assert.Throws<ShoppingSyncConflictException>(() => ShoppingListSync.Reapply(sent, latest.Serialize(), server.Serialize()));
    }
}
