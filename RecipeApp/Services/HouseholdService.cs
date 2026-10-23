using Plugin.Firebase.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RecipeApp.Services;

public record HouseholdMember(string Id, string Label,string Role="adult");
public record HouseholdInfo(string HouseholdId, string Name, string Owner, List<HouseholdMember> Members, bool FamilyEnabled,int MemberLimit=4,string Role="adult",int PendingRequests=0,int PermissionsVersion=0,bool CanRenameHousehold=false)
{public bool Restricted=>Role=="restricted";}
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
    internal static readonly CloudReadCache Reads=new();
    public static Task<T> CallAsync<T>(object data,bool bypassReadCache=false) => CallEndpointAsync<T>(Endpoint,data,bypassReadCache:bypassReadCache);
    public static async Task<T> CallEndpointAsync<T>(string endpoint,object data,bool forceRefresh=false,bool bypassReadCache=false)
    {
        var uid=UserId;var request=JsonSerializer.SerializeToElement(data);
        var key=HouseholdReadPolicy.Key(uid,endpoint,request);
        if(key!=null)
        {
            for(var attempt=0;;attempt++)
            {
                try{var value=await Reads.Read(key,async()=>{AccountSession.Check(uid);var fetched=await CallEndpointUncachedAsync<T>(endpoint,data,forceRefresh);AccountSession.Check(uid);return fetched;},HouseholdReadPolicy.MaxAge,bypassReadCache);AccountSession.Check(uid);return value;}
                catch(CloudReadInvalidatedException) when(attempt==0&&UserId==uid){ }
            }
        }
        var action=request.TryGetProperty("action",out var a)?a.GetString():null;
        string Text(string name)=>request.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
        var listKey=endpoint==Endpoint&&action=="patch"?HouseholdReadPolicy.List(uid,Text("scope")=="household"?Text("householdId"):null):null;
        var weekKey=endpoint==HouseholdMealsService.Endpoint&&action is "upsertMeal" or "deleteMeal" or "requestMeal"?HouseholdReadPolicy.Week(uid,Text("householdId"),Text("weekStart")):null;
        var membership=endpoint==Endpoint&&action is "create" or "join" or "leave" or "remove" or "delete" or "rename" or "setRole" or "resolveRequest" or "cancelRequest";
        if(listKey!=null)Reads.Invalidate(listKey);if(weekKey!=null)Reads.Invalidate(weekKey);if(membership)Reads.Clear();
        var result=await CallEndpointUncachedAsync<T>(endpoint,data,forceRefresh);
        if(listKey!=null){Reads.Invalidate(listKey);Reads.Store(listKey,result,HouseholdReadPolicy.MaxAge);}
        if(weekKey!=null)Reads.Invalidate(weekKey);
        if(membership)Reads.Clear();
        if(membership||listKey!=null||weekKey!=null)HouseholdWarmup.Invalidate(uid,membership,weekKey!=null);
        if(listKey!=null&&Text("scope")=="household")
        {
            var payload=JsonSerializer.SerializeToElement(result);
            if(payload.TryGetProperty("Items",out var items)||payload.TryGetProperty("items",out items))HouseholdWarmup.RecordList(uid,Text("householdId"),items.GetRawText());
        }
        return result;
    }
    private static async Task<T> CallEndpointUncachedAsync<T>(string endpoint, object data, bool forceRefresh = false)
    {
        var service=endpoint.EndsWith("/accountDeletion",StringComparison.Ordinal)?"accountDeletion":endpoint==HouseholdMealsService.Endpoint?"householdMeals":endpoint==Endpoint?"household":endpoint==FoodFeedService.Endpoint?"foodFeed":endpoint==AdminService.Endpoint?"administration":endpoint==BusinessService.Endpoint?"business":endpoint==RecipeApp.Views.CampaignWalletPage.Endpoint?"promotions":endpoint.EndsWith("/notifications",StringComparison.Ordinal)?"notifications":"billing";
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
            if(error.Code is "UNAUTHENTICATED" or "PERMISSION_DENIED" or "FAILED_PRECONDITION"){Reads.Clear();HouseholdWarmup.Invalidate(UserId,true,false);}
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
    public static Task<HouseholdInfo> StatusAsync(bool force=false) => CallAsync<HouseholdInfo>(new { action = "status" },force);
}
