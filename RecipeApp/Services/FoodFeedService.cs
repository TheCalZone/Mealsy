using System.Text.Json;
using RecipeApp.Models;

namespace RecipeApp.Services;

public static class FoodFeedService
{
    public const string Endpoint = "https://us-central1-mealsy-b732b.cloudfunctions.net/foodFeed";
    // Web and Android send the same lowercase payload format, independent of local recipe IDs.
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    public static Task<T> Call<T>(object data) => HouseholdService.CallEndpointAsync<T>(Endpoint, data);
    public static Task<FeedPage> Load(FeedCursor cursor = null, string mode = "forYou", string search = "") => Call<FeedPage>(new { action = "list", mode, search, cursor = cursor == null ? null : new { createdAt = cursor.CreatedAt, id = cursor.Id } });
    public static Task<FeedPreferences> Preferences() => Call<FeedPreferences>(new { action = "preferences" });
    public static Task<CommunityProfile> Profile() => HouseholdService.CallEndpointAsync<CommunityProfile>(Endpoint, new { action = "profile" }, true);
    public static string AuthorKey => "Feed.author." + HouseholdService.UserId;
}
