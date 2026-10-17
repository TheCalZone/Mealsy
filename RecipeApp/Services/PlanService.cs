using System.Text.Json;

namespace RecipeApp.Services;

public record PlanStatus(bool OwnPlan, bool FamilyEnabled, bool AdFree, long ExpiresAt, bool Owner, string ProductId, string BasePlanId,int ExtraSeats=0);
public static class PlanService
{
    public static event Action Changed;
    private static string Key => "Mealsy.plan." + HouseholdService.UserId;
    public static PlanStatus Cached
    {
        get
        {
            if (HouseholdService.UserId == null) return null;
            try { return JsonSerializer.Deserialize<PlanStatus>(Preferences.Get(Key, "null")); } catch (JsonException) { return null; }
        }
    }
    // Only a short offline ad-free grace is cached. Firebase always enforces family access.
    public static bool AdFree => Cached is { AdFree: true } plan && plan.ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        && long.TryParse(Preferences.Get(Key + ".checked", "0"), out var verified) && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - verified < 3600000;
    public static async Task<PlanStatus> RefreshAsync(string token = null)
    {
        var uid = HouseholdService.UserId;
        var result = await HouseholdService.CallEndpointAsync<PlanStatus>(MonetizationConfig.BillingEndpoint, new { action = token == null ? "status" : "verify", token });
        if (uid != HouseholdService.UserId) throw new InvalidOperationException("Your account changed.");
        Preferences.Set(Key, JsonSerializer.Serialize(result)); Preferences.Set(Key + ".checked", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        Changed?.Invoke(); return result;
    }
}
