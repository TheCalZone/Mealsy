using System.Text.Json;
using System.Text.RegularExpressions;
namespace RecipeApp.Services;

internal static class LocalAccountCleanup
{
    public static async Task Remove(string uid)
    {
        await RecipeService.DeleteAccountLocal(uid);
        var pattern="(?<![A-Za-z0-9_-])"+Regex.Escape(uid)+"(?![A-Za-z0-9_-])";
        IEnumerable<string> keys=Array.Empty<string>();
#if ANDROID
#pragma warning disable CA1422, CS0618
        keys=global::Android.Preferences.PreferenceManager.GetDefaultSharedPreferences(global::Android.App.Application.Context).All.Keys.ToList();
#pragma warning restore CA1422, CS0618
#elif IOS
        keys=Foundation.NSUserDefaults.StandardUserDefaults.ToDictionary().Keys.Select(k=>k.ToString()).ToList();
#endif
        foreach(var key in keys)
        {
            var owned=Regex.IsMatch(key,pattern);
            if(key.StartsWith("Mealsy.widget.",StringComparison.Ordinal))
            {
                try{owned=JsonSerializer.Deserialize<WidgetListTarget>(Preferences.Get(key,"null"))?.Account==uid;}catch(JsonException){ }
            }
            if(owned)Preferences.Remove(key);
        }
        foreach(var kind in new[]{"weeks","shopping"})
        {
            var path=PremiumLibrary.PathFor(uid,kind);var dir=Path.GetDirectoryName(path);
            if(Directory.Exists(dir))foreach(var file in Directory.EnumerateFiles(dir,Path.GetFileName(path)+"*"))File.Delete(file);
        }
        var drafts=Path.Combine(FileSystem.AppDataDirectory,"recipe-drafts");
        if(Directory.Exists(drafts))foreach(var file in Directory.EnumerateFiles(drafts))
        {
            try{using var value=JsonDocument.Parse(await File.ReadAllTextAsync(file));if(value.RootElement.GetProperty("Recipe").TryGetProperty("FirebaseUID",out var owner)&&owner.GetString()==uid)File.Delete(file);}
            catch(JsonException){ }catch(KeyNotFoundException){ }catch(InvalidOperationException){ }
        }
#if ANDROID
        RecipeApp.Platforms.Android.ShoppingQuickAddWidget.UpdateAll(global::Android.App.Application.Context);
#endif
    }
}
