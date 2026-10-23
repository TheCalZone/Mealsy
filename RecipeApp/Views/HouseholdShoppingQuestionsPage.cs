using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class HouseholdShoppingQuestionsPage:ContentPage
{
    private readonly string account=AccountSession.UserId,householdId,itemId;
    private readonly VerticalStackLayout messages=new(){Spacing=10,Padding=16};
    private readonly Editor draft=new(){Placeholder="e.g. Is this tinned or fresh?",MaxLength=1000,AutoSize=EditorAutoSizeOption.TextChanges,MinimumHeightRequest=70,MaximumHeightRequest=130};
    private readonly Picker recipient=new(){Title="Who would you like to ask?"};
    private ShoppingQuestionThread thread;private string messageId=Guid.NewGuid().ToString("N");private bool busy;
    private readonly Label status=ShoppingUi.Text("Loading conversation…",13);
    public HouseholdShoppingQuestionsPage(string household,string item)
    {
        householdId=household;itemId=item;Title="Shopping question";ShoppingUi.Theme(this);
        draft.SetDynamicResource(InputView.TextColorProperty,"ThemeText");draft.SetDynamicResource(InputView.PlaceholderColorProperty,"ThemeMuted");
        recipient.SetDynamicResource(Picker.TextColorProperty,"ThemeText");
        var composer=new VerticalStackLayout{Padding=12,Spacing=6,Children={recipient,draft,ShoppingUi.Button("Send message",()=>Run(Send)),status}};
        var grid=new Grid{RowDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)}};
        grid.Add(new ScrollView{Content=messages},0,0);grid.Add(composer,0,1);Content=grid;
        ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Run(Load)));
        ToolbarItems.Add(new ToolbarItem("Clear conversation…",null,async()=>await Run(async()=>{
            if(thread?.CanClear!=true)throw new InvalidOperationException("Only an adult can clear a conversation.");
            if(!await DisplayAlert("Clear this conversation?","Messages for this item will be removed for the household. The shopping item stays.","Clear",AppLocalization.Text("Cancel")))return;
            AccountSession.Check(account);await HouseholdShoppingQuestions.Call<object>(new{action="clear",householdId,itemId});await AppNavigation.Back();
        })));
    }
    protected override async void OnAppearing(){base.OnAppearing();await Run(Load);}
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;draft.IsEnabled=false;
        try{AccountSession.Check(account);await action();}
        catch(Exception e){status.Text=e is HouseholdException or InvalidOperationException?e.Message:"Conversation unavailable. Your draft is kept; retry when connected.";}
        finally{busy=false;draft.IsEnabled=true;}
    }
    private async Task Load()
    {
        thread=await HouseholdShoppingQuestions.Call<ShoppingQuestionThread>(new{action="read",householdId,itemId});AccountSession.Check(account);
        messages.Clear();messages.Add(ShoppingUi.Text(thread.Title,24,true));
        messages.Add(ShoppingUi.Text("Visible to this household. Questions alert the person who added the item; replies alert the people in the conversation. Latest 40 messages.",12));
        var members=thread.Members.Where(m=>m.Id!=account).ToList();
        recipient.ItemsSource=members.Select(m=>m.Name).ToList();recipient.SelectedIndex=-1;
        recipient.IsVisible=thread.Messages.Count==0&&(thread.TargetId==null||thread.TargetId==account||!members.Any(m=>m.Id==thread.TargetId));
        if(thread.Messages.Count==0)messages.Add(ShoppingUi.Text("Need a little more detail? Ask before you buy."));
        foreach(var message in thread.Messages)
        {
            var author=ShoppingUi.Text(message.AuthorName,12,true);author.TextColor=Color.FromArgb(new ShoppingListItem{AddedBy=message.AuthorId}.ContributorColor);
            var badge=new Border{Content=author,BackgroundColor=Colors.White,Padding=new Thickness(6,2),StrokeThickness=0,HorizontalOptions=LayoutOptions.Start};
            messages.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=6,Children={badge,ShoppingUi.Text(message.Text),ShoppingUi.Text(DateTimeOffset.FromUnixTimeMilliseconds(message.CreatedAt).ToLocalTime().ToString("g"),11)}}));
        }
        status.Text="Messages stay with this item, even if it is removed from the shopping list.";
    }
    private async Task Send()
    {
        if(thread==null)throw new InvalidOperationException("Refresh the conversation first.");
        if(string.IsNullOrWhiteSpace(draft.Text))throw new InvalidOperationException("Enter a question or reply.");
        string recipientId=null;
        if(recipient.IsVisible){var members=thread.Members.Where(m=>m.Id!=account).ToList();if(recipient.SelectedIndex<0)throw new InvalidOperationException("Choose a household member to ask.");recipientId=members[recipient.SelectedIndex].Id;}
        status.Text="Sending…";await HouseholdShoppingQuestions.Call<object>(new{action="send",householdId,itemId,messageId,text=draft.Text.Trim(),recipientId});AccountSession.Check(account);
        draft.Text="";messageId=Guid.NewGuid().ToString("N");await Load();
    }
}

