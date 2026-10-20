using MvvmHelpers;
using RecipeApp.Models;
using RecipeApp.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RecipeApp.ViewModels;

public class ShoppingListGroup : ObservableRangeCollection<ShoppingListItem>
{
    public string Title { get; }
    public string Summary => Count == 1 ? AppLocalization.Text("1 item") : AppLocalization.Format("{0} items",Count);
    public ShoppingListGroup(string title, IEnumerable<ShoppingListItem> items) : base(items) => Title = title;
}

public class ShoppingListViewModel : CustomViewModelBase
{
    private string StorageKey => sharedKey ?? ShoppingListStore.StateKey;
    private string sharedKey;
    private string loadedKey;
    public string CurrentJson => document.Serialize();
    public string SectionOrderContext=>StorageKey;
    private string ItemOrderKey=>StorageKey+".itemOrder";
    public bool CanDropItem(Guid source,Guid target)=>!IsEditing&&Groups.Any(group=>group.Any(item=>item.Id==source)&&group.Any(item=>item.Id==target));
    public void DropItem(Guid source,Guid target,bool after)
    {
        if(!CanDropItem(source,target))return;
        var order=ShoppingItemOrder.Drop(document.Items,ShoppingItemOrder.Read(Preferences.Get(ItemOrderKey,"[]")),source,target,after,document.Visible(SearchText,HideCollected).Select(item=>item.Id));
        Preferences.Set(ItemOrderKey,JsonSerializer.Serialize(order));RefreshGroups();StatusText="Item order saved on this device";
    }
    private string SectionOrderKey=>StorageKey+".sectionOrder";
    public List<string> SectionNames()=>ShoppingSectionOrder.Sort(Categories.Concat(document.Items.Select(i=>i.Category)),ShoppingSectionOrder.Read(Preferences.Get(SectionOrderKey,"[]")));
    public void SaveSectionOrder(IEnumerable<string> order)
    {
        Preferences.Set(SectionOrderKey,JsonSerializer.Serialize(ShoppingSectionOrder.Sort(SectionNames(),order)));
        RefreshGroups();StatusText="Section order saved for this list on this device";
    }
    public void ResetSectionOrder(){Preferences.Remove(SectionOrderKey);RefreshGroups();StatusText="Default section order restored";}
    public bool IsShared => sharedKey != null;
    public void SelectList(string key)
    {
        sharedKey = key; loaded = false; loadedKey = null; IsEditing = false; document = new();
        QuickName = QuickQuantity = QuickNote = EditName = EditQuantity = EditCategory = EditNote = "";
        SearchText = ""; ShowDetails = false;
    }
    public void AcceptRemote(string json)
    {
        var editId = editingItem?.Id;
        document = ShoppingListDocument.Deserialize(json);
        // Keep the form values, but point Save at the refreshed document's item.
        if (editId != null)
        {
            editingItem = document.Items.FirstOrDefault(item => item.Id == editId);
            if (editingItem == null) IsEditing = false;
        }
        ConnectItems(); UpdateCategories(); RefreshGroups(); NotifySummary(); Persist();
    }
    private string ExportKey => ShoppingListStore.ExportKey;
    private ShoppingListDocument document = new();
    private readonly List<ShoppingListItem> subscriptions = new();
    private bool loaded, batching;
    private string searchText, quickName, quickQuantity, quickNote, selectedCategory = "Other", statusText = "Changes save automatically";
    private bool hideCollected, showDetails, isEditing;
    private ShoppingListItem editingItem;
    private string editName, editQuantity, editCategory, editNote;

