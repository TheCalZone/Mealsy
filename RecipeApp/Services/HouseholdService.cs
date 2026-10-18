using Plugin.Firebase.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RecipeApp.Services;

public record HouseholdMember(string Id, string Label);
public record HouseholdInfo(string HouseholdId, string Name, string Owner, List<HouseholdMember> Members, bool FamilyEnabled,int MemberLimit=4);
public class HouseholdException : Exception
{
    public string Code { get; }
    public string Reason { get; }
    public string Reference { get; }
    public HouseholdException(string code, string message,string reason=null,string reference=null) : base(message) { Code=code;Reason=reason;Reference=reference; }
}

/// <summary>All permissions and membership changes are enforced by the callable backend.</summary>
public static class HouseholdService
{
    // Must match the Firebase project in google-services.json and the deployed function region.
    public const string Endpoint = "https://us-central1-mealsy-b732b.cloudfunctions.net/household";
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(40) };
    private static readonly JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };
    public static string UserId => AccountSession.UserId;
    public static Task<T> CallAsync<T>(object data) => CallEndpointAsync<T>(Endpoint, data);
    public static async Task<T> CallEndpointAsync<T>(string endpoint, object data, bool forceRefresh = false)
    {
        var service=endpoint==HouseholdMealsService.Endpoint?"householdMeals":endpoint==Endpoint?"household":endpoint==FoodFeedService.Endpoint?"foodFeed":endpoint==AdminService.Endpoint?"administration":endpoint==BusinessService.Endpoint?"business":endpoint==RecipeApp.Views.CampaignWalletPage.Endpoint?"promotions":endpoint.EndsWith("/notifications",StringComparison.Ordinal)?"notifications":"billing";
        var action=JsonSerializer.SerializeToElement(data).TryGetProperty("action",out var property)?property.GetString():null;
        var operation=DiagnosticPolicy.Operation(service,action);
        try {
        var user = CrossFirebaseAuth.Current.CurrentUser ?? throw new HouseholdException("UNAUTHENTICATED", "Sign in on the Account tab first. Each family member needs their own account.");
        var uid = user.Uid;
        var token = await user.GetIdTokenResultAsync(forceRefresh);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
#if ANDROID
        if(endpoint==RecipeApp.Views.CampaignWalletPage.Endpoint&&action is "prepareImpression" or "impression")
            request.Headers.Add("X-Firebase-AppCheck",await RecipeApp.Platforms.Android.PromotionAppVerification.Token());
#endif
        request.Content = JsonContent.Create(new { data });
        using var response = await client.SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (UserId != uid) throw new HouseholdException("UNAUTHENTICATED", "The signed-in account changed. Reopen this screen.");
        if (!response.IsSuccessStatusCode || body.RootElement.TryGetProperty("error", out _))
        {
            if (body.RootElement.TryGetProperty("error", out var error)) {
                var code=error.GetProperty("status").GetString();string reason=null,reference=null;
                if(error.TryGetProperty("details",out var details) && details.ValueKind==JsonValueKind.Object) {
                    if(details.TryGetProperty("reason",out var r) && r.ValueKind==JsonValueKind.String)reason=DiagnosticPolicy.Reason(r.GetString());
                    if(details.TryGetProperty("reference",out var id) && id.ValueKind==JsonValueKind.String && Guid.TryParseExact(id.GetString(),"N",out _))reference=id.GetString();
                }
                var message=DiagnosticPolicy.Message(reason,code,error.GetProperty("message").GetString()?.Replace("Google Play", "app store", StringComparison.OrdinalIgnoreCase));
                var failure=new HouseholdException(code,message,reason,reference);
                if(DiagnosticPolicy.Operational(code))reference=AppDiagnostics.Report(failure,operation,code,reason??"NONE",reference);
                if(!string.IsNullOrEmpty(reference))failure=new HouseholdException(code,message+"\nReference: "+reference,reason,reference);
                failure.Data["Mealsy.DiagnosticReference"]=reference??"";
                throw failure;
            }
            throw new HouseholdException("UNAVAILABLE", "The shared-list service is unavailable. Check your connection and try again.");
        }
        return body.RootElement.GetProperty("result").Deserialize<T>(json);
        } catch(HouseholdException error){
            if(DiagnosticPolicy.Operational(error.Code) && error.Data["Mealsy.DiagnosticReference"]==null){
                var reference=AppDiagnostics.Report(error,operation,error.Code,error.Reason??"NONE",error.Reference);
                throw new HouseholdException(error.Code,error.Message+"\nReference: "+reference,error.Reason,reference);
            }
            throw;
        }
        catch(Exception error) {
            var code=error is TaskCanceledException?"TIMEOUT":error is HttpRequestException?"NETWORK":error is JsonException or InvalidOperationException or KeyNotFoundException?"INVALID_RESPONSE":"AUTH_SERVICE_FAILURE";
            var reference=AppDiagnostics.Report(error,operation,code);
            var failure=new HouseholdException(code,DiagnosticPolicy.Message(null,code,"The account service could not complete the request. Sign in again and retry.")+"\nReference: "+reference,reference:reference);
            failure.Data["Mealsy.DiagnosticReference"]=reference;throw failure;
        }
    }
    public static Task<HouseholdInfo> StatusAsync() => CallAsync<HouseholdInfo>(new { action = "status" });
}
