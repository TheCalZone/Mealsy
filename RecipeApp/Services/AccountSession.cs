using Plugin.Firebase.Auth;
using Plugin.Firebase.Core.Exceptions;
using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;

public static class AccountSession
{
    private static readonly SemaphoreSlim sync=new(1,1);
    public static string UserId { get { try{return CrossFirebaseAuth.Current.CurrentUser?.Uid;}catch(Exception){return null;} } }
    public static string Email { get { try{return CrossFirebaseAuth.Current.CurrentUser?.Email;}catch(Exception){return null;} } }
    public static bool Owns(string owner)=>UserId==null ? string.IsNullOrEmpty(owner) : owner==UserId;
    public static string Status=>UserId==null?"Device mode · Your data stays on this device":Preferences.Get("Account.sync."+UserId,"Signed in · Cloud sync hasn't run yet");
    public static void Check(string uid){if(UserId!=uid)throw new InvalidOperationException("Your account changed. Reopen this screen before continuing.");}
    public static AppUser CachedProfile
    {
        get{var uid=UserId;if(uid==null)return null;try{return JsonSerializer.Deserialize<AppUser>(Preferences.Get("Account.profile."+uid,"null"))??new AppUser {FirebaseUID=uid,Email=Email};}catch(JsonException){return new AppUser {FirebaseUID=uid,Email=Email};}}
    }
    public static void SaveProfile(AppUser profile)
    {
        Check(profile.FirebaseUID);if(UserId==null)throw new InvalidOperationException("Sign in before editing a profile.");
        if((profile.FirstName??"").Length>80||(profile.LastName??"").Length>80||(profile.DietRestrictions??"").Length>500||(profile.FavouriteCuisines??"").Length>500)throw new InvalidOperationException("Shorten your profile details.");
        profile.Email=Email;Preferences.Set("Account.profile."+UserId,JsonSerializer.Serialize(profile));Preferences.Set("Account.profile.pending."+UserId,"true");
    }
    public static async Task<AppUser> LoadProfile()
    {
        var uid=UserId;if(uid==null)return null;
        if(Preferences.Get("Account.profile.pending."+uid,"")=="true")return CachedProfile;
        try{var profile=await FirestoreService.GetUserAsync(uid);Check(uid);if(Preferences.Get("Account.profile.pending."+uid,"")=="true")return CachedProfile;Preferences.Set("Account.profile."+uid,JsonSerializer.Serialize(profile));return profile;}
        catch(Exception){Check(uid);return CachedProfile;}
    }
    public static async Task SignIn(string email,string password)
    {
        if(UserId!=null)throw new InvalidOperationException("Sign out before signing in to another account.");
        var address=AccountInput.Email(email);if(string.IsNullOrEmpty(password))throw new InvalidOperationException("Enter your password.");
        try{await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(address,password);}catch(FirebaseAuthException e){throw new InvalidOperationException(AccountInput.Error(e.Reason.ToString()));}
        catch(Exception){throw new InvalidOperationException("Firebase authentication is unavailable. Check your connection and the app configuration.");}
        ClearOldPassword();_=SyncSafely();
    }
    public static async Task Register(string email,string password,string confirmation,string first,string last,string diet,string cuisines)
    {
        if(UserId!=null)throw new InvalidOperationException("Sign out before creating another account.");
        AccountInput.Registration(email,password,confirmation,first);
        if((last??"").Length>80||(diet??"").Length>500||(cuisines??"").Length>500)throw new InvalidOperationException("Shorten your optional profile details.");
        try{await CrossFirebaseAuth.Current.CreateUserAsync(AccountInput.Email(email),password);}catch(FirebaseAuthException e){throw new InvalidOperationException(AccountInput.Error(e.Reason.ToString()));}
        catch(Exception){throw new InvalidOperationException("Firebase authentication is unavailable. Check your connection and the app configuration.");}
        // Creating the Auth account succeeded. Profile or sync failure must not suggest registering again.
        try{SaveProfile(new AppUser {FirebaseUID=UserId,Email=Email,FirstName=first.Trim(),LastName=last?.Trim()??"",DietRestrictions=diet?.Trim()??"",FavouriteCuisines=cuisines?.Trim()??""});}catch(Exception){ }
        ClearOldPassword();_=SyncSafely();
    }
    public static async Task ResetPassword(string email)
    {
        var address=AccountInput.Email(email);
        try{await CrossFirebaseAuth.Current.SendPasswordResetEmailAsync(address);}
        catch(FirebaseAuthException e){if(e.Reason.ToString()!="UserNotFound")throw new InvalidOperationException(AccountInput.Error(e.Reason.ToString()));}
    }
    public static async Task SignOut(){await CrossFirebaseAuth.Current.SignOutAsync();ClearOldPassword();}
    public static void ClearOldPassword(){try{SecureStorage.Remove("email");SecureStorage.Remove("password");}catch(Exception){}}
    public static async Task<string> SyncSafely()
    {
        var uid=UserId;if(uid==null)return Status;await sync.WaitAsync();
        try
        {
            Check(uid);
            if(Preferences.Get("Account.profile.pending."+uid,"")=="true")
            {
                var pending=Preferences.Get("Account.profile."+uid,"null");var profile=CachedProfile;await FirestoreService.SetUserData(uid,new Dictionary<object,object>{{"first_name",profile.FirstName??""},{"last_name",profile.LastName??""},{"email",Email??""},{"diet_restrictions",profile.DietRestrictions??""},{"favourite_cuisines",profile.FavouriteCuisines??""},{"firebase_uid",uid}});Check(uid);if(Preferences.Get("Account.profile."+uid,"null")==pending)Preferences.Set("Account.profile.pending."+uid,"false");
            }
            await FirestoreService.SyncRecipes();Check(uid);await FirestoreService.SyncShoppingLibrary();Check(uid);
            var text="Cloud sync completed · "+DateTime.Now.ToString("t");Preferences.Set("Account.sync."+uid,text);return text;
        }
        catch(Exception){if(UserId==uid)Preferences.Set("Account.sync."+uid,"Signed in · Cloud sync unavailable. Your local data is kept; retry Sync account.");return UserId==uid?Status:"Account changed";}
        finally{sync.Release();}
    }
}
