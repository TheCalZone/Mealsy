using System.Globalization;
using System.Resources;

namespace RecipeApp.Services;

/// <summary>UI resources only. Never translate recipe content, category IDs or persisted values.</summary>
public static class AppLocalization
{
    private static readonly ResourceManager resources=new("RecipeApp.Resources.Localization.Strings",typeof(AppLocalization).Assembly);
    public static readonly string[] Languages={"system","en","es","fr","de","pt"};
    public static string Normalize(string language)=>Languages.Contains(language)?language:"system";
    public static CultureInfo Resolve(string language,CultureInfo device)
    {
        var code=Normalize(language);
        if(code=="system")code=device.TwoLetterISOLanguageName;
        return CultureInfo.GetCultureInfo(Languages.Contains(code)&&code!="system"?code:"en");
    }
    public static string Text(string source)
    {
        if(string.IsNullOrEmpty(source))return source??"";
        try{return resources.GetString(source,CultureInfo.CurrentUICulture)??source;}
        catch(MissingManifestResourceException){return source;}
    }
    public static string Format(string source,params object[] arguments)=>string.Format(CultureInfo.CurrentCulture,Text(source),arguments);
}
