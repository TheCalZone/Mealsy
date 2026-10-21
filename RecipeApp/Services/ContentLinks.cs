using System.Text.RegularExpressions;
namespace RecipeApp.Services;

public record ContentTarget(string Kind,string Id);
public static class ContentLinks
{
    public const string LandingPage="https://mealsy-b732b.web.app/share/";
    public static ContentTarget Target(string kind,string id)
    {
        if(kind is not ("post" or "social" or "plan") || !Regex.IsMatch(id??"",@"\A[A-Za-z0-9_-]{1,100}\z"))
            throw new InvalidOperationException("Use a valid Mealsy post or meal-plan link.");
        return new(kind,id);
    }
    public static string Url(ContentTarget target){Target(target.Kind,target.Id);return $"{LandingPage}?kind={target.Kind}&id={target.Id}";}
    public static ContentTarget Parse(string value)
    {
        if(value==null||value.Length>2000||!Uri.TryCreate(value,UriKind.Absolute,out var uri)||!string.IsNullOrEmpty(uri.UserInfo)||!uri.IsDefaultPort||!string.IsNullOrEmpty(uri.Fragment))throw new InvalidOperationException("Use a Mealsy sharing link.");
        var allowed=uri.Scheme=="https"&&(uri.Host is "mealsy-b732b.web.app" or "mealsy-b732b.firebaseapp.com")&&uri.AbsolutePath.TrimEnd('/')=="/share";
        allowed|=uri.Scheme=="mealsy"&&uri.Host=="content"&&uri.AbsolutePath is "" or "/";
        if(!allowed)throw new InvalidOperationException("Use a Mealsy sharing link.");
        var pairs=uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(p=>p.Split('=',2)).ToArray();
        if(pairs.Length!=2||pairs.Any(p=>p.Length!=2)||pairs.Count(p=>p[0]=="kind")!=1||pairs.Count(p=>p[0]=="id")!=1)throw new InvalidOperationException("This Mealsy link is incomplete.");
        return Target(Uri.UnescapeDataString(pairs.Single(p=>p[0]=="kind")[1]),Uri.UnescapeDataString(pairs.Single(p=>p[0]=="id")[1]));
    }
}
