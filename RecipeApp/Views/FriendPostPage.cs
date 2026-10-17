using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public class FriendPostPage:SocialPage
{
    private readonly string postId;private FoodPost post;private bool saved;private string cursor,parentId,pendingId=Guid.NewGuid().ToString("N"),pendingText;
    private readonly Editor message=new(){Placeholder="Add a kind comment…",MaxLength=1000,HeightRequest=90};
    public FriendPostPage(string id):base("Conversation"){postId=id;ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Run(Load)));}
    protected override async void OnAppearing(){base.OnAppearing();await Run(Load);}
    private async Task Load()
    {
        await SocialService.Require();var result=await SocialService.Call<SocialPostResult>(new{action="getPost",postId});Check();post=result.Post;SocialPolicy.ValidatePost(post);Body.Clear();
        Body.Add(ShoppingUi.Button(post.AuthorName,()=>AppNavigation.Open(new FriendProfilePage(post.AuthorId)),true));Body.Add(ShoppingUi.Text(SocialPolicy.AudienceLabel(post.Audience),13,true));Body.Add(ShoppingUi.Text(post.Caption??"",19));
        var photo=RecipeCardView.Photo(post.Photo,220);if(photo!=null)Body.Add(photo);
        if(post.Recipe!=null)Body.Add(ShoppingUi.Button("View & cook · "+post.Recipe.Title,()=>AppNavigation.Open(new RecipePreviewPage(RecipeDraft.FromRecipe(post.Recipe.ToRecipe(post.Photo))))));
        if(post.Recipe!=null)Body.Add(ShoppingUi.Button(saved?"Recipe saved":"Save a private recipe copy",()=>Run(async()=>{if(saved)return;Check();var copy=post.PrivateCopy(Account);await RecipeService.AddRecipe(copy);Check();saved=true;_=AccountSession.SyncSafely();await DisplayAlert("Saved","A copy is in your private recipe book. The shared post is unchanged.","OK");await Load();}),true));
        if(post.Recipe!=null)Body.Add(ShoppingUi.Button("Shop this recipe",()=>Run(async()=>{
            var value=await DisplayPromptAsync("Shopping portions","How many people are you shopping for?",initialValue:post.Recipe.Servings.ToString(),maxLength:4,keyboard:Keyboard.Numeric);
            if(value==null)return;if(!int.TryParse(value,out var portions)||portions<1||portions>1000)throw new InvalidOperationException("Choose 1–1,000 portions.");Check();
            await AppNavigation.Open(new ShoppingListMakerPage(new[]{new RecipeShoppingChoice(post.Recipe.ToRecipe("")){Selected=true,Servings=portions.ToString()}}));
        }),true));
        Body.Add(ShoppingUi.Button((post.Liked?"♥ Unlike":"♡ Like")+$" · {post.LikeCount}",()=>Run(async()=>{await SocialService.Call<object>(new{action="like",postId,liked=!post.Liked});await Load();}),true));
        if(post.AuthorId==Account){Body.Add(ShoppingUi.Button("Change post audience…",()=>Run(async()=>{
            var label=await DisplayActionSheet("Who can see this post?","Cancel",null,SocialPolicy.AudienceLabels);var index=Array.IndexOf(SocialPolicy.AudienceLabels,label);if(index<0)return;
            if(!await DisplayAlert("Change privacy?",$"New audience: {label}. Previously delivered copies or screenshots cannot be recalled.","Change audience","Cancel"))return;
            Check();await SocialService.Call<object>(new{action="setAudience",postId,audience=SocialPolicy.Audiences[index]});await Load();
        }),true));Body.Add(ShoppingUi.Button("Delete post…",()=>Run(async()=>{if(!await DisplayAlert("Delete post?","This removes the shared post, not your private original recipe.","Delete","Keep"))return;await SocialService.Call<object>(new{action="deletePost",postId});await AppNavigation.Back();}),true));}
        else Body.Add(ShoppingUi.Button("Report post",()=>Run(async()=>{var reason=await DisplayPromptAsync("Report post","What should a moderator review?",maxLength:500);if(string.IsNullOrWhiteSpace(reason))return;Check();await SocialService.Call<object>(new{action="reportPost",postId,reason});await DisplayAlert("Report received","A moderator can review the post.","OK");}),true));
        Body.Add(ShoppingUi.Button("Share link…",()=>ContentSharing.SharePost(post),true));
        Body.Add(ShoppingUi.Text("Comments & replies",23,true));Body.Add(ShoppingUi.Text(parentId==null?"Write a comment":"Replying to a comment",13));Body.Add(message);
        Body.Add(ShoppingUi.Button("Send",()=>Run(async()=>{
            if(string.IsNullOrWhiteSpace(message.Text))throw new InvalidOperationException("Write a comment first.");var text=message.Text.Trim();if(text!=pendingText){pendingId=Guid.NewGuid().ToString("N");pendingText=text;}
            await SocialService.Call<object>(new{action="comment",postId,id=pendingId,text,parentCommentId=parentId});Check();message.Text="";pendingText=null;parentId=null;await Load();
        })));
        if(parentId!=null)Body.Add(ShoppingUi.Button("Cancel reply",()=>Run(async()=>{parentId=null;pendingText=null;pendingId=Guid.NewGuid().ToString("N");await Load();}),true));cursor=null;await Comments();
    }
    private async Task Comments()
    {
        var page=await SocialService.Call<SocialComments>(new{action="comments",postId,cursor});Check();
        foreach(var comment in page.Comments){var row=new VerticalStackLayout{Spacing=6};row.Add(ShoppingUi.Button(comment.AuthorName,()=>AppNavigation.Open(new FriendProfilePage(comment.AuthorId)),true));row.Add(ShoppingUi.Text((comment.ParentCommentId==null?"":"↳ ")+comment.Text));
            row.Add(ShoppingUi.Button("Reply",()=>Run(async()=>{parentId=comment.Id;pendingText=null;pendingId=Guid.NewGuid().ToString("N");await Load();}),true));
            if(comment.AuthorId==Account)row.Add(ShoppingUi.Button("Delete comment…",()=>Run(async()=>{if(!await DisplayAlert("Delete comment?","Replies may remain, without the removed text.","Delete","Keep"))return;await SocialService.Call<object>(new{action="deleteComment",postId,commentId=comment.Id});await Load();}),true));Body.Add(ShoppingUi.Card(row));}
        cursor=page.Cursor;if(cursor!=null)Body.Add(ShoppingUi.Button("Older comments",()=>Run(Comments),true));
    }
}
