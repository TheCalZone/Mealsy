using Plugin.Firebase.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RecipeApp.Services;

public record HouseholdMember(string Id, string Label);
public record HouseholdInfo(string HouseholdId, string Name, string Owner, List<HouseholdMember> Members);
public class HouseholdException : Exception
{
    public string Code { get; }
    public HouseholdException(string code, string message) : base(message) => Code = code;
}

/// <summary>All permissions and membership changes are enforced by the callable backend.</summary>
public static class HouseholdService
{
    // Must match the Firebase project in google-services.json and the deployed function region.
    public const string Endpoint = "https://us-central1-mealsy-b732b.cloudfunctions.net/household";
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(40) };
    private static readonly JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };
    public static string UserId => CrossFirebaseAuth.Current.CurrentUser?.Uid;
    public static async Task<T> CallAsync<T>(object data)
    {
        var user = CrossFirebaseAuth.Current.CurrentUser ?? throw new HouseholdException("UNAUTHENTICATED", "Sign in on the Account tab first. Each family member needs their own account.");
        var uid = user.Uid;
        var token = await user.GetIdTokenResultAsync(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Content = JsonContent.Create(new { data });
        using var response = await client.SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (UserId != uid) throw new HouseholdException("UNAUTHENTICATED", "The signed-in account changed. Reopen this screen.");
        if (!response.IsSuccessStatusCode || body.RootElement.TryGetProperty("error", out _))
        {
            if (body.RootElement.TryGetProperty("error", out var error))
                throw new HouseholdException(error.GetProperty("status").GetString(), error.GetProperty("message").GetString());
            throw new HouseholdException("UNAVAILABLE", "Household service is unavailable. Check your connection and Firebase deployment.");
        }
        return body.RootElement.GetProperty("result").Deserialize<T>(json);
    }
    public static Task<HouseholdInfo> StatusAsync() => CallAsync<HouseholdInfo>(new { action = "status" });
}
