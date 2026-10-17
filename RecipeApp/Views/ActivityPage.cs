using RecipeApp.Services;
namespace RecipeApp.Views;
public record ActivityItem(string Id,string Type,string Message,string PostId,long CreatedAt,bool Read,bool PrivacyManaged=false,string TargetUserId=null);
public record ActivityInbox(List<ActivityItem> Items);
public class ActivityPage:ContentPage
{
    private readonly string account=HouseholdService.UserId;
    private readonly VerticalStackLayout body=new(){Padding=20,Spacing=12};
    private const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/notifications";
    public ActivityPage(){Title="Activity";ShoppingUi.Theme(this);Content=new ScrollView{Content=body};ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Load()));
#if ANDROID
        ToolbarItems.Add(new ToolbarItem("Phone alerts",null,async()=>{try{var choice=await DisplayActionSheet("Activity notifications","Cancel",null,"Enable phone alerts","Turn off phone alerts");if(choice=="Enable phone alerts")await RecipeApp.Platforms.Android.ActivityNotifications.Enable();else if(choice=="Turn off phone alerts")await RecipeApp.Platforms.Android.ActivityNotifications.Disable();}catch(Exception e){await DisplayAlert("Notifications",e is HouseholdException or InvalidOperationException?e.Message:"Couldn't register this phone. Try again shortly.","OK");}}));
#endif
    }
    protected override async void OnAppearing(){base.OnAppearing();await Load();}
    private async Task Load(){try{
        AccountSession.Check(account);
        var inbox=await HouseholdService.CallEndpointAsync<ActivityInbox>(Endpoint,new{action="list"});AccountSession.Check(account);body.Clear();
        body.Add(ShoppingUi.Text("Friends, comments & replies",24,true));
        body.Add(ShoppingUi.Text("Your latest 50 notifications. Refresh to check for new activity.",13));
        if(inbox.Items.Count==0)body.Add(ShoppingUi.Text("You're all caught up."));
        foreach(var item in inbox.Items){var card=new VerticalStackLayout{Spacing=6};card.Add(ShoppingUi.Text((item.Read?"":"● ")+item.Message,16,true));card.Add(ShoppingUi.Text(DateTimeOffset.FromUnixTimeMilliseconds(item.CreatedAt).ToLocalTime().ToString("g"),12));
            if(!string.IsNullOrWhiteSpace(item.TargetUserId) || !string.IsNullOrWhiteSpace(item.PostId))
                card.Add(ShoppingUi.Button(string.IsNullOrWhiteSpace(item.PostId)?"Open profile":"Open conversation",async()=>{
                    AccountSession.Check(account);await HouseholdService.CallEndpointAsync<object>(Endpoint,new{action="read",id=item.Id});AccountSession.Check(account);
                    ContentPage destination=string.IsNullOrWhiteSpace(item.PostId)?new FriendProfilePage(item.TargetUserId):item.PrivacyManaged?new FriendPostPage(item.PostId):new FeedCommentsPage(item.PostId,false);
                    await Navigation.PushAsync(destination);
                }));body.Add(ShoppingUi.Card(card));}
    }catch(Exception e){body.Clear();body.Add(ShoppingUi.Text(e is HouseholdException?e.Message:"Activity unavailable. Refresh to retry."));}}
}
