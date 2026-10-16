using System.Text.Json;
using RecipeApp.Services;
using Xunit;

public class SettingsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("[]")]
    public void DamagedSettingsFallBackWithoutBlockingStartup(string json)
        => Assert.Equal(new SettingsOptions(), SettingsOptions.Read(json));

    [Fact]
    public void UnknownValuesFromFutureVersionsKeepValidPreferences()
    {
        var settings = SettingsOptions.Read("{\"Palette\":\"Future\",\"Appearance\":null,\"RecipeSort\":\"Unknown\",\"HideCollected\":true,\"NewPreference\":42}");
        Assert.Equal("Forest",settings.Palette);
        Assert.Equal("System",settings.Appearance);
        Assert.Equal("Recent",settings.RecipeSort);
        Assert.True(settings.HideCollected);
    }

    [Fact]
    public void SavedPreferencesSurviveSerializationAndPartialUpgrades()
    {
        var saved = new SettingsOptions { Palette="Berry", Appearance="Dark", LargerText=true, ShowPhotos=false, HideCollected=true, ShoppingDetails=true, RecipeSort="Quickest", KeepAwake=true };
        Assert.Equal(saved, SettingsOptions.Read(JsonSerializer.Serialize(saved)));
        var older = SettingsOptions.Read("{\"Palette\":\"Ocean\"}");
        Assert.Equal("Ocean",older.Palette);
        Assert.True(older.ShowPhotos);
        Assert.Equal("System",older.Appearance);
    }
}
