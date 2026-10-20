using System.Net;
using System.Text.RegularExpressions;
namespace RecipeApp.Services;

/// <summary>Extracts browser-shared recipe URLs without fetching any page.</summary>
public static class SharedRecipeLink
{
    public static string Extract(string text)
    {
        if(string.IsNullOrWhiteSpace(text)||text.Length>10000)throw new InvalidOperationException("Share a recipe page link from your browser's Share menu.");
        var candidates=new List<string>();
        foreach(Match match in Regex.Matches(WebUtility.HtmlDecode(text),@"https://[^\s<>""']+",RegexOptions.IgnoreCase))
        {
            var value=match.Value.TrimEnd('.',',',';', '!');
            while(value.EndsWith(')')&&value.Count(c=>c==')')>value.Count(c=>c=='('))value=value[..^1];
            while(value.EndsWith(']')&&value.Count(c=>c==']')>value.Count(c=>c=='['))value=value[..^1];
            if(value.Length<=2000&&Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&uri.Host.Length>0&&uri.UserInfo.Length==0&&uri.IsDefaultPort)
                candidates.Add(uri.AbsoluteUri);
        }
        foreach(var candidate in candidates)
        {
            try{return WebsiteRecipe.Validate(candidate).AbsoluteUri;}catch(InvalidOperationException){ }
        }
        // Unsupported sites are still opened in the importer, where the existing
        // supported-site validation explains the manual-entry alternative.
        return candidates.FirstOrDefault()??throw new InvalidOperationException("No usable HTTPS recipe link was found. Share the page link rather than a screenshot or file.");
    }
}
