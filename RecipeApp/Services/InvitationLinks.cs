using System.Text.RegularExpressions;
namespace RecipeApp.Services;

public static class InvitationLinks
{
    public const string DownloadPage="https://mealsy-b732b.web.app/join/";
    public const string PlayStore="https://play.google.com/store/apps/details?id=com.companyname.recipeapp";
    public static string Code(string value)
    {
        var text=(value??"").Trim();if(text.Length>2000)throw new InvalidOperationException("This invitation is too long.");
        if(Uri.TryCreate(text,UriKind.Absolute,out var uri))
        {
            var allowed=uri.Scheme=="https"&&(uri.Host is "mealsy-b732b.web.app" or "mealsy-b732b.firebaseapp.com")&&uri.AbsolutePath.TrimEnd('/')=="/join";
            allowed|=uri.Scheme=="mealsy"&&uri.Host=="join";
            if(!allowed||!string.IsNullOrEmpty(uri.UserInfo))throw new InvalidOperationException("Use a Mealsy invitation link or the household code.");
            var values=(uri.Fragment.Length>1?uri.Fragment[1..]:uri.Query.TrimStart('?')).Split('&');
            text=values.Where(p=>p.StartsWith("code=",StringComparison.Ordinal)).Select(p=>Uri.UnescapeDataString(p[5..])).FirstOrDefault()??"";
        }
        if(!Regex.IsMatch(text,@"\A[a-fA-F0-9]{48}\z"))throw new InvalidOperationException("Paste the 48-character household code or its Mealsy invitation link.");
        return text.ToLowerInvariant();
    }
    public static string ForHousehold(string code)=>DownloadPage+"#code="+Code(code);
    public static string ShareHousehold(string name,string code)=>$"Join {name} on Mealsy!\n\nGet the Android app and open your invitation:\n{ForHousehold(code)}\n\nHousehold code: {Code(code)}\n\nCreate or sign in with your own account, then enter this code in Get started or My household. This single-use code expires in 7 days. Share it only with the intended family member. Creating an account is free.";
}
