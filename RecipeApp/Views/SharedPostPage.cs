using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class SharedLinkSignInPage:ContentPage
{
    public SharedLinkSignInPage(){Title="Open shared Mealsy content";ShoppingUi.Theme(this);Content=new Grid{RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Star)}};var grid=(Grid)Content;grid.Add(ShoppingUi.Text("Sign in to open this share. Your link is saved while you sign in or create an account.",15),0,0);grid.Add(new AuthFormView(false),0,1);}
}
public class SharedPostPage:SocialPage
{
    private readonly ContentTarget target;
    public SharedPostPage(ContentTarget target):base("Shared post"){this.target=target;ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Run(Load)));}
    protected override async void OnAppearing(){base.OnAppearing();await Run(Load);}
    private async Task Load()
    {
        Body.Clear();FoodPost post;
        if(target.Kind=="social"){await SocialService.Require();post=(await SocialService.Call<SocialPostResult>(new{action="getPost",postId=target.Id})).Post;}
        else post=(await FoodFeedService.Call<SocialPostResult>(new{action="getPost",postId=target.Id})).Post;
        Check();SocialPolicy.ValidatePost(post);
        if(post.Id!=target.Id || (target.Kind=="post"&&(post.PrivacyManaged||post.Audience!="public")))throw new InvalidOperationException("This post could not be verified. Ask the sender for a current link.");
        Body.Add(ShoppingUi.Text(post.AuthorName,25,true));Body.Add(ShoppingUi.Text(SocialPolicy.AudienceLabel(post.Audience),13));Body.Add(ShoppingUi.Text(post.Caption??"",18));
        var photo=RecipeCardView.Photo(post.Photo,220);if(photo!=null)Body.Add(photo);
        if(post.Recipe!=null)Body.Add(ShoppingUi.Button("View recipe & save a copy",()=>AppNavigation.Open(post.PrivacyManaged?new FriendPostPage(post.Id):new FeedRecipePage(post))));
        Body.Add(ShoppingUi.Button("Open conversation",()=>AppNavigation.Open(post.PrivacyManaged?new FriendPostPage(post.Id):new FeedCommentsPage(post.Id,false))));
        Body.Add(ShoppingUi.Button("Share link…",()=>ContentSharing.SharePost(post),true));
        Body.Add(ShoppingUi.Text("Sharing a link does not grant access. Only people allowed by the author's current privacy settings can open it.",13));
    }
}