    public ObservableRangeCollection<ShoppingListGroup> Groups { get; } = new();
    public ObservableRangeCollection<string> Categories { get; } = new();
    public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) RefreshGroups(); } }
    public bool HideCollected { get => hideCollected; set { if (SetProperty(ref hideCollected, value)) RefreshGroups(); } }
    public bool ShowDetails { get => showDetails; set => SetProperty(ref showDetails, value); }
    public string QuickName { get => quickName; set { SetProperty(ref quickName, value); AddItemCommand?.ChangeCanExecute(); } }
    public string QuickQuantity { get => quickQuantity; set => SetProperty(ref quickQuantity, value); }
    public string QuickNote { get => quickNote; set => SetProperty(ref quickNote, value); }
    public string SelectedCategory { get => selectedCategory; set => SetProperty(ref selectedCategory, value); }
    public string StatusText { get => statusText; set => SetProperty(ref statusText, value); }
    public int Total => document.Items.Count;
    public int Collected => document.Items.Count(i => i.InBasket);
    public int Remaining => Total - Collected;
    public double Progress => Total == 0 ? 0 : (double)Collected / Total;
    public string ProgressText => Total == 0 ? AppLocalization.Text("Ready for your next shop") : AppLocalization.Format("{0} of {1} collected",Collected,Total);
    public string RemainingText => Remaining == 1 ? AppLocalization.Text("1 item left") : AppLocalization.Format("{0} items left",Remaining);
    public string EmptyTitle => Total == 0 ? AppLocalization.Text("A fresh start") : Remaining == 0 && HideCollected ? AppLocalization.Text("All done!") : AppLocalization.Text("No matching items");
    public string EmptyMessage => Total == 0 ? AppLocalization.Text("Add your first item above, paste a list, or send ingredients from a recipe.")
        : Remaining == 0 && HideCollected ? "Everything is collected. Turn off Hide collected to review your list."
        : "Try a different search or turn off Hide collected.";
    public bool IsEditing { get => isEditing; set { if (SetProperty(ref isEditing, value)) NotifySummary(); } }
    public string EditName { get => editName; set => SetProperty(ref editName, value); }
    public string EditQuantity { get => editQuantity; set => SetProperty(ref editQuantity, value); }
    public string EditCategory { get => editCategory; set => SetProperty(ref editCategory, value); }
    public string EditNote { get => editNote; set => SetProperty(ref editNote, value); }

    public Command AddItemCommand { get; }
    public Command ToggleDetailsCommand { get; }
    public Command<ShoppingListItem> EditItemCommand { get; }
    public Command<ShoppingListItem> ToggleItemCommand { get; }
    public Command<ShoppingListItem> RemoveItemCommand { get; }
    public Command SaveEditCommand { get; }
    public Command CancelEditCommand { get; }
    public Command UndoCommand { get; }
    public Command RedoCommand { get; }
    public Command ClearCollectedCommand { get; }
    public Command ClearListCommand { get; }
    public Command PasteCommand { get; }
    public Command ShareCommand { get; }

    public ShoppingListViewModel()
    {
        AddItemCommand = new Command(AddItem, () => !string.IsNullOrWhiteSpace(QuickName));
        ToggleDetailsCommand = new Command(() => ShowDetails = !ShowDetails);
        EditItemCommand = new Command<ShoppingListItem>(BeginEdit);
        ToggleItemCommand = new Command<ShoppingListItem>(item => { if (item != null) item.InBasket = !item.InBasket; });
        RemoveItemCommand = new Command<ShoppingListItem>(item => { if (item == null) return; document.Remove(item.Id); Changed("Item removed · Undo available"); });
        SaveEditCommand = new Command(() => _ = RunAsync(SaveEditAsync));
        CancelEditCommand = new Command(() => IsEditing = false);
        UndoCommand = new Command(() => { document.Undo(); Changed("Change undone"); }, () => document.CanUndo && !IsEditing);
        RedoCommand = new Command(() => { document.Redo(); Changed("Change restored"); }, () => document.CanRedo && !IsEditing);
        ClearCollectedCommand = new Command(() => { document.ClearCollected(); Changed("Collected items removed · Undo available"); }, () => Collected > 0 && !IsEditing);
        ClearListCommand = new Command(() => _ = RunAsync(ClearAsync), () => Total > 0 && !IsEditing);
        PasteCommand = new Command(() => _ = RunAsync(PasteAsync), () => !IsEditing);
        ShareCommand = new Command(() => _ = RunAsync(ShareAsync), () => Total > 0 && !IsEditing);
        UpdateCategories();
    }

    public void Load(bool manual, string incomingJson)
    {
        HideCollected = AppSettings.Current.HideCollected;
        ShowDetails = AppSettings.Current.ShoppingDetails;
        if (loadedKey != StorageKey) { loaded = false; document = new(); IsEditing = false; loadedKey = StorageKey; }
        var legacy = IsShared ? "" : Preferences.Get(ShoppingListStore.LegacyKey, "");
        var stored = Preferences.Get(StorageKey, "");
        if (!loaded)
        {
            try { if (!string.IsNullOrWhiteSpace(stored)) document = ShoppingListDocument.Deserialize(stored); }
            catch (JsonException)
            {
                Preferences.Set(StorageKey + ".recovery", stored);
                StatusText = "Previous list could not be read; a recovery copy was kept";
            }
            loaded = true;
        }
        else if (!string.IsNullOrWhiteSpace(stored) && stored != document.Serialize())
        {
            document.Replace(ShoppingListDocument.Deserialize(stored).Items);
            StatusText = "List updated";
        }
        if (!string.IsNullOrWhiteSpace(incomingJson))
        {
            ImportIngredients(incomingJson);
            SearchText = "";
            HideCollected = false;
            Changed("New recipe list ready · Undo available");
        }
        else if (!IsShared && (legacy != Preferences.Get(ExportKey, "") || (Total == 0 && !string.IsNullOrWhiteSpace(legacy))))
        {
            document.MergeLegacyText(legacy);
            Changed("List updated");
        }
        else if (!IsShared && Total == 0 && !manual && string.IsNullOrWhiteSpace(Preferences.Get(StorageKey, "")))
        {
            var oldIngredients = Preferences.Get(ShoppingListStore.IngredientsKey, "");
            if (!string.IsNullOrWhiteSpace(oldIngredients)) ImportIngredients(oldIngredients);
            Changed();
        }
        else { ConnectItems(); RefreshGroups(); NotifySummary(); }
    }

    private void ImportIngredients(string json)
    {
        // Shell may already have decoded the query. Try raw JSON first to preserve literal '+' and '%'.
        List<Ingredient> ingredients;
        try { ingredients = JsonSerializer.Deserialize<List<Ingredient>>(json); }
        catch (JsonException) { ingredients = JsonSerializer.Deserialize<List<Ingredient>>(Uri.UnescapeDataString(json)); }
        document.Replace(ShoppingListDocument.FromIngredients(ingredients ?? new()));
    }

    private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(QuickName)) return;
        var addedName=QuickName;
        document.Add(new() { Name = QuickName, Quantity = QuickQuantity?.Trim() ?? "", Category = SelectedCategory, Note = QuickNote?.Trim() ?? "" });
        QuickName = QuickQuantity = QuickNote = "";
        SearchText = "";
        Changed("Item added");
        IngredientSuggestionStore.Remember(HouseholdService.UserId,addedName);
    }

    private void BeginEdit(ShoppingListItem item)
    {
        if (item == null) return;
        editingItem = item;
        EditName = item.Name; EditQuantity = item.Quantity; EditCategory = item.Category; EditNote = item.Note;
        IsEditing = true;
    }

    public void RemoveEditingItem()
    {
        if (editingItem == null) return;
        document.Remove(editingItem.Id);
        IsEditing = false;
        Changed("Item removed · Undo available");
    }

    private async Task SaveEditAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            await Shell.Current.DisplayAlert("Item name required", "Give this item a name before saving.", "OK");
            return;
        }
        if (editingItem == null) return;
        batching = true;
        try
        {
            var input=ShoppingItemInput.Parse(EditName,EditQuantity);editingItem.Name=input.Name;editingItem.Quantity=input.Quantity;
            editingItem.Category = ShoppingListDocument.CategoryName(EditCategory); editingItem.Note = EditNote?.Trim() ?? "";
        }
        finally { batching = false; }
        document.Commit();
        IsEditing = false;
        Changed("Item updated");
    }

    private void ConnectItems()
    {
        foreach (var item in subscriptions) item.PropertyChanged -= ItemChanged;
        subscriptions.Clear();
        foreach (var item in document.Items) { item.PropertyChanged += ItemChanged; subscriptions.Add(item); }
    }
    private void ItemChanged(object sender, PropertyChangedEventArgs args)
    {
        if (batching || args.PropertyName is not (nameof(ShoppingListItem.InBasket) or nameof(ShoppingListItem.Name)
            or nameof(ShoppingListItem.Quantity) or nameof(ShoppingListItem.Note) or nameof(ShoppingListItem.Category))) return;
        document.Commit();
        Persist();
        NotifySummary();
        if (HideCollected) RefreshGroups();
    }
    private void Changed(string message = "Saved on this device")
    {
        ConnectItems(); UpdateCategories(); RefreshGroups(); NotifySummary(); Persist(message);
    }
    public void Persist(string message = "Saved on this device")
    {
        if (!loaded || loadedKey != StorageKey) return;
        try
        {
            if (IsShared) Preferences.Set(StorageKey, document.Serialize());
            else ShoppingListStore.Save(document);
            StatusText = message;
        }
        catch (Exception) { StatusText = "Couldn't save changes. Keep this screen open and try again."; }
    }
    private void RefreshGroups()
    {
        var order=SectionNames();
        Groups.ReplaceRange(ShoppingItemOrder.Sort(document.Visible(SearchText, HideCollected),ShoppingItemOrder.Read(Preferences.Get(ItemOrderKey,"[]"))).GroupBy(i => i.Category)
            .OrderBy(g=>order.FindIndex(name=>name.Equals(g.Key,StringComparison.OrdinalIgnoreCase)))
            .Select(g => new ShoppingListGroup(g.Key, g)));
        OnPropertyChanged(nameof(EmptyTitle)); OnPropertyChanged(nameof(EmptyMessage));
    }
    private void UpdateCategories()
    {
        var selected = SelectedCategory;
        Categories.ReplaceRange(new[] { "Produce", "Dairy", "Meat", "Pantry", "Bakery", "Frozen", "Drinks", "Other" }
            .Concat(document.Items.Select(i => i.Category)).Distinct(StringComparer.OrdinalIgnoreCase));
        SelectedCategory = selected != null && Categories.Contains(selected) ? selected : "Other";
    }
    private void NotifySummary()
    {
        foreach (var property in new[] { nameof(Total), nameof(Collected), nameof(Remaining), nameof(Progress), nameof(ProgressText), nameof(RemainingText), nameof(EmptyTitle), nameof(EmptyMessage) }) OnPropertyChanged(property);
        UndoCommand.ChangeCanExecute(); RedoCommand.ChangeCanExecute(); ClearCollectedCommand.ChangeCanExecute(); ClearListCommand.ChangeCanExecute(); ShareCommand.ChangeCanExecute(); PasteCommand.ChangeCanExecute();
    }
    private async Task ClearAsync()
    {
        if (await Shell.Current.DisplayAlert("Start a fresh list?", "Remove all items? You can undo this while the screen remains open.", "Clear list", "Keep list"))
        { document.Clear(); Changed("List cleared · Undo available"); }
    }
    private async Task PasteAsync()
    {
        var text = await Clipboard.Default.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) { await Shell.Current.DisplayAlert("Nothing to paste", "Copy a list with one item per line first.", "OK"); return; }
        var before = Total;
        document.AppendText(text);
        SearchText = "";
        Changed($"List updated from clipboard · {Total - before} new rows · Compatible amounts combined");
    }
    private async Task ShareAsync()
    {
        var choice = await Shell.Current.DisplayActionSheet("Send your shopping list", "Cancel", null, "Share remaining", "Share full list", "Copy remaining", "Copy full list");
        if (choice == null || choice == "Cancel") return;
        var text = document.PlainText(remainingOnly: choice.EndsWith("remaining", StringComparison.Ordinal),sectionOrder:SectionNames(),itemOrder:ShoppingItemOrder.Read(Preferences.Get(ItemOrderKey,"[]")));
        if (string.IsNullOrWhiteSpace(text)) { await Shell.Current.DisplayAlert("Nothing left to buy", "All items are collected.", "OK"); return; }
        if (choice.StartsWith("Copy")) { await Clipboard.Default.SetTextAsync(text); StatusText = "List copied"; }
        else await Share.Default.RequestAsync(new ShareTextRequest { Title = "Shopping list", Text = text });
    }
    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(); }
        catch (Exception) { await Shell.Current.DisplayAlert("Couldn't finish", "Please try that action again.", "OK"); }
        finally { IsBusy = false; }
    }
}
