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
        var dark = AppSettings.Current.IsDark(app.RequestedTheme == AppTheme.Dark);
        var theme = ThemeColors.For(AppSettings.Current, app.RequestedTheme == AppTheme.Dark);
        var colors = new Dictionary<string, string> {
            ["ThemePrimary"] = theme.Primary, ["ThemeAccent"] = theme.Accent,
            ["ThemeOnAccent"] = "#FFFFFF", ["ThemeText"] = theme.Text,
            ["ThemeMuted"] = theme.Muted,
            ["ThemeBackground"] = theme.Background,
            ["ThemeSurface"] = theme.Surface,
            ["ThemeSubtle"] = dark ? "#2C3A46" : "#E7EDF1",
            ["ThemeOutline"] = dark ? "#485966" : "#CDD7DF"
        };
        foreach (var entry in colors) app.Resources[entry.Key] = Color.FromArgb(entry.Value);
        for (var size = 10; size <= 36; size++) app.Resources["ThemeFont" + size] = size * (AppSettings.Current.LargerText ? 1.18 : 1.0);
        app.Resources["ThemeShowPhotos"] = AppSettings.Current.ShowPhotos;
        ApplySystemBars();
#if ANDROID
        RecipeApp.Platforms.Android.ShoppingQuickAddWidget.UpdateAll(global::Android.App.Application.Context);
#endif
    }
    public static void ApplySystemBars()
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(()=>{
            var window=Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;if(window==null)return;
            var color=Application.Current?.Resources.TryGetValue("ThemePrimary",out var value)==true&&value is Color selected?selected:Color.FromArgb("#214D35");
            window.SetStatusBarColor(global::Android.Graphics.Color.Argb((int)(color.Alpha*255),(int)(color.Red*255),(int)(color.Green*255),(int)(color.Blue*255)));
            new AndroidX.Core.View.WindowInsetsControllerCompat(window,window.DecorView).AppearanceLightStatusBars=false;
        });
#endif
    }

}
