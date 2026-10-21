using System.Text.Json;
namespace RecipeApp.Services;

/// <summary>Device preferences only: never part of account, recipe or shopping data.</summary>
public sealed record SettingsOptions
{
    public string Palette { get; init; } = "Forest";
    public string Appearance { get; init; } = "System";
    public bool LargerText { get; init; }
    public string Language { get; init; } = "system";
    public bool ShowPhotos { get; init; } = true;
    public bool HideCollected { get; init; }
    public bool ShoppingDetails { get; init; }
    public string RecipeSort { get; init; } = "Recent";
    public bool KeepAwake { get; init; }
    public bool IsDark(bool systemDark) => Appearance switch { "Light"=>false,"Dark"=>true,_=>systemDark };
    public static readonly string[] Palettes = { "Forest", "Ocean", "Berry", "Terracotta", "Slate" };
    public static readonly string[] Appearances = { "System", "Light", "Dark" };
    public static readonly string[] Sorts = { "Recent", "Name", "Quickest", "Top rated" };
    public SettingsOptions Normalize() => this with {
        Language = AppLocalization.Normalize(Language),
        Palette = Palettes.Contains(Palette) ? Palette : "Forest",
        Appearance = Appearances.Contains(Appearance) ? Appearance : "System",
        RecipeSort = Sorts.Contains(RecipeSort) ? RecipeSort : "Recent"
    };
    public static SettingsOptions Read(string json)
    {
        try { return (JsonSerializer.Deserialize<SettingsOptions>(json) ?? new()).Normalize(); }
        catch (JsonException) { return new(); }
    }
}
