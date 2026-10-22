using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RecipeApp.Services;

public static class RecipeDraftStore
{
    private static readonly SemaphoreSlim gate = new(1, 1);
    public static string PathFor(string account, long recipeId) => Path.Combine(FileSystem.AppDataDirectory, "recipe-drafts", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((account == null ? "guest" : "user:" + account) + ":" + recipeId))) + ".json");
    public static async Task Save(string account, RecipeDraft draft)
    {
        if(account!=null&&Preferences.Get("Mealsy.deletion.pending","")==account)throw new InvalidOperationException("Account deletion is pending. Draft saving is stopped.");
        draft.SavedAt = DateTimeOffset.UtcNow;
        var text = JsonSerializer.Serialize(draft); var path = PathFor(account, draft.Recipe.Id);
        await gate.WaitAsync();
        try { Directory.CreateDirectory(Path.GetDirectoryName(path)); await File.WriteAllTextAsync(path + ".tmp", text); File.Move(path + ".tmp", path, true); }
        finally { gate.Release(); }
    }
    public static async Task<RecipeDraft> Load(string account, long id)
    {
        await gate.WaitAsync();
        try
        {
            var path = PathFor(account, id); if (!File.Exists(path)) return null;
            try
            {
                var draft = JsonSerializer.Deserialize<RecipeDraft>(await File.ReadAllTextAsync(path));
                if (draft?.Recipe == null || draft.Recipe.Id != id || draft.Sections == null || draft.Steps == null || draft.Sections.Any(s => s?.Items == null || s.Items.Any(i => i == null)) || draft.Steps.Any(s => s == null)) throw new JsonException();
                return draft;
            }
            catch (JsonException) { File.Copy(path, path + ".recovery", true); throw; }
        }
        finally { gate.Release(); }
    }
    public static async Task Clear(string account, long id)
    {
        await gate.WaitAsync(); try { File.Delete(PathFor(account, id)); } finally { gate.Release(); }
    }
}
