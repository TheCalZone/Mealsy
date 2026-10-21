namespace RecipeApp.Models;

public record FeedCursor(long CreatedAt, string Id);
public record FeedIngredient(string Name, string Quantity, string Category, string Note, string Section = "Ingredients", bool IsKey = true);
public record FeedRecipe(string Title, string Method, int Servings, int PrepMinutes, List<FeedIngredient> Ingredients, string Description = "", int CookMinutes = 0)
{
    public static FeedRecipe FromRecipe(Recipe value) => new(value.RecipeName ?? "", value.RecipeMethod ?? "", (int)value.Serving, (int)value.PrepTime,
        value.Ingredients.Select(i => new FeedIngredient(i.Name ?? "", i.Quantity ?? "", i.Tag ?? "Other", i.Note ?? "", string.IsNullOrWhiteSpace(i.IngredientSection) ? "Ingredients" : i.IngredientSection, i.MainIngredient)).ToList(), value.Description ?? "", (int)value.CookTime);
    public Recipe ToRecipe(string photo) => new() { RecipeName = Title, RecipeMethod = Method, Serving = Servings, PrepTime = PrepMinutes,
        Description = Description, CookTime = CookMinutes, RecipeImage = photo, LastUpdated = DateTimeOffset.UtcNow,
        Ingredients = Ingredients.Select(i => new Ingredient { Name = i.Name, Quantity = i.Quantity, Tag = i.Category, Note = i.Note, IngredientSection = i.Section, MainIngredient = i.IsKey }).ToList() };
}
public class FoodPost
{
    public string Audience { get; set; } = "public";
    public bool PrivacyManaged { get; set; }
    public string Id { get; set; }
    public string Caption { get; set; }
    public string AuthorId { get; set; }
    public string AuthorName { get; set; }
    public long CreatedAt { get; set; }
    public string Photo { get; set; }
    public FeedRecipe Recipe { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool Liked { get; set; }
    public bool Following { get; set; }
    public bool Promoted { get; set; }
    public string BusinessId { get; set; }
    public bool BusinessVerified { get; set; }
    public string PromotionCampaignId { get; set; }
    public List<string> Topics { get; set; } = new();
    public Recipe PrivateCopy(string account){var copy=Recipe.ToRecipe(Photo);copy.FirebaseUID=account;copy.Notes=$"Shared by {AuthorName} on Mealsy · Shared post {Id}";if(!string.IsNullOrEmpty(BusinessId))copy.CloudKey="business-copy-"+Id;return copy;}
}
public record FollowedCook(string Id, string Name);
public record CommunityProfile(string Username, string Discriminator, bool EmailVerified, bool PromotionCheckoutEnabled);
public record FeedPreferences(List<string> Interests, bool SetupComplete, List<string> Options, List<FollowedCook> Following,List<string> ExcludedIngredients=null);
public record FoodComment(string Id, string Text, string AuthorId, string AuthorName, long CreatedAt,string ParentCommentId=null,string ReplyToName=null);
public record FeedPage(List<FoodPost> Posts, FeedCursor Cursor, bool Moderator);
public record CommentPage(List<FoodComment> Comments, FeedCursor Cursor);
