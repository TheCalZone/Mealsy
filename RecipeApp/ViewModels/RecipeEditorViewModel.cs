using MvvmHelpers;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;

namespace RecipeApp.ViewModels;

public class RecipeEditorViewModel : ObservableObject
{
    public RecipeDraft Draft { get; private set; } = new();
    public event Action StructureChanged;
    public Func<Recipe, Task> RecipeSaved { get; set; }
    private readonly string account = HouseholdService.UserId;
    public string EditorAccount => account;
    private CancellationTokenSource debounce;
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private bool ready, completed, busy;
    private string status = "Your draft saves on this device", error;
    private string initial;
    public string Status { get => status; set => SetProperty(ref status, value); }
    public string Error { get => error; set => SetProperty(ref error, value); }
    public bool Busy { get => busy; set => SetProperty(ref busy, value); }
    public bool IsEditing => Draft.Recipe.Id != 0;
    public string Heading => IsEditing ? "Make it even better" : "Your next favourite meal";
    public string Summary => $"{Draft.IngredientCount} ingredients · {Draft.Steps.Count(s => !string.IsNullOrWhiteSpace(s.Text))} steps";
    public double Progress => ((string.IsNullOrWhiteSpace(Draft.Recipe.RecipeName) ? 0 : 1) + (Draft.IngredientCount > 0 ? 1 : 0) + (Draft.Steps.Any(s => !string.IsNullOrWhiteSpace(s.Text)) ? 1 : 0)) / 3d;
    public bool HasPhoto => !string.IsNullOrWhiteSpace(Draft.Recipe.RecipeImage);
    private string State() => JsonSerializer.Serialize(new { Draft.Recipe, Draft.Servings, Draft.PrepMinutes, Draft.CookMinutes, Draft.Rating, Draft.Sections, Draft.Steps, Draft.PendingPaste });
    public async Task Initialize(Recipe recipe = null,bool recoverDraft=true)
    {
        if (ready) return;
        Draft = recipe == null ? new() : RecipeDraft.FromRecipe(recipe); initial = State();
        await RecipeService.GetStaplesForLocal();CheckAccount();
        try
        {
            var recovered = recoverDraft?await RecipeDraftStore.Load(account, recipe?.Id ?? 0):null;
            if (recovered != null) { Draft = recovered; Status = "Draft restored · Your saved recipe hasn't changed"; }
        }
        catch (JsonException) { Status = "An unreadable draft was kept as a recovery copy. A fresh editor is open."; }
        catch (Exception) { Status = "Draft storage is unavailable. Keep this screen open until you save."; }
        ready = true; Refresh();
    }
    private void CheckAccount()
    {
        if (account != HouseholdService.UserId) throw new InvalidOperationException("Your account changed. Return to your recipe book and reopen the editor. Your previous account's draft was kept.");
    }
    public void Changed()
    {
        if (!ready || completed || Busy) return;
        Error = ""; OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(Progress)); OnPropertyChanged(nameof(HasPhoto));
        debounce?.Cancel(); debounce = new(); var cancellation = debounce.Token;
        _ = SaveLater(cancellation);
    }
    private async Task SaveLater(CancellationToken token)
    {
        try { await Task.Delay(800, token); if (!token.IsCancellationRequested) await SaveDraft(); }
        catch (OperationCanceledException) { }
    }
    public async Task SaveDraft()
    {
        if (!ready || completed) return;
        await saveGate.WaitAsync();
        try
        {
            if (completed) return;
            CheckAccount();
            if (State() == initial) { await RecipeDraftStore.Clear(account, Draft.Recipe.Id); Status = "No unsaved changes"; return; }
            await RecipeDraftStore.Save(account, Draft); Status = "Draft saved on this device";
        }
        catch (Exception) { Status = "Draft couldn't be saved. Keep this screen open and save the recipe."; }
        finally { saveGate.Release(); }
    }
    public void Refresh()
    {
        foreach (var property in new[] { nameof(Draft), nameof(Heading), nameof(Summary), nameof(Progress), nameof(HasPhoto) }) OnPropertyChanged(property);
        StructureChanged?.Invoke(); Changed();
    }
    public async Task Run(Func<Task> action)
    {
        if (Busy || completed) return; Busy = true;
        try { CheckAccount(); await action(); }
        catch (Exception e) { await Shell.Current.DisplayAlert("Recipe", e is InvalidOperationException ? e.Message : "That action couldn't be completed. Your draft has been kept. Please try again.", "OK"); }
        finally { Busy = false; }
    }
    public async Task Save()
    {
        await Run(async () => {
            debounce?.Cancel();
            var errors = Draft.Validate();
            if (errors.Count > 0) { Error = string.Join("\n", errors.Select(e => "• " + e)); await Shell.Current.DisplayAlert("Check your recipe", string.Join("\n", errors), "Keep editing"); return; }
            var recipe = Draft.Build();
            if (recipe.Id != 0)
            {
                var current = await RecipeService.GetRecipe(recipe.Id);
                if (current == null || current.LastUpdated != Draft.OriginalUpdatedAt) throw new InvalidOperationException("This recipe changed or was removed while your draft was open. Your draft is kept; copy its text from Preview before reopening the saved recipe.");
                await RecipeService.UpdateRecipeAndIngredientsAsync(recipe, recipe.Ingredients);
            }
            else { recipe.FirebaseUID = account; await RecipeService.AddRecipe(recipe); }
            // Once local save succeeds, retries must never insert a second recipe.
            completed = true;
            await saveGate.WaitAsync();
            try { await RecipeDraftStore.Clear(account, Draft.Recipe.Id); } catch (Exception) { Status = "Saved. The old draft couldn't be cleared."; }
            finally { saveGate.Release(); }
            if (account != null)
            {
                try { if (Draft.Recipe.Id == 0) await FirestoreService.AddRecipe(recipe); else await FirestoreService.UpdateRecipe(recipe); }
                catch (Exception) { await Shell.Current.DisplayAlert("Saved on this device", "Cloud sync couldn't finish. Your recipe is safely saved locally.", "OK"); }
            }
            await AppNavigation.Back();
            if (RecipeSaved != null) await RecipeSaved(recipe);
        });
    }
    public async Task Discard()
    {
        if (Busy || completed) return;
        if (!await Shell.Current.DisplayAlert(IsEditing ? "Discard these edits?" : "Discard this draft?", "Your saved recipes will be kept. The draft on this device will be removed.", "Discard", "Keep editing")) return;
        await Run(async () => {
            debounce?.Cancel(); await saveGate.WaitAsync();
            try { await RecipeDraftStore.Clear(account, Draft.Recipe.Id); completed = true; } finally { saveGate.Release(); }
            await AppNavigation.Back();
        });
    }
    public async Task ChoosePhoto()
    {
        await Run(async () => {
            var file = await MediaPicker.Default.PickPhotoAsync(); if (file == null) return;
            using var source = await file.OpenReadAsync(); Draft.Recipe.RecipeImage = await FeedPhoto.PrepareAsync(source);
            OnPropertyChanged(nameof(HasPhoto)); StructureChanged?.Invoke();
        });
        Changed();
    }
    public void AddIngredient(RecipeSection section)
    {
        if (Draft.Sections.Sum(s => s.Items.Count) >= 100) { Error = "This recipe already has 100 ingredient rows."; return; }
        section.Items.Add(new() { MainIngredient = true, Tag = "Other" }); Refresh();
    }
    public void PasteIngredients(string text)
    {
        var parsed = RecipeDraft.ParseIngredients(text);
        if (Draft.Sections.Sum(s => s.Items.Count) + parsed.Sum(s => s.Items.Count) > 100) throw new InvalidOperationException("Paste fewer ingredients; recipes support up to 100 rows.");
        foreach (var section in parsed)
        {
            foreach (var item in section.Items)
            {
                var known = RecipeService.MasterIngredientList.FirstOrDefault(i => item.Name.Equals(i.Name, StringComparison.OrdinalIgnoreCase));
                if (known != null) item.Tag = ShoppingListDocument.CategoryName(known.Tag);
            }
            var existing = Draft.Sections.FirstOrDefault(s => s.Name.Equals(section.Name, StringComparison.OrdinalIgnoreCase));
            if (existing == null) Draft.Sections.Add(section); else foreach (var item in section.Items) existing.Items.Add(item);
        }
        Draft.PendingPaste = ""; Refresh();
    }
    public async Task Preview()
    {
        await Run(async () => {
            await SaveDraft();
            await Shell.Current.Navigation.PushAsync(new RecipeApp.Views.RecipePreviewPage(Draft));
        });
    }
}
