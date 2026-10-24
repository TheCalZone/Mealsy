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

    [Fact]
    public void ServerAttributionDoesNotCreateChangesOrBlockInFlightEditsAndDeletion()
    {
        var item=new ShoppingListItem{Name="Milk"};var sent=List(item).Serialize();
        var server=ShoppingListDocument.Deserialize(sent);server.Items[0].AddedBy="adult";server.Items[0].AddedByName="Alex";server.Items[0].AdditionKind="added";
        Assert.Empty(ShoppingListSync.Changes(sent,server.Serialize()));
        var latest=ShoppingListDocument.Deserialize(sent);latest.Items[0].Quantity="2 L";
        var result=ShoppingListSync.Reapply(sent,latest.Serialize(),server.Serialize());
        Assert.Equal("adult",result.Items[0].AddedBy);Assert.Equal("2 L",result.Items[0].Quantity);
        Assert.Empty(ShoppingListSync.Reapply(sent,"[]",server.Serialize()).Items);
        var duplicate=ShoppingListSync.Reapply("[]",sent,server.Serialize());
        Assert.Equal("Alex",duplicate.Items[0].AddedByName);
    }
    [Fact]
    public void ContributorBadgeDistinguishesRequestsAndSurvivesSerialization()
    {
        var item=new ShoppingListItem{Name="Bread",AddedBy="child",AddedByName="Sam",AdditionKind="requested"};
        var copy=ShoppingListDocument.Deserialize(List(item).Serialize()).Items[0];
        Assert.True(copy.HasContributor);Assert.Equal("Requested by Sam",copy.ContributorLabel);Assert.Equal(item.ContributorColor,copy.ContributorColor);
        Assert.False(new ShoppingListItem{Name="Legacy"}.HasContributor);
    }

    [Fact]
    public void ClearCheckedSyncsOnlyDeletionsAndUndoRestoresContributorDetails()
    {
        var uncheckedItem=new ShoppingListItem{Name="Milk"};
        var checkedItem=new ShoppingListItem{Name="Bread",InBasket=true,AddedBy="alex",AddedByName="Alex",AdditionKind="added"};
        var list=List(uncheckedItem,checkedItem);var baseline=list.Serialize();
        list.ClearCollected();var change=Assert.Single(ShoppingListSync.Changes(baseline,list.Serialize()));
        Assert.Equal(checkedItem.Id,change.before.Id);Assert.Null(change.after);
        Assert.Equal(uncheckedItem.Id,Assert.Single(list.Items).Id);
        list.Undo();Assert.Empty(ShoppingListSync.Changes(baseline,list.Serialize()));
        Assert.Equal("Alex",list.Items.Single(i=>i.Id==checkedItem.Id).AddedByName);
    }
}
