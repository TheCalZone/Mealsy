using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
using Xunit;

public class ShoppingListTests
{
    [Fact]
    public void DeleteOnCheckIsOneUndoableChangeAndPreservesOtherCheckedItems()
    {
        var list=new ShoppingListDocument();
        var existing=new ShoppingListItem {Name="Bread",InBasket=true};
        var milk=new ShoppingListItem {Name="Milk",Quantity="2 L"};
        list.Add(existing);list.Add(milk);
        var baseline=list.Serialize();
        milk.InBasket=true;
        Assert.True(list.CommitCheckedItem(milk.Id,true));
        Assert.Equal(existing.Id,Assert.Single(list.Items).Id);
        var change=Assert.Single(ShoppingListSync.Changes(baseline,list.Serialize()));
        Assert.Equal(milk.Id,change.before.Id);Assert.Null(change.after);
        list.Undo();
        Assert.False(list.Items.Single(i=>i.Id==milk.Id).InBasket);
        Assert.Equal("2 L",list.Items.Single(i=>i.Id==milk.Id).Quantity);
        list.Redo();Assert.DoesNotContain(list.Items,i=>i.Id==milk.Id);
    }
    [Fact]
    public void KeepOnCheckRetainsItemAndHidingDoesNotDeleteIt()
    {
        var list=new ShoppingListDocument();var item=new ShoppingListItem {Name="Eggs"};list.Add(item);
        item.InBasket=true;
        Assert.False(list.CommitCheckedItem(item.Id,false));
        Assert.Empty(list.Visible("",true));Assert.Single(list.Items);
        list.Undo();Assert.False(Assert.Single(list.Items).InBasket);
    }

    [Fact]
    public void CollectedStateAndEditsSurviveSerialization()
    {
        var list = new ShoppingListDocument();
        var item = new ShoppingListItem { Name = "Milk", Quantity = "2 L", Category = "dairy", Note = "Oat", InBasket = true };
        list.Add(item);
        item.Name = "Oat milk";
        list.Commit();
        var restored = ShoppingListDocument.Deserialize(list.Serialize());
        var saved = Assert.Single(restored.Items);
        Assert.Equal(item.Id, saved.Id);
        Assert.Equal("Oat milk", saved.Name);
        Assert.Equal("Dairy", saved.Category);
        Assert.Equal("2 L", saved.Quantity);
        Assert.Equal("Oat", saved.Note);
        Assert.True(saved.InBasket);
    }

    [Fact]
    public void FilteredViewsCannotDiscardHiddenItems()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Apples", Category = "Veg", InBasket = true });
        list.Add(new() { Name = "Milk", Category = "Dairy", Note = "Oat" });
        Assert.Equal("Milk", Assert.Single(list.Visible("oat", true)).Name);
        Assert.Empty(list.Visible("Apples", true));
        Assert.Equal(2, list.Items.Count);
        Assert.Equal(2, ShoppingListDocument.Deserialize(list.Serialize()).Items.Count);
    }

    [Fact]
    public void RemovalAndClearingCanBeUndoneAndRedone()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Rice" });
        list.Add(new() { Name = "Beans", InBasket = true });
        list.ClearCollected();
        Assert.Equal("Rice", Assert.Single(list.Items).Name);
        list.Undo();
        Assert.True(list.Items.Single(i => i.Name == "Beans").InBasket);
        list.Redo();
        Assert.Single(list.Items);
        list.Clear();
        Assert.Empty(list.Items);
        list.Undo();
        Assert.Equal("Rice", Assert.Single(list.Items).Name);
        list.Remove(list.Items[0].Id);
        list.Undo();
        Assert.Single(list.Items);
    }

    [Fact]
    public void ANewEditInvalidatesRedoHistory()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Rice" });
        list.Undo();
        Assert.True(list.CanRedo);
        list.Add(new() { Name = "Pasta" });
        Assert.False(list.CanRedo);
    }

    [Fact]
    public void PasteHandlesLineEndingsHeadingsAndCheckboxesWithoutDuplicateItems()
    {
        var list = new ShoppingListDocument();
        list.AppendText("[Veg]\r\n- Apples\r\n[x] Pears\r\n\r\n[Pantry]\n• Rice\n");
        list.AppendText("[Produce]\nApples\n[Pantry]\nRice");
        Assert.Equal(3, list.Items.Count);
        Assert.Equal("Produce", list.Items.Single(i => i.Name == "Apples").Category);
        Assert.True(list.Items.Single(i => i.Name == "Pears").InBasket);
        list.AppendText("[Other]\r- Soap\r• Toothpaste");
        Assert.Equal(5,list.Items.Count);
        Assert.Contains(list.Items,i => i.Name=="Toothpaste");
    }

    [Fact]
    public void ExternalRecipeAppendsKeepCollectedStateQuantityAndNotes()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Apples", Category = "Produce", Quantity = "4", Note = "Green", InBasket = true });
        var externalText = list.PlainText(includeDetails: false) + "\nBananas";
        list.MergeLegacyText(externalText);
        var apples = list.Items.Single(i => i.Name == "Apples");
        Assert.True(apples.InBasket);
        Assert.Equal("4", apples.Quantity);
        Assert.Equal("Green", apples.Note);
        Assert.Contains(list.Items, i => i.Name == "Bananas");
    }

    [Fact]
    public void SharingRemainingItemsIncludesDetailsButExcludesCollectedItems()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Collected item", InBasket = true });
        list.Add(new() { Name = "Rice", Quantity = "500 g", Note = "Brown" });
        var text = list.PlainText(remainingOnly: true);
        Assert.Contains("Rice — 500 g (Brown)", text);
        Assert.DoesNotContain("Collected item", text);
    }

    [Fact]
    public void InvalidStorageIsRejectedInsteadOfSilentlyReplacingTheList()
    {
        Assert.Throws<JsonException>(() => ShoppingListDocument.Deserialize("not JSON"));
        Assert.Throws<JsonException>(() => ShoppingListDocument.Deserialize("null"));
    }

    [Fact]
    public void DirectCheckboxChangesParticipateInUndoHistory()
    {
        var list = new ShoppingListDocument();
        list.Add(new() { Name = "Milk" });
        list.Items[0].InBasket = true;
        list.Commit();
        list.Undo();
        Assert.False(list.Items[0].InBasket);
        list.Redo();
        Assert.True(list.Items[0].InBasket);
    }

    [Fact]
    public void RecipeImportConsolidatesDuplicatesAndKeepsQuantitiesNotesAndCategories()
    {
        var items = ShoppingListDocument.FromIngredients(new[]
        {
            new Ingredient { Name = "Milk", Quantity = "1 L", Tag = "dairy", Note = "Oat" },
            new Ingredient { Name = "milk", Quantity = "1 l", Tag = "Dairy", Note = "oat" },
            new Ingredient { Name = "Lemon + lime", Tag = "Veg" }
        });
        var milk = Assert.Single(items, i => i.Name == "Milk");
        Assert.Equal("2 L", milk.Quantity);
        Assert.Equal("Oat", milk.Note);
        Assert.Equal("Produce", Assert.Single(items, i => i.Name == "Lemon + lime").Category);
    }

    [Fact]
    public void UndoHistoryIsBounded()
    {
        var list = new ShoppingListDocument();
        for (var i = 0; i < 35; i++) list.Add(new() { Name = "Item " + i });
        var undone = 0;
        while (list.CanUndo) { list.Undo(); undone++; }
        Assert.Equal(30, undone);
        Assert.Equal(5, list.Items.Count);
    }
}
