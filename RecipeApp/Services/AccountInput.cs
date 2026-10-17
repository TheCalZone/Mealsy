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
        "WeakPassword"=>"This password wasn't accepted. Use a stronger password.",
        "WrongPassword" or "UserNotFound" or "InvalidCredential"=>"The email or password wasn't accepted. Check both, or request a password reset.",
        "UserDisabled"=>"This account is disabled. Contact the app owner.",
        "OperationNotAllowed"=>"Sign-in is temporarily unavailable. Please try again later.",
        "InvalidApiKey" or "AppNotAuthorized"=>"The account service is unavailable. Please try again later.",
        "NetworkError"=>"Couldn't reach the account service. Check your connection and try again.",
        "TooManyRequests"=>"Too many attempts. Wait a little before trying again.",
        "UserTokenExpired" or "InvalidUserToken"=>"Your sign-in expired. Sign in again.",
        _=>"The account service couldn't complete this request. Check your connection and try again."
    };
}
