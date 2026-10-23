using RecipeApp.Services;
using Xunit;
public class ThemeColorsTests
{
    [Theory]
    [InlineData("Forest","#214D35","#386347")]
    [InlineData("Ocean","#164E69","#286D89")]
    [InlineData("Berry","#66344F","#914768")]
    [InlineData("Terracotta","#743E2D","#9B5138")]
    [InlineData("Slate","#334155","#50627B")]
    public void Palettes_and_appearance_are_shared_with_native_widgets(string palette,string light,string dark)
    {
        var settings=new SettingsOptions{Palette=palette};
        Assert.Equal(light,ThemeColors.For(settings,false).Accent);
        Assert.Equal(dark,ThemeColors.For(settings,true).Accent);
        Assert.Equal(light,ThemeColors.For(settings with{Appearance="Light"},true).Accent);
        Assert.Equal(dark,ThemeColors.For(settings with{Appearance="Dark"},false).Accent);
        Assert.Equal("#1D2730",ThemeColors.For(settings with{Appearance="Dark"},false).Surface);
        Assert.Equal("#FFFFFF",ThemeColors.For(settings with{Appearance="Light"},true).Surface);
    }
}
