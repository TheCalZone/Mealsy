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
    private readonly bool premiumOnly;
    public AdminPage(bool premium=false)
    {
        premiumOnly=premium||!ProductFeatures.SocialEnabled;Title = premiumOnly?"Complimentary Premium":"Administrator"; ShoppingUi.Theme(this);
        var body = new VerticalStackLayout { Padding = 20, Spacing = 14 };
        body.Add(ShoppingUi.Text("Mealsy administration",26,true));
        body.Add(ShoppingUi.Text("Changes here are recorded in the administrator activity log.",13));
        if(ProductFeatures.SocialEnabled)body.Add(Action("Community posts & reports", () => Load("posts",true)));
        body.Add(ShoppingUi.Text("Complimentary Premium access",21,true));
        body.Add(ShoppingUi.Text("Find an existing account by email, then grant 30, 90 or 365 days without a purchase. A reason is required and every change is audited. This does not create an app-store subscription or grant administrator rights.",13));
        body.Add(email); body.Add(Action("Find account / manage Premium", FindUser));
        body.Add(Action("Administrator activity", () => Load("audit",true)));
        if(ProductFeatures.SocialEnabled)body.Add(Action("Verify businesses & creators",()=>Navigation.PushAsync(new BusinessVerificationPage())));
        body.Add(status); body.Add(results);
        more = Action("Load more", () => Load(section,false)); more.IsVisible = false; body.Add(more);
        Content = new ScrollView { Content = body };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); results.Clear(); more.IsVisible = false;
        try { Check(); var role = await AdminService.Status(); Check(); if (!role.Admin) throw new HouseholdException("PERMISSION_DENIED","Administrator access is required."); if(premiumOnly){status.Text="Enter the recipient’s registered email to manage complimentary Premium.";email.Focus();}else await Load("posts",true); }
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
        Check(); if(which=="posts"&&!ProductFeatures.SocialEnabled)return;if(busy)return; busy=true;
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
            body.Add(ShoppingUi.Text("Complimentary Premium: "+Expiry(user.GrantExpiresAt)+"\nSubscription: "+Expiry(user.SubscriptionExpiresAt),13));
            body.Add(ShoppingUi.Text("Complimentary access includes personal Premium tools. It covers the household if this account owns it. No payment is taken. Revoking a grant does not cancel a paid subscription.",13));
            foreach(var days in new[]{30,90,365})body.Add(Action($"Give {days} days of Premium free",()=>Grant(user,days)));
            body.Add(Action("Choose another duration…",async()=>{
                var input=await DisplayPromptAsync("Complimentary Premium","Number of days (1–365)","Next","Cancel",initialValue:"30",maxLength:3,keyboard:Keyboard.Numeric);Check();if(input==null)return;
                if(!int.TryParse(input,out var days)||days<1||days>365)throw new HouseholdException("INVALID_ARGUMENT","Choose between 1 and 365 days.");
                await Grant(user,days);
            }));
            body.Add(Action("Revoke complimentary Premium…",()=>Grant(user,0)));results.Add(ShoppingUi.Card(body));
        } finally {busy=false;}
    }
    private async Task Grant(AdminUser user,int days)
    {
        if(busy)return;busy=true;var changed=false;
        try{
            Check();var message=days==0?$"Remove complimentary Premium from {user.Email}? Paid subscriptions remain active until their own expiry.":$"Give {user.Email} complimentary Premium for {days} days from now? This replaces any previous complimentary expiry with {DateTimeOffset.Now.AddDays(days):d}. No charge or store subscription is created.";
            if(!await DisplayAlert("Confirm Premium change",message,days==0?"Revoke access":"Grant free Premium","Cancel"))return;
            var reason=await Reason("Reason for this Premium change");if(reason==null)return;Check();
            await AdminService.Call<object>(new{action="setFamilyGrant",uid=user.Uid,days,reason});Check();email.Text=user.Email;changed=true;
        }finally{busy=false;}
        if(changed){await FindUser();await DisplayAlert("Premium updated",days==0?"The complimentary grant was revoked. This does not cancel a paid subscription. Cached offline access may last up to one hour.":"Complimentary Premium is active. Ask the recipient to open Account → Premium or use Restore purchases to refresh access. No payment was taken.","OK");}
    }

}
