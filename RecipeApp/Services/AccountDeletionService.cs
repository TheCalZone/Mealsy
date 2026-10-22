using Plugin.Firebase.Auth;
namespace RecipeApp.Services;

public record OwnedDeletionSpace(string Id,string Name);
public record AccountDeletionPreview(List<OwnedDeletionSpace> OwnedHouseholds,List<OwnedDeletionSpace> OwnedBusinesses);
public record AccountDeletionReceipt(bool Accepted,string Reference);
public static class AccountDeletionService
{
    private const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/accountDeletion";
    public static bool IsPending(string uid)=>uid!=null&&Preferences.Get("Mealsy.deletion.pending","")==uid;
    public static async Task<AccountDeletionPreview> Prepare(string password)
    {
        var uid=AccountSession.UserId??throw new InvalidOperationException("Sign in before deleting your account.");
        var email=AccountSession.Email;
        if(string.IsNullOrWhiteSpace(password)||string.IsNullOrWhiteSpace(email))throw new InvalidOperationException("Enter your account password to confirm ownership.");
        try{await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email,password);}
        catch(Exception){throw new InvalidOperationException("Your password could not be confirmed. Check it and retry, or reset your password from Account.");}
        AccountSession.Check(uid);
        return await HouseholdService.CallEndpointAsync<AccountDeletionPreview>(Endpoint,new{action="prepare"},forceRefresh:true);
    }
    public static async Task<AccountDeletionReceipt> Request()
    {
        var uid=AccountSession.UserId??throw new InvalidOperationException("Sign in before deleting your account.");
        var receipt=await HouseholdService.CallEndpointAsync<AccountDeletionReceipt>(Endpoint,new{action="request",confirmation="DELETE",deleteOwnedSpaces=true},forceRefresh:false);
        if(!receipt.Accepted||string.IsNullOrWhiteSpace(receipt.Reference))throw new InvalidOperationException("Deletion was not confirmed by the server. Your local data has been kept.");
        Preferences.Set("Mealsy.deletion.reference",receipt.Reference);
        Preferences.Set("Mealsy.deletion.pending",uid);
        await FinishLocal();return receipt;
    }
    public static async Task FinishLocal()
    {
        var uid=Preferences.Get("Mealsy.deletion.pending","");if(uid.Length==0)return;
        HouseholdWarmup.ResetSession();
        // Wait for an existing account sync to finish before erasing its cache.
        await AccountSession.WaitForSyncBeforeDeletion();
        await LocalAccountCleanup.Remove(uid);
        if(AccountSession.UserId==uid)await AccountSession.SignOut();
        Preferences.Remove("Mealsy.deletion.pending");
    }
}
