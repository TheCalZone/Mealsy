using System.Net.Mail;
namespace RecipeApp.Services;
public static class AccountInput
{
    public static string Email(string value)
    {
        var text=(value??"").Trim();
        if(text.Length>254||!MailAddress.TryCreate(text,out var address)||address.Address!=text||!address.Host.Contains('.'))throw new InvalidOperationException("Enter a complete email address.");
        return text;
    }
    public static void Registration(string email,string password,string confirmation,string firstName)
    {
        Email(email);
        if(string.IsNullOrWhiteSpace(firstName)||firstName.Trim().Length>80)throw new InvalidOperationException("Enter your first name (up to 80 characters).");
        if((password??"").Length<8)throw new InvalidOperationException("Use at least 8 characters for your new password.");
        if(password!=confirmation)throw new InvalidOperationException("The two passwords don't match.");
    }
    public static string Error(string reason)=>reason switch
    {
        "EmailAlreadyInUse"=>"An account already uses that email. Sign in or reset its password.",
        "InvalidEmail"=>"Check your email address and try again.",
        "WeakPassword"=>"Firebase rejected this password. Use a stronger password that meets your project's password policy.",
        "WrongPassword" or "UserNotFound" or "InvalidCredential"=>"The email or password wasn't accepted. Check both, or request a password reset.",
        "UserDisabled"=>"This account is disabled. Contact the app owner.",
        "OperationNotAllowed"=>"Email/password sign-in isn't enabled in Firebase Authentication. The app owner needs to enable it.",
        "InvalidApiKey" or "AppNotAuthorized"=>"Firebase rejected this app's configuration. Check the Firebase Android app and API key restrictions.",
        "NetworkError"=>"Couldn't reach Firebase. Check your connection and try again.",
        "TooManyRequests"=>"Too many attempts. Wait a little before trying again.",
        "UserTokenExpired" or "InvalidUserToken"=>"Your sign-in expired. Sign in again.",
        _=>"Firebase couldn't complete this request. Check your connection; the app owner may need to check the Firebase configuration."
    };
}
