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
}
public record FoodComment(string Id, string Text, string AuthorId, string AuthorName, long CreatedAt);
public record FeedPage(List<FoodPost> Posts, FeedCursor Cursor, bool Moderator);
public record CommentPage(List<FoodComment> Comments, FeedCursor Cursor);
