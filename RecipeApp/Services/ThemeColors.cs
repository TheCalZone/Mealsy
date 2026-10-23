namespace RecipeApp.Services;

// Shared by MAUI and Android RemoteViews/native dialogs, including cold widget launches.
public sealed record ThemeColors(string Primary,string Accent,string Text,string Muted,string Background,string Surface)
{
    public static ThemeColors For(SettingsOptions settings,bool systemDark)
    {
        var dark=settings.IsDark(systemDark);
        var (primary,accent)=settings.Palette switch {
            "Ocean"=>("#164E69","#286D89"), "Berry"=>("#66344F","#914768"),
            "Terracotta"=>("#743E2D","#9B5138"), "Slate"=>("#334155","#50627B"),
            _=>("#214D35","#386347")
        };
        return new(primary,dark?accent:primary,dark?"#EEF1F5":"#202A32",dark?"#B7C2CC":"#5B6873",dark?"#11171D":"#F5F7F8",dark?"#1D2730":"#FFFFFF");
    }
}
