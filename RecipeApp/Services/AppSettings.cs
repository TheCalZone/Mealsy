using System.Text.Json;
namespace RecipeApp.Services;

public static class AppSettings
{
    private const string Key = "mealsy.device-settings.v1";
    public static SettingsOptions Current { get; private set; } = SettingsOptions.Read(Preferences.Get(Key, ""));
    public static void Save(SettingsOptions options)
    {
        var next = options.Normalize();
        // Write first so a failed save cannot leave the visible settings ahead of storage.
        Preferences.Set(Key, JsonSerializer.Serialize(next));
        Current = next;
        ThemeService.Apply();
    }
    public static void Reset() => Save(new());
}

public static class ThemeService
{
    public static void Initialize()
    {
        Application.Current.RequestedThemeChanged += (_, _) => ApplyColors();
        Apply();
    }
    public static void Apply()
    {
        var app = Application.Current;
        app.UserAppTheme = AppSettings.Current.Appearance switch { "Light" => AppTheme.Light, "Dark" => AppTheme.Dark, _ => AppTheme.Unspecified };
        ApplyColors();
    }
    private static void ApplyColors()
    {
        var app = Application.Current;
        var dark = app.RequestedTheme == AppTheme.Dark;
        var (primary, accent) = AppSettings.Current.Palette switch {
            "Ocean" => ("#164E69", "#286D89"),
            "Berry" => ("#66344F", "#914768"),
            "Terracotta" => ("#743E2D", "#9B5138"),
            "Slate" => ("#334155", "#50627B"),
            _ => ("#214D35", "#386347")
        };
        var colors = new Dictionary<string, string> {
            ["ThemePrimary"] = primary, ["ThemeAccent"] = dark ? accent : primary,
            ["ThemeOnAccent"] = "#FFFFFF", ["ThemeText"] = dark ? "#EEF1F5" : "#202A32",
            ["ThemeMuted"] = dark ? "#B7C2CC" : "#5B6873",
            ["ThemeBackground"] = dark ? "#11171D" : "#F5F7F8",
            ["ThemeSurface"] = dark ? "#1D2730" : "#FFFFFF",
            ["ThemeSubtle"] = dark ? "#2C3A46" : "#E7EDF1",
            ["ThemeOutline"] = dark ? "#485966" : "#CDD7DF"
        };
        foreach (var entry in colors) app.Resources[entry.Key] = Color.FromArgb(entry.Value);
        for (var size = 10; size <= 36; size++) app.Resources["ThemeFont" + size] = size * (AppSettings.Current.LargerText ? 1.18 : 1.0);
        app.Resources["ThemeShowPhotos"] = AppSettings.Current.ShowPhotos;
    }
}
