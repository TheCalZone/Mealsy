using RecipeApp.Models;

namespace RecipeApp.Services;

public record AdminStatus(bool Admin);
public record AdminUser(string Uid, string Email, string Username, bool EmailVerified, bool Disabled, bool Admin, long GrantExpiresAt, long SubscriptionExpiresAt,bool CanVerifyEmail=false);
public class AdminPost : FoodPost
{
    public bool Hidden { get; set; }
    public bool Deleted { get; set; }
    public bool PromotionActive { get; set; }
    public List<AdminReport> Reports { get; set; } = new();
}
public record AdminReport(string Id, string Reason, long CreatedAt);
public record AdminPosts(List<AdminPost> Posts, FeedCursor Cursor);
public record AdminAuditEntry(string Id, string Actor, string Action, string Target, string Reason, long CreatedAt);
public record AdminAudit(List<AdminAuditEntry> Entries, FeedCursor Cursor);
public static class AdminService
{
    public const string Endpoint = "https://us-central1-mealsy-b732b.cloudfunctions.net/administration";
    public static Task<T> Call<T>(object data) => HouseholdService.CallEndpointAsync<T>(Endpoint, data, true);
    public static Task<AdminStatus> Status() => Call<AdminStatus>(new { action = "status" });
}
