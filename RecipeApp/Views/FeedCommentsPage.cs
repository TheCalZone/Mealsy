using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

public class FeedCommentsPage : ContentPage
{
    private readonly string postId, account;
    private readonly bool moderator;
    private readonly VerticalStackLayout comments = new() { Spacing = 12 };
    private readonly Entry message = new() { Placeholder = "Add a kind comment…", MaxLength = 1000, TextColor = Colors.Black };
    private readonly Entry name = new() { Placeholder = "Public display name", MaxLength = 60, TextColor = Colors.Black };
    private readonly Label status = FoodFeedPage.Text("");
    private FeedCursor cursor;
    private readonly Button more;
    private string pendingId = Guid.NewGuid().ToString("N"), pendingText;
    private bool busy;
    public FeedCommentsPage(string postId, bool moderator)
    {
        this.postId = postId; this.moderator = moderator; account = HouseholdService.UserId;
        Title = "Around the table"; BackgroundColor = Color.FromArgb("#F5F7F2"); name.Text = Preferences.Get(FoodFeedService.AuthorKey, "");
        more = FoodFeedPage.Button("Older comments", () => Load(false));
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = {
            FoodFeedPage.Text("Join the conversation", 26, true), name, message,
            FoodFeedPage.Button("Post comment", async () => {
                CheckAccount();
                if (string.IsNullOrWhiteSpace(message.Text) || string.IsNullOrWhiteSpace(name.Text)) { await DisplayAlert("Comment", "Enter a display name and a comment.", "OK"); return; }
                if (pendingText != message.Text) { pendingId = Guid.NewGuid().ToString("N"); pendingText = message.Text; }
                var sent = message.Text;
                await FoodFeedService.Call<object>(new { action = "comment", postId, id = pendingId, text = sent, authorName = name.Text });
                if (message.Text == sent) message.Text = "";
                pendingId = Guid.NewGuid().ToString("N"); pendingText = null;
                Preferences.Set(FoodFeedService.AuthorKey, name.Text); await Load(true);
            }), status, comments, more } } };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await Load(true); }
        catch (Exception) { status.Text = "Comments unavailable. Reopen this screen to retry."; }
    }
    private void CheckAccount() { if (account == null || account != HouseholdService.UserId) throw new HouseholdException("UNAUTHENTICATED", "Your account changed. Reopen the comments."); }
    private async Task Load(bool reset)
    {
        CheckAccount(); if (busy) return; busy = true;
        try
        {
            var result = await FoodFeedService.Call<CommentPage>(new { action = "comments", postId,
                cursor = reset || cursor == null ? null : new { createdAt = cursor.CreatedAt, id = cursor.Id } });
            CheckAccount(); if (reset) comments.Clear(); cursor = result.Cursor; more.IsVisible = cursor != null;
            foreach (var comment in result.Comments)
            {
                var item = new VerticalStackLayout { Spacing = 4 };
                item.Add(FoodFeedPage.Text(comment.AuthorName, 16, true)); item.Add(FoodFeedPage.Text(comment.Text));
                item.Add(FoodFeedPage.Text(DateTimeOffset.FromUnixTimeMilliseconds(comment.CreatedAt).ToLocalTime().ToString("g"), 12));
                if (comment.AuthorId == account || moderator) item.Add(FoodFeedPage.Button("Remove comment", async () => {
                    if (!await DisplayAlert("Remove comment?", "This removes the shared comment.", "Remove", "Cancel")) return;
                    CheckAccount(); await FoodFeedService.Call<object>(new { action = "deleteComment", postId, commentId = comment.Id }); await Load(true);
                }));
                comments.Add(item);
            }
            status.Text = comments.Count == 0 ? "Start the conversation." : "Newest comments first";
        }
        finally { busy = false; }
    }
}
