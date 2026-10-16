using Plugin.Firebase.Auth;
namespace RecipeApp.Helpers;
public static class AuthHelper
{
    public static IFirebaseAuth Client=>CrossFirebaseAuth.Current;
    public static string UserId=>RecipeApp.Services.AccountSession.UserId;
    public static string Email=>RecipeApp.Services.AccountSession.Email;
    public static void Reset()=>RecipeApp.Services.AccountSession.ClearOldPassword();
}
