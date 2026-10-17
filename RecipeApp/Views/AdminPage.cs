using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

public class AdminPage : ContentPage
{
    private readonly string account = AccountSession.UserId;
    private readonly VerticalStackLayout results = new() { Spacing = 14 };
    private readonly Label status = ShoppingUi.Text("");
    private readonly Entry email = new() { Placeholder = "Find account by email", Keyboard = Keyboard.Email, MaxLength = 320 };
    private FeedCursor cursor;
    private string section = "posts";
    private bool busy;
    private Button more;
    public AdminPage()
    {
        Title = "Administrator"; ShoppingUi.Theme(this);
        var body = new VerticalStackLayout { Padding = 20, Spacing = 14 };
        body.Add(ShoppingUi.Text("Mealsy administration",26,true));
        body.Add(ShoppingUi.Text("Automatic safety checks remain active. Changes here are recorded in the administrator activity log.",13));
        body.Add(Action("Community posts & reports", () => Load("posts",true)));
        body.Add(email); body.Add(Action("Find account", FindUser));
        body.Add(Action("Administrator activity", () => Load("audit",true)));
        body.Add(Action("Verify businesses & creators",()=>Navigation.PushAsync(new BusinessVerificationPage())));
        body.Add(status); body.Add(results);
        more = Action("Load more", () => Load(section,false)); more.IsVisible = false; body.Add(more);
        Content = new ScrollView { Content = body };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); results.Clear(); more.IsVisible = false;
        try { Check(); var role = await AdminService.Status(); Check(); if (!role.Admin) throw new HouseholdException("PERMISSION_DENIED","Administrator access is required."); await Load("posts",true); }
        catch (Exception e) { status.Text = e is HouseholdException ? e.Message : "Administration unavailable. Reopen this screen to retry."; }
    }
    private void Check() { if(account == null || account != AccountSession.UserId) throw new HouseholdException("UNAUTHENTICATED","Your account changed. Close administration and reopen Account."); }
    private Button Action(string text, Func<Task> action)
    {
        var button = FoodFeedPage.Button(text, async () => { Check(); await action(); Check(); });
        return button;
    }
    private async Task<string> Reason(string title)
    {
        Check(); var value = await DisplayPromptAsync(title,"Add a short reason for the activity log.","Continue","Cancel",maxLength:500);
        Check(); if (value == null) return null;
        if (string.IsNullOrWhiteSpace(value)) throw new HouseholdException("INVALID_ARGUMENT","A reason is required.");
        return value.Trim();
    }
    private async Task Load(string which, bool reset)
    {
        Check(); if(busy)return; busy=true;
        try {
            var next = reset ? null : cursor;
            if(reset){results.Clear();cursor=null;section=which;} more.IsVisible=false;
            status.Text="Loading…";
            if(which=="posts") {
                var page=await AdminService.Call<AdminPosts>(new{action="posts",cursor=next});Check();
                foreach(var post in page.Posts)results.Add(Post(post));cursor=page.Cursor;
                status.Text="Newest posts first · Reports appear beneath each post";
            } else {
                var page=await AdminService.Call<AdminAudit>(new{action="audit",cursor=next});Check();
                foreach(var entry in page.Entries)results.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=6,Children={
                    ShoppingUi.Text(entry.Action,17,true),ShoppingUi.Text(entry.Reason),
                    ShoppingUi.Text($"Target: {entry.Target}\nAdministrator: {entry.Actor}",12),
                    ShoppingUi.Text(DateTimeOffset.FromUnixTimeMilliseconds(entry.CreatedAt).ToLocalTime().ToString("g"),12)}}));
                cursor=page.Cursor;status.Text="Administrator activity · Newest first";
            }
            more.IsVisible=cursor!=null;if(results.Count==0)status.Text="Nothing here yet.";
        } finally {busy=false;}
    }
    private View Post(AdminPost post)
    {
        var body=new VerticalStackLayout{Spacing=10};
        body.Add(ShoppingUi.Text(post.AuthorName??"Removed post",18,true));
        body.Add(ShoppingUi.Text(post.Deleted?"Removed":post.Hidden?"Hidden from community":"Visible to community",13,true));
        body.Add(ShoppingUi.Text($"Post: {post.Id}\nAuthor: {post.AuthorId}",12));
        if(!post.Deleted) {
            if(!string.IsNullOrEmpty(post.Caption))body.Add(ShoppingUi.Text(post.Caption));
            if(!string.IsNullOrEmpty(post.Photo)){var image=RecipeCardView.Photo(post.Photo,180);if(image!=null)body.Add(image);}
            if(post.Recipe!=null)body.Add(Action("View recipe · "+post.Recipe.Title,()=>Navigation.PushAsync(new FeedRecipePage(post))));
            body.Add(Action(post.Hidden?"Restore post":"Hide post",async()=>{
                var reason=await Reason(post.Hidden?"Restore this post?":"Hide this post?");if(reason==null)return;
                await AdminService.Call<object>(new{action="setPostHidden",postId=post.Id,hidden=!post.Hidden,reason});await Load("posts",true);
            }));
            if(post.PromotionActive)body.Add(Action("Pause promotion",async()=>{
                var reason=await Reason("Pause this promotion?");if(reason==null)return;
                await AdminService.Call<object>(new{action="pausePromotion",postId=post.Id,reason});await Load("posts",true);
            }));
        }
        foreach(var report in post.Reports??new()) {
            body.Add(ShoppingUi.Text("Report: "+report.Reason,14));
            body.Add(Action("Mark report reviewed",async()=>{
                var reason=await Reason("Mark this report reviewed?");if(reason==null)return;
                await AdminService.Call<object>(new{action="resolveReport",postId=post.Id,reporterId=report.Id,reason});await Load("posts",true);
            }));
        }
        return ShoppingUi.Card(body);
    }
    private async Task FindUser()
    {
        if(busy)return;busy=true;
        try {
            var user=await AdminService.Call<AdminUser>(new{action="lookupUser",email=email.Text??""});Check();
            results.Clear();cursor=null;more.IsVisible=false;status.Text="Account found";
            var body=new VerticalStackLayout{Spacing=10};
            body.Add(ShoppingUi.Text(user.Username.Length>0?user.Username:user.Email,20,true));body.Add(ShoppingUi.Text(user.Email));
            body.Add(ShoppingUi.Text($"Account: {user.Uid}\nEmail {(user.EmailVerified?"confirmed":"unconfirmed")} · {(user.Admin?"Administrator":"Member")}{(user.Disabled?" · Disabled":"")}",13));
            string Expiry(long value)=>value>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()?DateTimeOffset.FromUnixTimeMilliseconds(value).ToLocalTime().ToString("g"):"None active";
            body.Add(ShoppingUi.Text("Family grant: "+Expiry(user.GrantExpiresAt)+"\nSubscription: "+Expiry(user.SubscriptionExpiresAt),13));
            body.Add(ShoppingUi.Text("Grants cover this account's household if they are its owner. Revoking a grant does not cancel a paid subscription.",13));
            body.Add(Action("Grant temporary Family access",async()=>{
                var input=await DisplayPromptAsync("Family access","Number of days (1–365)","Next","Cancel",initialValue:"30",maxLength:3,keyboard:Keyboard.Numeric);Check();if(input==null)return;
                if(!int.TryParse(input,out var days)||days<1||days>365)throw new HouseholdException("INVALID_ARGUMENT","Choose between 1 and 365 days.");
                var reason=await Reason("Grant access to "+user.Email+"?");if(reason==null)return;
                await AdminService.Call<object>(new{action="setFamilyGrant",uid=user.Uid,days,reason});await FindUser();
            }));
            body.Add(Action("Revoke temporary Family grant",async()=>{
                var reason=await Reason("Revoke grant for "+user.Email+"?");if(reason==null)return;
                await AdminService.Call<object>(new{action="setFamilyGrant",uid=user.Uid,days=0,reason});await FindUser();
            }));results.Add(ShoppingUi.Card(body));
        } finally {busy=false;}
    }
}
