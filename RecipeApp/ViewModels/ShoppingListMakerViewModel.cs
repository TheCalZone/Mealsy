using MvvmHelpers;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels;

public class ShoppingLibraryItem : ObservableObject
{
    public long Id { get; set; }
    public bool IsSnack { get; set; }
    public string Name { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string Category { get; set; } = "Other";
    public string Note { get; set; } = "";
    public string Account { get; set; }
    public string FirestoreId { get; set; }
    private bool selected;
    public bool Selected { get => selected; set => SetProperty(ref selected, value); }
    public string Key => (IsSnack ? "snack:" : "essential:") + Id;
    public ShoppingLibraryItem Copy() => new() { Id=Id, IsSnack=IsSnack, Name=Name, Quantity=Quantity, Category=Category, Note=Note, Account=Account, FirestoreId=FirestoreId, Selected=Selected };
    public Snack ToSnack() => new() { Id=Id, Name=Name, Quantity=Quantity, Tag=Category, Note=Note, FirebaseUID=Account, FirestoreID=FirestoreId, CloudKey=FirestoreId, LastUpdated=DateTimeOffset.UtcNow };
    public Staple ToStaple() => new() { Id=Id, Name=Name, Quantity=Quantity, Tag=Category, Note=Note, FirebaseUID=Account, FirestoreID=FirestoreId, CloudKey=FirestoreId, LastUpdated=DateTimeOffset.UtcNow };
}
public class ShoppingListMakerViewModel : ObservableObject
{
    public List<RecipeShoppingChoice> Recipes { get; private set; } = new();
    public List<ShoppingLibraryItem> Library { get; private set; } = new();
    public List<ShoppingReviewItem> Review { get; private set; } = new();
    public event Action Changed;
    public string Account { get; private set; } = HouseholdService.UserId;
    public bool Busy { get; private set; }
    public bool Loaded { get; private set; }
    public string Summary => $"{Recipes.Count(r => r.Selected)} recipes · {Library.Count(s => s.Selected)} extras";
    private bool Own(string owner) => Account == null ? string.IsNullOrEmpty(owner) : owner == Account;
    public void CheckAccount() { if (Account != HouseholdService.UserId) throw new InvalidOperationException("Your account changed. Reopen the list creator before continuing."); }
    public void Notify() => Changed?.Invoke();
    public async Task Load()
    {
        if (Busy) return;
        Busy = true;
        try
        {
            if (Account != HouseholdService.UserId) { Account=HouseholdService.UserId; Reset(); Loaded=false; }
            await RecipeService.GetRecipesForLocal(); await RecipeService.GetSnacksForLocal(); await RecipeService.GetStaplesForLocal();
            CheckAccount();
            var previous = Recipes.ToDictionary(r => r.Recipe.Id);
            Recipes = RecipeService.Recipes.Where(r => Own(r.FirebaseUID)).Select(r => {
                var choice = new RecipeShoppingChoice(r);
                if (previous.TryGetValue(r.Id, out var old)) { choice.Selected=old.Selected; choice.Servings=old.Servings; choice.Batches=old.Batches; }
                choice.PropertyChanged += (_, _) => Notify(); return choice;
            }).ToList();
            Recipes.AddRange(previous.Values.Where(r => r.Recipe.Id==0));
            var selected = Library.Where(i => i.Selected).Select(i => i.Key).ToHashSet();
            Library = RecipeService.Snacks.Where(s => Own(s.FirebaseUID)).Select(s => new ShoppingLibraryItem { Id=s.Id, IsSnack=true, Name=s.Name, Quantity=s.Quantity, Category=ShoppingListDocument.CategoryName(s.Tag), Note=s.Note, Account=s.FirebaseUID, FirestoreId=s.CloudKey ?? s.FirestoreID ?? s.Name })
                .Concat(RecipeService.Staples.Where(s => Own(s.FirebaseUID)).Select(s => new ShoppingLibraryItem { Id=s.Id, Name=s.Name, Quantity=s.Quantity, Category=ShoppingListDocument.CategoryName(s.Tag), Note=s.Note, Account=s.FirebaseUID, FirestoreId=s.CloudKey ?? s.FirestoreID ?? s.Name })).OrderBy(s => s.Name).ToList();
            foreach (var item in Library) { item.Selected=selected.Contains(item.Key); item.PropertyChanged += (_, _) => Notify(); }
            Loaded=true; Notify();
        }
        finally { Busy=false; }
    }
    public void BuildReview()
    {
        CheckAccount(); var old = Review.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.First());
        Review = ShoppingPlan.Build(Recipes, Library.Where(s => s.IsSnack && s.Selected).Select(s => s.ToSnack()),
            Library.Where(s => !s.IsSnack && s.Selected).Select(s => s.ToStaple()), Library.Where(s => !s.IsSnack).Select(s => s.Name), CupboardStore.Load(Account));
        foreach (var row in Review) { if (old.TryGetValue(row.Key, out var previous)) { if(row.CheckCupboard==previous.CheckCupboard)row.Included=previous.Included; row.Item.Quantity=previous.Item.Quantity; row.Item.Note=previous.Item.Note; } row.PropertyChanged += (_, _) => Notify(); }
        Notify();
    }
    public void Reset()
    {
        foreach (var choice in Recipes) { choice.Selected=false; choice.Servings=Math.Max(1, choice.Recipe.Serving).ToString(); choice.Batches="1"; }
        foreach (var item in Library) item.Selected=false;
        Review.Clear(); Notify();
    }
    public async Task SaveExtra(ShoppingLibraryItem item, ShoppingLibraryItem original = null)
    {
        CheckAccount();
        if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 200) throw new InvalidOperationException("Enter an item name (up to 200 characters).");
        if ((item.Quantity ?? "").Length > 200 || (item.Note ?? "").Length > 500) throw new InvalidOperationException("Keep quantities within 200 characters and notes within 500.");
        if (item.Name.Contains('/') || item.Name is "." or "..") throw new InvalidOperationException("Use a name without / characters.");
        if (Library.Any(s => s.IsSnack==item.IsSnack && s.Id!=item.Id && ShoppingQuantity.Identity(s.Name)==ShoppingQuantity.Identity(item.Name))) throw new InvalidOperationException("That item is already in this section. Edit its existing entry instead.");
        item.Name=item.Name.Trim(); item.Quantity=ShoppingQuantity.Normalize(item.Quantity); item.Note=item.Note?.Trim() ?? ""; item.Category=ShoppingListDocument.CategoryName(item.Category); item.Account=Account;
        // Cloud paths in older versions used the name. Preserve the document ID when renaming.
        item.FirestoreId ??= original?.FirestoreId ?? original?.Name ?? Guid.NewGuid().ToString("N");
        if (item.IsSnack) { var value=item.ToSnack(); if (item.Id==0) await RecipeService.InsertSnack(value); else await RecipeService.UpdateSnack(value); item.Id=value.Id; }
        else { var value=item.ToStaple(); if (item.Id==0) await RecipeService.InsertStaple(value); else await RecipeService.UpdateStaple(value); item.Id=value.Id; }
        CheckAccount();
        try { if (item.IsSnack) await FirestoreService.UpdateSnack(item.ToSnack()); else await FirestoreService.UpdateStaple(item.ToStaple()); }
        catch (Exception) { await Shell.Current.DisplayAlert("Saved on this device", "Cloud sync couldn't finish. Your library item is saved locally.", "OK"); }
        await Load();
    }
    public async Task DeleteExtra(ShoppingLibraryItem item)
    {
        CheckAccount();
        // Do the cloud delete first so a failure cannot silently resurrect a locally deleted item.
        if (item.IsSnack) { await FirestoreService.DeleteSnack(item.ToSnack()); await RecipeService.DeleteSnack(item.ToSnack()); }
        else { await FirestoreService.DeleteStaple(item.ToStaple()); await RecipeService.DeleteStaple(item.ToStaple()); }
        await Load();
    }
    public async Task WriteList(bool replace)
    {
        if (Busy) return; Busy=true;
        try
        {
            CheckAccount(); var selected=Review.Where(r => r.Included).Select(r => r.ToIngredient()).ToList();
            if (selected.Count==0) throw new InvalidOperationException("Tick at least one item that you need before creating your list.");
            var lists=PersonalShoppingLists.All();
            var destination=lists[0];
            if(lists.Count>1)
            {
                var names=lists.Select((l,i)=>$"{i+1}. {l.Name}").ToArray();
                var choice=await Shell.Current.DisplayActionSheet("Choose personal shopping list","Cancel",null,names);
                CheckAccount();var index=Array.IndexOf(names,choice);if(index<0)return;
                destination=lists[index];
            }
            if(destination.Id!="main")
            {
                var key=PersonalShoppingLists.StorageKey(destination.Id);
                var stored=Preferences.Get(key,"");
                var document=string.IsNullOrWhiteSpace(stored)?new ShoppingListDocument():ShoppingListDocument.Deserialize(stored);
                if(replace)document.Replace(ShoppingListDocument.FromIngredients(selected));else document.AppendIngredients(selected);
                Preferences.Set(key,document.Serialize());
                Reset();await Shell.Current.GoToAsync("//ShoppingListPage?PersonalListId="+destination.Id);return;
            }
            if (replace)
            {
                // Validate existing storage before allowing an intentional replacement.
                var saved=Preferences.Get(ShoppingListStore.StateKey, ""); if (!string.IsNullOrWhiteSpace(saved)) ShoppingListDocument.Deserialize(saved);
                var document=new ShoppingListDocument(); document.Replace(ShoppingListDocument.FromIngredients(selected)); ShoppingListStore.Save(document);
            }
            else ShoppingListStore.AppendIngredients(selected);
            Reset(); await Shell.Current.GoToAsync("//ShoppingListPage?OpenPersonal=True");
        }
        finally { Busy=false; }
    }
    public async Task UseRecipes(IEnumerable<RecipeShoppingChoice> choices)
    {
        await Load(); CheckAccount(); Reset();
        foreach (var requested in choices)
        {
            var existing=Recipes.FirstOrDefault(r => r.Recipe.Id==requested.Recipe.Id);
            if (existing==null && requested.Recipe.Id==0) { existing=new RecipeShoppingChoice(requested.Recipe); existing.PropertyChanged += (_,_) => Notify(); Recipes.Add(existing); }
            if (existing == null) throw new InvalidOperationException("A selected recipe isn't available to this account.");
            existing.Selected=true; existing.Servings=requested.Servings; existing.Batches=requested.Batches;
        }
        BuildReview();
    }
}
