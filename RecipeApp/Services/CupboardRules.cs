using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RecipeApp.Models;

namespace RecipeApp.Services;

public static class CupboardRules
{
    public static IReadOnlyList<string> Defaults { get; } = Array.AsReadOnly(new[] {
        "Salt","Sea salt","Table salt","Pepper","Black pepper","White pepper","Sugar",
        "Olive oil","Extra virgin olive oil","Vegetable oil","Sunflower oil","Rapeseed oil",
        "Oregano","Basil","Thyme","Rosemary","Parsley","Mixed herbs","Herbs de Provence","Herbes de Provence",
        "Bay leaf","Bay leaves","Cumin","Paprika","Smoked paprika","Cinnamon","Coriander","Coriander leaves",
        "Chilli powder","Chili powder","Chilli flakes","Chili flakes","Ancho chilli flakes","Ancho chili flakes",
        "Fennel seeds","Garlic granules","Baking powder","Bicarbonate of soda"
    });
    public static string Identity(string name)
    {
        var value = Regex.Replace(name ?? "", @"\([^)]*\)", " ").Split(',')[0].ToLowerInvariant().Normalize(NormalizationForm.FormD);
        value = new string(value.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        value = Regex.Replace(value, @"[^\p{L}\p{N}]+", " ").Trim();
        return Regex.Replace(value, @"^(?:(?:freshly|fresh|dried|ground|finely|roughly|chopped|minced|crushed|grated)\s+)+", "");
    }
    public static bool Matches(string name, IEnumerable<string> rules) => Identity(name).Length > 0 && rules.Any(rule => Identity(rule) == Identity(name));
    public static bool Check(Ingredient item, IEnumerable<string> rules, IEnumerable<string> essentials = null) => item.ShoppingPreference switch {
        1 => false,
        2 => true,
        _ => !item.MainIngredient || Matches(item.Name, rules) || Matches(item.Name, essentials ?? Array.Empty<string>())
    };
    public static List<string> Validate(IEnumerable<string> values)
    {
        var list = values?.ToList() ?? throw new InvalidOperationException("Choose a cupboard ingredient list.");
        if(list.Count > 200 || list.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 100 || Identity(s).Length == 0 || s.Any(char.IsControl)))
            throw new InvalidOperationException("Use up to 200 ingredient names, each 1–100 characters long.");
        return list.Select(s => s.Trim()).DistinctBy(Identity).OrderBy(s => s,StringComparer.OrdinalIgnoreCase).ToList();
    }
}

public static class CupboardStore
{
    private static string Key(string account) => "CupboardRules.v1." + (account ?? "device");
    public static List<string> Load(string account)
    {
        var raw = Preferences.Get(Key(account), "");
        if(string.IsNullOrEmpty(raw)) return CupboardRules.Defaults.ToList();
        try { return CupboardRules.Validate(JsonSerializer.Deserialize<List<string>>(raw)); }
        catch(Exception e) when(e is JsonException or InvalidOperationException) { throw new InvalidOperationException("Your cupboard recognition list could not be read. Open Cupboard ingredients and restore defaults to repair it. Your recipes are kept."); }
    }
    public static void Save(string account,IEnumerable<string> rules) => Preferences.Set(Key(account),JsonSerializer.Serialize(CupboardRules.Validate(rules)));
    public static void Reset(string account) => Save(account,CupboardRules.Defaults);
}
