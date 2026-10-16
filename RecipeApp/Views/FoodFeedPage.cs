using System.Collections.ObjectModel;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

public class FoodFeedPage : ContentPage
{
    private readonly ObservableCollection<FoodPost> posts = new();
    private readonly Label status = Text("Discover what’s cooking", 16);
    private readonly Button more;
    private FeedCursor cursor;
    private bool busy, moderator;
    private string account;
    private int count;
    public FoodFeedPage()
    {
        Title = "Food feed"; ShoppingUi.Theme(this);
        more = Button("Load more", () => Load(false));
        var layout = new VerticalStackLayout { Padding = 16, Spacing = 14, Children = {
            Text("The Mealsy table", 30, true), Text("Recipes, kitchen wins and inspiration from the community.", 15),
            Button("Share something delicious", async () => await Navigation.PushAsync(new FeedComposerPage())),
            status, new RecipeApp.Controls.AdBanner() } };
        var collection = new CollectionView { ItemsSource = posts, SelectionMode = SelectionMode.None, Header = layout, Footer = more,
            ItemTemplate = new DataTemplate(() => {
                var holder = new ContentView { Padding = new Thickness(16, 0, 16, 16) };
                holder.BindingContextChanged += (_, _) => holder.Content = holder.BindingContext is FoodPost post ? Card(post) : null;
                return holder;
            }) };
        var refresh = new RefreshView { Content = collection };
        refresh.Refreshing += async (_, _) => { await Load(true); refresh.IsRefreshing = false; };
        Content = refresh;
        ToolbarItems.Add(new ToolbarItem("Refresh", null, async () => await Load(true)));
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Load(true); }
    internal static Label Text(string value, int size = 15, bool bold = false) => ShoppingUi.Text(value, size, bold);
    internal static Button Button(string title, Func<Task> action)
    {
        var button = new Button { Text = title, HeightRequest = 46, CornerRadius = 12, TextColor = Colors.White };
        button.SetDynamicResource(VisualElement.BackgroundColorProperty, "ThemeAccent");
        button.Clicked += async (_, _) => {
            button.IsEnabled = false;
            try { await action(); }
            catch (HouseholdException e) { await Shell.Current.DisplayAlert("Food feed", e.Message, "OK"); }
            catch (Exception) { await Shell.Current.DisplayAlert("Food feed unavailable", "Check your connection and try again. Your local recipes are still available.", "OK"); }
            finally { button.IsEnabled = true; }
        };
        return button;
    }
    private async Task Load(bool reset)
    {
        if (busy) return;
        var uid = HouseholdService.UserId;
        if (uid != account || reset) { posts.Clear(); cursor = null; count = 0; account = uid; }
        if (uid == null) { status.Text = "Sign in on the Account tab to join the Mealsy community."; more.IsVisible = false; return; }
        busy = true; status.Text = "Setting the table…";
        try
        {
            var page = await FoodFeedService.Load(reset ? null : cursor);
            if (HouseholdService.UserId != uid) { posts.Clear(); return; }
            moderator = page.Moderator; cursor = page.Cursor;
            foreach (var post in page.Posts) { posts.Add(post); count++; }
            more.IsVisible = cursor != null;
            status.Text = count == 0 ? cursor != null ? "No visible posts on this page. Load more to keep exploring." : "Be the first to share a meal. Pull down to refresh." : "Newest first · Pull down to refresh";
        }
        catch (HouseholdException e) { status.Text = e.Message; more.IsVisible = true; }
        catch (Exception) { status.Text = "Feed unavailable. Check your connection; pull down to retry."; more.IsVisible = true; }
        finally { busy = false; }
    }
    private View Card(FoodPost post)
    {
        var body = new VerticalStackLayout { Spacing = 10 };
        body.Add(Text(post.AuthorName, 17, true));
        body.Add(Text(DateTimeOffset.FromUnixTimeMilliseconds(post.CreatedAt).ToLocalTime().ToString("g"), 12));
        if (!string.IsNullOrEmpty(post.Photo))
        {
            var photo = RecipeCardView.Photo(post.Photo, 220);
            if (photo != null) body.Add(photo);
        }
        if (!string.IsNullOrEmpty(post.Caption)) body.Add(Text(post.Caption));
        if (post.Recipe != null) body.Add(Button("View recipe · " + post.Recipe.Title, async () => await Navigation.PushAsync(new FeedRecipePage(post))));
        Button like = null;
        like = Button(LikeText(post), async () => {
            EnsureAccount(); var liked = !post.Liked;
            await FoodFeedService.Call<object>(new { action = "like", postId = post.Id, liked });
            post.LikeCount = Math.Max(0, post.LikeCount + (liked ? 1 : -1)); post.Liked = liked; like.Text = LikeText(post);
        });
        body.Add(like);
        body.Add(Button($"Comments · {post.CommentCount}", async () => { EnsureAccount(); await Navigation.PushAsync(new FeedCommentsPage(post.Id, moderator)); }));
        body.Add(Button("Post options", async () => {
            EnsureAccount();
            var owner = post.AuthorId == account;
            var choices = new List<string> { "Report post" }; if (owner || moderator) choices.Add("Delete post"); if (moderator) choices.Add("Hide post");
            var choice = await DisplayActionSheet("Post options", "Cancel", null, choices.ToArray());
            if (choice == "Report post") {
                var reason = await DisplayPromptAsync("Report post", "Tell the moderators what is wrong with this post.", maxLength: 500);
                if (!string.IsNullOrWhiteSpace(reason)) { EnsureAccount(); await FoodFeedService.Call<object>(new { action = "report", postId = post.Id, reason }); await DisplayAlert("Report received", "A moderator can review your report.", "OK"); }
            }
            else if (choice is "Delete post" or "Hide post") {
                if (!await DisplayAlert(choice + "?", choice == "Delete post" ? "This removes the shared post. Your original private recipe is kept." : "This hides the post from the community.", "Continue", "Cancel")) return;
                EnsureAccount(); await FoodFeedService.Call<object>(new { action = choice == "Delete post" ? "delete" : "moderate", postId = post.Id, hidden = true }); await Load(true);
            }
        }));
        return ShoppingUi.Card(body);
    }
    private static string LikeText(FoodPost post) => $"{(post.Liked ? "♥ Liked" : "♡ Like")} · {post.LikeCount}";
    private void EnsureAccount() { if (account != HouseholdService.UserId) throw new HouseholdException("UNAUTHENTICATED", "Your account changed. Refresh the feed."); }
}

