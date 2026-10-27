namespace RecipeApp.Services;
public record ShoppingConversationSummary(string ItemId,string Title,int Count,long UpdatedAt);
public record ShoppingConversationList(List<ShoppingConversationSummary> Threads);
public record ShoppingQuestionMember(string Id,string Name,string Color=null);
public record ShoppingQuestionMessage(string Id,string AuthorId,string AuthorName,string Text,long CreatedAt);
public record ShoppingQuestionThread(string Title,string TargetId,List<ShoppingQuestionMember> Members,bool CanClear,List<ShoppingQuestionMessage> Messages);
public record ShoppingQuestionAlert(string Id,string Type,string Message,string HouseholdId,string ItemId,long CreatedAt,bool Read);
public record ShoppingQuestionInbox(List<ShoppingQuestionAlert> Items);
public static class HouseholdShoppingQuestions
{
    public const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/householdQuestions";
    public const string InboxEndpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/notifications";
    public static Task<T> Call<T>(object data)=>HouseholdService.CallEndpointAsync<T>(Endpoint,data);
}