public class HouseholdShoppingInboxPage:ContentPage
{
    private readonly string account=AccountSession.UserId;private bool busy;
    private readonly VerticalStackLayout body=new(){Padding=16,Spacing=12};
    public HouseholdShoppingInboxPage()
    {
        Title="Shopping questions & replies";ShoppingUi.Theme(this);Content=new ScrollView{Content=body};
        ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Load()));
#if ANDROID
        ToolbarItems.Add(new ToolbarItem("Phone alerts",null,async()=>{
            try{AccountSession.Check(account);var choice=await DisplayActionSheet("Household phone alerts",AppLocalization.Text("Cancel"),null,"Enable phone alerts","Turn off phone alerts");AccountSession.Check(account);
                if(choice=="Enable phone alerts")await RecipeApp.Platforms.Android.ActivityNotifications.Enable();else if(choice=="Turn off phone alerts")await RecipeApp.Platforms.Android.ActivityNotifications.Disable();}
            catch(Exception e){await DisplayAlert("Phone alerts",e is HouseholdException or InvalidOperationException?e.Message:"Couldn't update phone alerts. Retry when connected.","OK");}
        }));
#endif
    }
    protected override async void OnAppearing(){base.OnAppearing();await Load();}
    private async Task Load()
    {
        if(busy)return;busy=true;body.Clear();body.Add(ShoppingUi.Text("Checking for questions…"));
        try{
            AccountSession.Check(account);var inbox=await HouseholdService.CallEndpointAsync<ShoppingQuestionInbox>(HouseholdShoppingQuestions.InboxEndpoint,new{action="list"});AccountSession.Check(account);body.Clear();
            body.Add(ShoppingUi.Text("A little clarity before the shop",24,true));body.Add(ShoppingUi.Text("Swipe a household shopping item and choose Ask. Questions and replies appear here; refresh to check for new messages.",13));
#if ANDROID
            body.Add(ShoppingUi.Text("Enable optional phone alerts from the options menu. Alerts keep message text off your lock screen.",13));
#else
            body.Add(ShoppingUi.Text("Questions are available here. Background phone alerts are not configured for this iOS build.",13));
#endif
            body.Add(ShoppingUi.Button("Browse household conversations",async()=>{
                AccountSession.Check(account);var household=await HouseholdService.StatusAsync();AccountSession.Check(account);
                if(household.HouseholdId==null)throw new InvalidOperationException("Join a household first.");
                var conversations=await HouseholdShoppingQuestions.Call<ShoppingConversationList>(new{action="list",householdId=household.HouseholdId});AccountSession.Check(account);
                body.Clear();body.Add(ShoppingUi.Text("Household item conversations",24,true));
                if(conversations.Threads.Count==0)body.Add(ShoppingUi.Text("No conversations yet. Swipe an item in the household list and choose Ask."));
                foreach(var conversation in conversations.Threads)body.Add(ShoppingUi.Button(conversation.Title+" · "+conversation.Count+" messages",()=>AppNavigation.Open(new HouseholdShoppingQuestionsPage(household.HouseholdId,conversation.ItemId)),true));
            },true));
            var items=inbox.Items.Where(i=>i.Type=="householdQuestion").ToList();if(items.Count==0)body.Add(ShoppingUi.Text("No shopping questions yet."));
            foreach(var item in items)body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=6,Children={ShoppingUi.Text((item.Read?"":"● ")+item.Message,16,true),ShoppingUi.Text(DateTimeOffset.FromUnixTimeMilliseconds(item.CreatedAt).ToLocalTime().ToString("g"),12),ShoppingUi.Button("Open conversation",async()=>{
                AccountSession.Check(account);await HouseholdService.CallEndpointAsync<object>(HouseholdShoppingQuestions.InboxEndpoint,new{action="read",id=item.Id});AccountSession.Check(account);
                await AppNavigation.Open(new HouseholdShoppingQuestionsPage(item.HouseholdId,item.ItemId));
            })}}));
        }catch(Exception e){body.Clear();body.Add(ShoppingUi.Text(e is HouseholdException or InvalidOperationException?e.Message:"Questions unavailable. Refresh to retry."));}
        finally{busy=false;}
    }
}
