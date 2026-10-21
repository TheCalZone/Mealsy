using System.Globalization;
using RecipeApp.Services;
using Xunit;

public class LocalizationTests
{
    [Theory]
    [InlineData("system","es-MX","es")]
    [InlineData("system","fr-CA","fr")]
    [InlineData("system","pt-BR","pt")]
    [InlineData("system","ja-JP","en")]
    [InlineData("de","en-US","de")]
    [InlineData("unknown","fr-FR","fr")]
    public void PhoneLanguageAndExplicitOverridesHavePredictableFallback(string preference,string device,string expected)
        =>Assert.Equal(expected,AppLocalization.Resolve(preference,CultureInfo.GetCultureInfo(device)).Name);
    [Theory]
    [InlineData("es-MX","Inicio")]
    [InlineData("fr-CA","Accueil")]
    [InlineData("de-DE","Start")]
    [InlineData("pt-BR","Início")]
    public void RealSatelliteResourcesLoadAndMissingKeysPreserveContent(string language,string home)
    {
        var previous=CultureInfo.CurrentUICulture;
        try{
            CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(language);
            Assert.Equal(home,AppLocalization.Text("Home"));
            Assert.Equal("Grandma’s special recipe",AppLocalization.Text("Grandma’s special recipe"));
            Assert.DoesNotContain("{0}",AppLocalization.Format("{0} items left",3));
        }finally{CultureInfo.CurrentUICulture=previous;}
    }
    [Fact]
    public void EveryShippedLanguageContainsEveryKeyAndPreservesFormatArguments()
    {
        var manager=new System.Resources.ResourceManager("RecipeApp.Resources.Localization.Strings",typeof(AppLocalization).Assembly);
        var baseline=manager.GetResourceSet(CultureInfo.InvariantCulture,true,false);
        foreach(var language in new[]{"es","fr","de","pt"})
        {
            var translated=manager.GetResourceSet(CultureInfo.GetCultureInfo(language),true,false);
            Assert.NotNull(translated);
            foreach(System.Collections.DictionaryEntry entry in baseline)
            {
                var value=translated.GetString((string)entry.Key);
                Assert.False(string.IsNullOrWhiteSpace(value),$"{language}: {entry.Key}");
                string[] Arguments(string text)=>System.Text.RegularExpressions.Regex.Matches(text,@"\{\d+\}").Select(m=>m.Value).Order().ToArray();
                Assert.Equal(Arguments((string)entry.Value),Arguments(value));
            }
        }
    }
    [Fact]
    public void LanguagePreferenceRoundTripsWithoutChangingExistingChoices()
    {
        var saved=SettingsOptions.Read("{\"Language\":\"pt\",\"Palette\":\"Ocean\",\"RecipeSort\":\"Name\"}");
        Assert.Equal("pt",saved.Language);Assert.Equal("Ocean",saved.Palette);Assert.Equal("Name",saved.RecipeSort);
        Assert.Equal("system",SettingsOptions.Read("{\"Language\":\"not-supported\"}").Language);
        Assert.Equal("system",SettingsOptions.Read("{}").Language);
    }
    [Fact]
    public void LanguageResolutionDoesNotChangeRegionalNumberParsing()
    {
        var previous=CultureInfo.CurrentCulture;
        try{
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
            _=AppLocalization.Resolve("en",CultureInfo.GetCultureInfo("fr-FR"));
            Assert.Equal("fr-FR",CultureInfo.CurrentCulture.Name);
            Assert.True(ShoppingQuantity.TryParse("1,5 kg",out var amount));Assert.Equal(1500m,amount.Value);
        }finally{CultureInfo.CurrentCulture=previous;}
    }
}
