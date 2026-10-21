using System.Collections.ObjectModel;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

public class FoodFeedPage : ContentPage
{
    private readonly ObservableCollection<FoodPost> posts = new();
    private readonly Label status = Text("Discover what’s cooking", 16);
    private readonly Button more;
    private readonly LoadingIndicator loading=new("Loading the food community…",false);
    private FeedCursor cursor;
    private bool busy, moderator;
    private string account;
    private int count;
    private string mode = "forYou", activeSearch = "";
    private readonly SearchBar search = new() { Placeholder = "Search meals, ingredients or cooks", MaxLength = 120 };
    private string promptedAccount;
    private bool reloadPending;
    private bool visible;
    private string Mode => mode;
    private Task SearchPosts() { activeSearch=search.Text?.Trim()??""; return Load(true); }
    public FoodFeedPage()
    {
        Title = "Food feed"; ShoppingUi.Theme(this);
        more = Button("Load more", () => Load(false));
        var layout = new VerticalStackLayout { Padding = 16, Spacing = 14, Children = {
            Text("The Mealsy table", 30, true), Text("Recipes, kitchen wins and inspiration from the community.", 15),
            Button("Share something delicious", async () => await Navigation.PushAsync(new FeedComposerPage())),
            new HorizontalStackLayout { Spacing=8, Children={
                Button("For you",()=>{mode="forYou";return Load(true);}),
                Button("Following",()=>{mode="following";return Load(true);}),
                Button("Latest",()=>{mode="latest";return Load(true);})}}, search,
            Button("Friends feed",()=>AppNavigation.Open(new FriendsPage("feed"))),
            Button("Friends & requests",()=>AppNavigation.Open(new FriendsPage())),
            Button("Search all posts", SearchPosts),
            Button("Businesses & recipe creators",()=>Navigation.PushAsync(new BusinessDirectoryPage())),
            status, loading, new RecipeApp.Controls.AdBanner() } };
        search.SearchButtonPressed += async (_, _) => await SearchPosts();
        search.TextChanged += async (_, e) => { if (string.IsNullOrEmpty(e.NewTextValue) && activeSearch.Length>0) await SearchPosts(); };
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
        ToolbarItems.Add(new ToolbarItem("Activity",null,async()=>await Navigation.PushAsync(new ActivityPage())));
        ToolbarItems.Add(new ToolbarItem("Food interests & following", null, async () => {
            try { await Navigation.PushAsync(new FeedPreferencesPage(await FoodFeedService.Preferences())); }
            catch (Exception) { await DisplayAlert("Food interests", "Sign in and connect to update your interests.", AppLocalization.Text("OK")); }
        }));
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        visible=true;loading.Message="Opening your food community…";loading.IsBusy=true;
        var uid = AccountSession.UserId;
        if (uid != null && promptedAccount != uid)
        {
            try {
                var preferences = await FoodFeedService.Preferences(); AccountSession.Check(uid); promptedAccount = uid;
                if (!preferences.SetupComplete) { loading.IsBusy=false;await Navigation.PushAsync(new FeedPreferencesPage(preferences)); return; }
            } catch (Exception) { status.Text = "Interests unavailable. You can try again from the menu."; }
        }
        await Load(true);
    }
    internal static Label Text(string value, int size = 15, bool bold = false) => ShoppingUi.Text(value, size, bold);
    internal static Button Button(string title, Func<Task> action)
    {
        var button = new Button { Text = title, HeightRequest = 46, CornerRadius = 12, TextColor = Colors.White };
        button.SetDynamicResource(VisualElement.BackgroundColorProperty, "ThemeAccent");
        button.Clicked += async (_, _) => {
            button.IsEnabled = false;
            try { await ActionProgress.Run(button,action); }
            catch (HouseholdException e) { await Shell.Current.DisplayAlert("Food feed", e.Message, AppLocalization.Text("OK")); }
            catch (Exception error) { var reference=AppDiagnostics.Report(error,"ui.action");await Shell.Current.DisplayAlert("Food feed unavailable", "Check your connection and try again. Your local recipes are still available.\nReference: "+reference, AppLocalization.Text("OK")); }
            finally { button.IsEnabled = true; }
        };
        return button;
    }
    private async Task Load(bool reset)
    {
        if (busy) { reloadPending |= reset; return; }
        var uid = HouseholdService.UserId;
        if (uid != account || reset) { posts.Clear(); cursor = null; count = 0; account = uid; }
        if (uid == null) { status.Text = "Sign in on the Account tab to join the Mealsy community."; more.IsVisible = false; loading.IsBusy=false;return; }
        busy = true; loading.Message=reset?"Refreshing the food community…":"Loading more posts…";loading.IsBusy=true;status.Text = "Setting the table…";
        try
        {
            var page = await FoodFeedService.Load(reset ? null : cursor, Mode, activeSearch);
            if (HouseholdService.UserId != uid) { posts.Clear(); return; }
            moderator = page.Moderator; cursor = page.Cursor;
            foreach (var post in page.Posts) { posts.Add(post); count++; }
            more.IsVisible = cursor != null;
            status.Text = count == 0 ? cursor != null ? "No matches on this page. Load more to search older posts." : Mode == "following" ? "No posts found. Follow cooks from Latest, or clear your search." : "No posts found. Try another search or share the first meal."
                : activeSearch.Length>0 ? "Search results from the whole community · Load more for older matches" : Mode == "forYou" ? "Chosen for your interests, likes and follows · Refresh for updates" : "Newest first · Pull down to refresh";
        }
        catch (HouseholdException e) { status.Text = e.Message; more.IsVisible = true; }
        catch (Exception error) { var reference=AppDiagnostics.Report(error,"feed.load");status.Text = "Feed unavailable. Check your connection; pull down to retry.\nReference: "+reference; more.IsVisible = true; }
        finally { busy = false; loading.IsBusy=false;if (reloadPending) { reloadPending = false; await Load(true); } }
    }
    private View Card(FoodPost post)
    {
        if(post.PrivacyManaged)return SocialPage.Post(post);
        var body = new VerticalStackLayout { Spacing = 10 };
        if(post.Promoted)body.Add(Text("Promoted",13,true));
        body.Add(Button(post.AuthorName+" · View profile",()=>AppNavigation.Open(new FriendProfilePage(post.AuthorId))));
        if(!string.IsNullOrEmpty(post.BusinessId)) {
            body.Add(Text(post.BusinessVerified?"Verified business / creator":"Business / creator",13,true));
            body.Add(Button("View creator profile",()=>Navigation.PushAsync(new BusinessPublicPage(post.BusinessId))));
            _=BusinessService.Track(post.BusinessId,"postViews",post.Id);
        }
        if (post.AuthorId != account && string.IsNullOrEmpty(post.BusinessId)) body.Add(Button(post.Following ? "✓ Following · Unfollow" : "+ Follow this cook", async () => {
            EnsureAccount(); await FoodFeedService.Call<object>(new { action = "follow", postId = post.Id, following = !post.Following });
            EnsureAccount(); await Load(true);
        }));
        if (post.Topics?.Count > 0) body.Add(Text(string.Join(" · ", post.Topics), 12));
        body.Add(Text(DateTimeOffset.FromUnixTimeMilliseconds(post.CreatedAt).ToLocalTime().ToString("g"), 12));
        if (!string.IsNullOrEmpty(post.Photo))
        {
            var photo = RecipeCardView.Photo(post.Photo, 220);
            if (photo != null) body.Add(photo);
        }
        if (!string.IsNullOrEmpty(post.Caption)) body.Add(Text(post.Caption));
        if (post.Recipe != null) body.Add(Button("View recipe · " + post.Recipe.Title, async () => {if(!string.IsNullOrEmpty(post.BusinessId))_=BusinessService.Track(post.BusinessId,"recipeOpens",post.Id);await Navigation.PushAsync(new FeedRecipePage(post));}));
        Button like = null;
        like = Button(LikeText(post), async () => {
            EnsureAccount(); var liked = !post.Liked;
            await FoodFeedService.Call<object>(new { action = "like", postId = post.Id, liked });
            post.LikeCount = Math.Max(0, post.LikeCount + (liked ? 1 : -1)); post.Liked = liked; like.Text = LikeText(post);
        });
        body.Add(like);
        body.Add(Button($"Comments · {post.CommentCount}", async () => { EnsureAccount(); await Navigation.PushAsync(new FeedCommentsPage(post.Id, moderator)); }));
        body.Add(Button("Share post…",()=>ContentSharing.SharePost(post)));
        body.Add(Button("Post options", async () => {
            EnsureAccount();
            var owner = post.AuthorId == account;
            var choices = new List<string> { "Report post" }; if (owner || moderator) choices.Add("Delete post"); if (moderator) choices.Add("Hide post");
            if(owner&&post.Recipe!=null)choices.Add("Promotion information");
            var choice = await DisplayActionSheet("Post options", "Cancel", null, choices.ToArray());
            if(choice=="Promotion information"){await DisplayAlert("Promoted recipes","Promotion infrastructure is being prepared. Paid checkout is not available yet. Active promotions will always be labelled and must pass the same safety checks.",AppLocalization.Text("OK"));return;}
            if (choice == "Report post") {
                var reason = await DisplayPromptAsync("Report post", "Tell the moderators what is wrong with this post.", maxLength: 500);
                if (!string.IsNullOrWhiteSpace(reason)) { EnsureAccount(); await FoodFeedService.Call<object>(new { action = "report", postId = post.Id, reason }); await DisplayAlert("Report received", "A moderator can review your report.", AppLocalization.Text("OK")); }
            }
            else if (choice is "Delete post" or "Hide post") {
                if (!await DisplayAlert(choice + "?", choice == "Delete post" ? "This removes the shared post. Your original private recipe is kept." : "This hides the post from the community.", AppLocalization.Text("Continue"), AppLocalization.Text("Cancel"))) return;
                EnsureAccount(); await FoodFeedService.Call<object>(new { action = choice == "Delete post" ? "delete" : "moderate", postId = post.Id, hidden = true }); await Load(true);
            }
        }));
        var card=ShoppingUi.Card(body);
        if(post.Promoted&&!string.IsNullOrEmpty(post.PromotionCampaignId))card.Loaded+=async(_,_)=>await MeasureImpression(card,post);
        return card;
    }
    protected override void OnDisappearing(){visible=false;base.OnDisappearing();}
    private bool HalfVisible(View card){
#if ANDROID
        if(!visible||!IsVisible||Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.HasWindowFocus!=true||card.Handler?.PlatformView is not Android.Views.View native||native.Width<=0||native.Height<=0)return false;
        using var rect=new Android.Graphics.Rect();return native.GetGlobalVisibleRect(rect)&&(long)rect.Width()*rect.Height()*2>=(long)native.Width*native.Height;
#else
        return false;
#endif
    }
    private async Task MeasureImpression(View card,FoodPost post){try{
        var owner=account;for(var i=0;i<120&&visible&&!HalfVisible(card);i++)await Task.Delay(500);
        if(!HalfVisible(card)||owner!=HouseholdService.UserId)return;
        var prepared=await HouseholdService.CallEndpointAsync<ImpressionTicket>(CampaignWalletPage.Endpoint,new{action="prepareImpression",postId=post.Id});
        for(var i=0;i<11;i++){if(!HalfVisible(card)||owner!=HouseholdService.UserId)return;await Task.Delay(100);}
        if(HalfVisible(card)&&owner==HouseholdService.UserId)await HouseholdService.CallEndpointAsync<object>(CampaignWalletPage.Endpoint,new{action="impression",postId=post.Id,ticket=prepared.Ticket});
    }catch{/* A failed measurement never charges or blocks the feed. */}}
    private record ImpressionTicket(string Ticket);
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
            if (saved) { await DisplayAlert("Already saved", "You saved a copy during this visit.", AppLocalization.Text("OK")); return; }
            var owner=HouseholdService.UserId;var copy=post.PrivateCopy(owner);
            if(!string.IsNullOrEmpty(post.BusinessId)){
                var existing=await RecipeService.GetAllRecipes();AccountSession.Check(owner);
                if(existing.Any(r=>r.FirebaseUID==owner&&r.CloudKey==copy.CloudKey)){saved=true;await DisplayAlert("Already saved","This business recipe is already in your recipe book. Your edited copy has been kept.",AppLocalization.Text("OK"));return;}
            }
            AccountSession.Check(owner);await RecipeService.AddRecipe(copy); saved = true;
            await DisplayAlert("Recipe saved", "A private copy is now in your recipe book. Editing it won't change the shared post.", AppLocalization.Text("OK"));
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