public class FeedRecipePage : ContentPage
{
    public FeedRecipePage(FoodPost post)
    {
        Title = post.Recipe.Title; ShoppingUi.Theme(this);
        var body = new VerticalStackLayout { Spacing = 12 };
        var reader = new RecipeReadView(post.Recipe.ToRecipe(post.Photo), privateDetails: false);
        body.Add(reader);
        var saved = false;
        body.Add(FoodFeedPage.Button("Save to my recipes", async () => {
            if (saved) { await DisplayAlert("Already saved", "You saved a copy during this visit.", "OK"); return; }
            var copy=post.Recipe.ToRecipe(post.Photo);copy.FirebaseUID=HouseholdService.UserId;await RecipeService.AddRecipe(copy); saved = true;
            await DisplayAlert("Recipe saved", "A private copy is now in your recipe book. Editing it won't change the shared post.", "OK");
        }));
        body.Add(FoodFeedPage.Button("Add ingredients to my shopping list", async () => {
            await Navigation.PushAsync(new ShoppingListMakerPage(new[] { new RecipeShoppingChoice(post.Recipe.ToRecipe("")) { Selected=true,Servings=reader.CurrentServings.ToString() } }));
        }));
        body.Remove(reader);
        var layout = new Grid { RowDefinitions = new() { new(GridLength.Star), new(GridLength.Auto) } };
        layout.Add(reader, 0, 0); layout.Add(body, 0, 1); Content = layout;
        Appearing += (_, _) => reader.StartCooking();
        Disappearing += (_, _) => reader.StopCooking();
    }
}
