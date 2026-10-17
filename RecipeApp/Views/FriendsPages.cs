using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public abstract class SocialPage:ContentPage
{
    protected readonly string Account=AccountSession.UserId;
    protected readonly VerticalStackLayout Body=new(){Padding=16,Spacing=14};
    private bool busy;
    private readonly LoadingIndicator progress=new("Loading…",false);
    protected SocialPage(string title){Title=title;ShoppingUi.Theme(this);var grid=new Grid{RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Star)}};grid.Add(progress,0,0);grid.Add(new ScrollView{Content=Body},0,1);Content=grid;}
    protected void Check()=>AccountSession.Check(Account);
    protected async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;progress.IsBusy=true;
        try{if(Account==null)throw new InvalidOperationException("Sign in to use friends and profiles.");Check();await action();Check();}
        catch(Exception e){if(Account!=AccountSession.UserId || e is HouseholdException { Code: "UNAUTHENTICATED" or "PERMISSION_DENIED" or "NOT_FOUND" })Body.Clear();Body.Add(ShoppingUi.Text(e is InvalidOperationException or HouseholdException?e.Message:"This couldn't finish. Check your connection and retry.",14));}
        finally{progress.IsBusy=false;busy=false;}
    }
    protected View Person(FriendPerson person)=>ShoppingUi.Card(new VerticalStackLayout{Spacing=7,Children={ShoppingUi.Text(person.Username,20,true),ShoppingUi.Text(person.Bio??"",14),ShoppingUi.Button("View profile",()=>AppNavigation.Open(new FriendProfilePage(person.UserId)))}});
    internal static View Post(FoodPost post)
    {
        SocialPolicy.ValidatePost(post);
        var body=new VerticalStackLayout{Spacing=8};body.Add(ShoppingUi.Button(post.AuthorName,()=>AppNavigation.Open(new FriendProfilePage(post.AuthorId)),true));
        body.Add(ShoppingUi.Text(SocialPolicy.AudienceLabel(post.Audience),12,true));if(!string.IsNullOrWhiteSpace(post.Caption))body.Add(ShoppingUi.Text(post.Caption));
        var photo=RecipeCardView.Photo(post.Photo);if(photo!=null)body.Add(photo);if(post.Recipe!=null)body.Add(ShoppingUi.Text(post.Recipe.Title,19,true));
        body.Add(ShoppingUi.Text($"{post.LikeCount} likes · {post.CommentCount} comments",12));body.Add(ShoppingUi.Button("Open conversation",()=>AppNavigation.Open(new FriendPostPage(post.Id))));body.Add(ShoppingUi.Button("Share link…",()=>ContentSharing.SharePost(post),true));return ShoppingUi.Card(body);
    }
}
public class FriendsPage:SocialPage
{
    private string section,cursor;private readonly SearchBar query=new(){Placeholder="Search public names",MaxLength=60};
    public FriendsPage(string initial="friends"):base("Friends"){section=initial;ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Run(()=>Load(true))));}
    protected override async void OnAppearing(){base.OnAppearing();await Run(()=>Load(true));}
    private async Task Load(bool reset)
    {
        Check();if(reset){cursor=null;Body.Clear();Body.Add(ShoppingUi.Text("Good food, good company",27,true));
            var tabs=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},RowDefinitions=new(){new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8,RowSpacing=8};
            var options=new[]{("Friends","friends"),("Requests","incoming"),("Sent requests","outgoing"),("Friends feed","feed")};for(var i=0;i<options.Length;i++){var value=options[i];tabs.Add(ShoppingUi.Button(value.Item1,()=>Run(async()=>{section=value.Item2;await Load(true);}),true),i%2,i/2);}Body.Add(tabs);Body.Add(ShoppingUi.Button("Blocked accounts",()=>Run(async()=>{section="blocked";await Load(true);}),true));
            Body.Add(query);Body.Add(ShoppingUi.Button("Find people",()=>Run(async()=>{if((query.Text??"").Trim().Length<2)throw new InvalidOperationException("Enter at least two characters.");section="search";await Load(true);}),true));Body.Add(ShoppingUi.Button("My public profile",()=>AppNavigation.Open(new FriendProfilePage(Account)),true));
        }
        await SocialService.Require();Check();
        if(section=="feed"){
            var page=await SocialService.Call<SocialPosts>(new{action="friendsFeed",cursor});Check();foreach(var post in page.Posts)Body.Add(Post(post));cursor=page.Cursor;if(page.Posts.Count==0)Body.Add(ShoppingUi.Text("No visible friend posts in this page. Accepted friends' posts appear here when their audience permits it."));
        }else{
            var page=await SocialService.Call<FriendPeople>(new{action=section=="search"?"searchPeople":"friendList",kind=section,query=query.Text?.Trim()??"",cursor});Check();foreach(var person in page.People)Body.Add(Person(person));cursor=page.Cursor;if(page.People.Count==0)Body.Add(ShoppingUi.Text(section=="friends"?"Your table has room. Search for a cook and send a friend request.":"No people in this page."));
        }
        if(cursor!=null)Body.Add(ShoppingUi.Button("Load more",()=>Run(()=>Load(false))));
        Body.Add(ShoppingUi.Button("Refresh",()=>Run(()=>Load(true)),true));
    }
}
public class FriendProfilePage:SocialPage
{
    private readonly string userId;private string cursor;
    public FriendProfilePage(string id):base("Cook profile"){userId=id;ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Run(Load)));}
    protected override async void OnAppearing(){base.OnAppearing();await Run(Load);}
    private async Task Load()
    {
        await SocialService.Require();var profile=await SocialService.Call<SocialProfile>(new{action="viewProfile",userId});Check();if(profile.UserId!=userId)throw new InvalidOperationException("The requested profile could not be verified.");Body.Clear();
        var photo=RecipeCardView.Photo(profile.Avatar,160);if(photo!=null)Body.Add(photo);Body.Add(ShoppingUi.Text(profile.Username,29,true));Body.Add(ShoppingUi.Text(profile.Bio??""));Body.Add(ShoppingUi.Text($"{profile.FriendCount} friends · Only posts you can access are shown",13));
        if(userId==Account)Body.Add(ShoppingUi.Button("Edit my profile",()=>AppNavigation.Open(new EditFriendProfilePage(profile))));
        else{
            if(profile.Relationship=="incoming"){Body.Add(RelationshipButton("Accept request","acceptFriend"));Body.Add(RelationshipButton("Decline request","declineFriend"));}
            else if(profile.Relationship=="outgoing")Body.Add(RelationshipButton("Cancel friend request","cancelFriend"));
            else if(profile.Relationship=="friends")Body.Add(RelationshipButton("Remove friend…","removeFriend",true));
            else if(profile.Relationship=="blocked")Body.Add(RelationshipButton("Unblock this account","unblock"));
            else Body.Add(RelationshipButton("Send friend request","requestFriend"));
            if(profile.Relationship!="blocked")Body.Add(RelationshipButton("Block this account…","block",true));
            Body.Add(ShoppingUi.Button("Report profile",()=>Run(async()=>{var reason=await DisplayPromptAsync("Report profile","What should moderators review?",maxLength:500);if(string.IsNullOrWhiteSpace(reason))return;Check();await SocialService.Call<object>(new{action="reportProfile",userId,reason});await DisplayAlert("Report received","A moderator can review this profile.","OK");}),true));
        }
        Body.Add(ShoppingUi.Button("Refresh profile",()=>Run(Load),true));cursor=null;if(profile.Relationship!="blocked")await Posts();
    }
    private Button RelationshipButton(string text,string action,bool confirm=false)=>ShoppingUi.Button(text,()=>Run(async()=>{
        if(confirm&&!await DisplayAlert(text,"This changes who can interact with you and access friend-only posts. Nothing will be published.","Continue","Cancel"))return;
        Check();await SocialService.Call<object>(new{action,userId});await Load();
    }),true);
    private async Task Posts(){var page=await SocialService.Call<SocialPosts>(new{action="profilePosts",userId,cursor});Check();foreach(var post in page.Posts)Body.Add(Post(post));cursor=page.Cursor;if(cursor!=null)Body.Add(ShoppingUi.Button("Older posts",()=>Run(Posts),true));}
}
public class EditFriendProfilePage:SocialPage
{
    private readonly Editor bio;private readonly Switch discoverable;private string avatar;
    public EditFriendProfilePage(SocialProfile profile):base("Edit public profile")
    {
        avatar=profile.Avatar;bio=new Editor{Text=profile.Bio,MaxLength=300,HeightRequest=130};discoverable=new Switch{IsToggled=profile.Discoverable};
        Body.Add(ShoppingUi.Text("Tell cooks a little about you",26,true));Body.Add(ShoppingUi.Text("Your bio and photo are visible to signed-in users who can open your profile. Hiding from search does not make existing posts private or remove friends. Keep private details out; manage your username in Account."));Body.Add(bio);
        Body.Add(new HorizontalStackLayout{Children={discoverable,ShoppingUi.Text("Show my profile in people search",14)}});
        Body.Add(ShoppingUi.Button("Choose profile photo",()=>Run(async()=>{var file=await MediaPicker.Default.PickPhotoAsync();if(file==null)return;using var stream=await file.OpenReadAsync();avatar=await FeedPhoto.PrepareAsync(stream);Check();Body.Add(ShoppingUi.Text("Profile photo ready · location metadata removed",12));}),true));
        Body.Add(ShoppingUi.Button("Remove profile photo",()=>{avatar="";return Task.CompletedTask;},true));
        Body.Add(ShoppingUi.Button("Save profile",()=>Run(async()=>{await SocialService.Require();Check();await SocialService.Call<object>(new{action="updateProfile",bio=bio.Text??"",avatar=avatar??"",discoverable=discoverable.IsToggled});await AppNavigation.Back();})));
    }
}
