using RecipeApp.Services;

namespace RecipeApp.Views;

/// <summary>Members use their own Firebase credentials; no passwords are shared.</summary>
public class HouseholdPage : ContentPage
{
    private readonly VerticalStackLayout content = new() { Spacing = 14, Padding = 24 };
    private readonly Label status = new() { FontSize = 14 };
    private bool busy;
    private string account;
    private readonly HashSet<string> expanded = new();
    private View Drawer(string key,string title,string subtitle,View details)
    {
        var panel=new ContentView {Content=details,IsVisible=expanded.Contains(key)};
        Button button=null;
        button=ShoppingUi.Button("",()=>{
            panel.IsVisible=!panel.IsVisible;
            if(panel.IsVisible)expanded.Add(key);else expanded.Remove(key);
            Update();return Task.CompletedTask;
        },true);
        button.FontSize=17;button.FontAttributes=FontAttributes.Bold;
        void Update()
        {
            button.Text=title+(panel.IsVisible?"  ▴":"  ▾");
            SemanticProperties.SetDescription(button,title+", "+subtitle+(panel.IsVisible?", expanded":", collapsed"));
            SemanticProperties.SetHint(button,panel.IsVisible?"Tap to hide options":"Tap to show options");
        }
        Update();
        var body=new VerticalStackLayout {Spacing=8,Children={button,ShoppingUi.Text(subtitle,13),panel}};
        return ShoppingUi.Card(body);
    }
    private async Task Invite(HouseholdInfo household)
    {
        CheckAccount();
        var role="adult";
        if(household.PermissionsVersion>=1)
        {
            var roleChoice=await DisplayActionSheet("Invitation access","Cancel",null,"Restricted · Request additions","Adult · Edit and approve");
            if(roleChoice==null||roleChoice=="Cancel")return;
            role=roleChoice.StartsWith("Restricted")?"restricted":"adult";CheckAccount();
        }
        var invite=await HouseholdService.CallAsync<System.Text.Json.JsonElement>(new {action="invite",role});
        CheckAccount();var code=invite.GetProperty("code").GetString();
        var choice=await DisplayActionSheet("Invitation ready · expires in 7 days","Cancel",null,"Share invitation","Copy code");
        CheckAccount();
        if(choice=="Copy code"){await Clipboard.Default.SetTextAsync(code);status.Text="Invitation code copied. Each code can be used once.";}
        else if(choice=="Share invitation")await Share.Default.RequestAsync(new ShareTextRequest {Title="Join my Mealsy household",Text=InvitationLinks.ShareHousehold(household.Name,code)});
    }
    public HouseholdPage()
    {
        Title = AppLocalization.Text("Your household");
        ShoppingUi.Theme(this);
        Content = new ScrollView { Content = content };
        ToolbarItems.Add(new ToolbarItem("Refresh household",null,async()=>await Run(Refresh)){Order=ToolbarItemOrder.Secondary});
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Refresh); }
    private Button Action(string title,Func<Task> action)=>ShoppingUi.Button(title,()=>Run(action));
    private async Task Run(Func<Task> action)
    {
        if (busy) return;
        busy = true;IsBusy=true;
        try { await action(); }
        catch (HouseholdException e) { await DisplayAlert(AppLocalization.Text("Household"), e.Message, AppLocalization.Text("OK")); }
        catch (Exception) { await DisplayAlert("Connection unavailable", "Could not reach the household service. Check your connection and try again.", AppLocalization.Text("OK")); }
        finally { foreach(var loader in content.Children.OfType<LoadingIndicator>().ToList())content.Remove(loader);busy = false;IsBusy=false; }
    }
    private void CheckAccount()
    {
        if (account != HouseholdService.UserId) throw new HouseholdException("UNAUTHENTICATED", "Your account changed. Reopen this screen.");
    }
    private async Task Refresh()
    {
        if(account!=HouseholdService.UserId)expanded.Clear();
        account = HouseholdService.UserId;
        status.Text="";
        content.Clear();
        content.Add(ShoppingUi.Text("Your household", 30, true));
        content.Add(ShoppingUi.Text("Shared shopping and meals. Your personal lists stay private.",14));
        if (account == null) { content.Add(ShoppingUi.Text("Sign in or create an account on the Account tab to get started."));content.Add(Action("Create free account",()=>AppNavigation.Open(new SignupPage())));content.Add(Action("Already have an account? Sign in",()=>AppNavigation.Root("AccountPage"))); return; }
        content.Add(new LoadingIndicator("Loading your household…"));
        PlanStatus plan;
        try { plan = await PlanService.RefreshAsync(); }
        catch (Exception) { plan = PlanService.Cached ?? new PlanStatus(false, false, false, 0, true, "", ""); }
        // Membership cleanup must remain available even if Play's API is unavailable.

        var household = await HouseholdService.StatusAsync();
        CheckAccount();
        foreach(var loader in content.Children.OfType<LoadingIndicator>().ToList())content.Remove(loader);
        if (household.HouseholdId == null)
        {
            content.Add(Action(plan.OwnPlan?"Manage Premium":"Explore Premium household sharing",()=>AppNavigation.Open(new PlanPage())));
            if (!plan.OwnPlan) content.Add(ShoppingUi.Text("A subscription held by the owner is required to create a household. You can join an owner who already subscribes."));
            if (plan.OwnPlan) content.Add(Action("Create a household", async () => {
                var name = await DisplayPromptAsync("Create household", "What would you like to call your household?", placeholder: "Our family", maxLength: 80);
                if (string.IsNullOrWhiteSpace(name)) return;
                CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "create", name }); await Refresh();
            }));
            content.Add(Action("Join with invitation code", async () => {
                var code = await DisplayPromptAsync("Join your family", "Paste the invitation code or Mealsy invitation link sent by the household owner.", maxLength: 2000);
                if (string.IsNullOrWhiteSpace(code)) return;
                if (!await DisplayAlert("Join household?", "Your account email will be visible to the household owner and other members. Your personal list stays private.", "Join", AppLocalization.Text("Cancel"))) return;
                CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "join", code=InvitationLinks.Code(code) }); await Refresh();
            }));
        }
        else
        {
            var isOwner=household.Owner==account;
            content.Add(ShoppingUi.Card(new VerticalStackLayout {Spacing=6,Children={
                ShoppingUi.Text(household.Name,25,true),
                ShoppingUi.Text($"{household.Members.Count} of {household.MemberLimit} people · "+(isOwner?"You manage this household":"You’re a member"),14)
            }}));
            if(!household.FamilyEnabled)content.Add(ShoppingUi.Text("Sharing is paused. The owner needs to renew or restore Premium. Your household data is kept.",14));
            else
            {
                content.Add(Action("Open shared shopping list",()=>AppNavigation.ShoppingList("//ShoppingListPage?OpenHouseholdId="+Uri.EscapeDataString(household.HouseholdId))));
                content.Add(Action("Open shared meal plan",()=>AppNavigation.Open(new HouseholdMealsPage())));
            }
            if(household.PermissionsVersion>=1)content.Add(Action($"Requests · {household.PendingRequests} pending",()=>AppNavigation.Open(new HouseholdRequestsPage())));
            else content.Add(ShoppingUi.Text("Roles and approval requests need the updated Firebase household backend before they can be enabled.",13));
            content.Add(ShoppingUi.Text("Family members",21,true));
            content.Add(ShoppingUi.Text("Tap a person to see their role and available options.",13));
            foreach(var member in household.Members.OrderByDescending(m=>m.Id==household.Owner).ThenBy(m=>m.Label))
            {
                var owner=member.Id==household.Owner;var self=member.Id==account;
                var options=new VerticalStackLayout {Spacing=9};
                options.Add(ShoppingUi.Text(owner?"Manages invitations, roles and membership. Their Premium plan covers the household.":member.Role=="restricted"?"Can view shared lists and meals, check off shopping, and request additions. An adult approves requests. Personal lists stay unrestricted.":"Can edit shared shopping and meals, and approve or decline requests.",14));
                if(self)options.Add(Action("Open my account",()=>AppNavigation.Root("AccountPage")));
                if(isOwner&&!owner&&household.PermissionsVersion>=1)options.Add(Action("Change household role",async()=>{
                    var choice=await DisplayActionSheet("Access for "+member.Label,"Cancel",null,"Adult · Edit and approve","Restricted · Request additions");
                    if(choice==null||choice=="Cancel")return;
                    var role=choice.StartsWith("Restricted")?"restricted":"adult";
                    if(!await DisplayAlert("Change access?",role=="restricted"?"Shared additions will become requests. They can still check off items and use their personal lists freely.":"They will be able to edit shared shopping and meals and approve requests.","Change role","Keep current"))return;
                    CheckAccount();await HouseholdService.CallAsync<object>(new {action="setRole",householdId=household.HouseholdId,member=member.Id,role});await Refresh();
                }));
                if(isOwner&&!owner)options.Add(Action("Remove from household",async()=>{
                    if(!await DisplayAlert("Remove "+member.Label+"?","They will lose access to shared shopping and meal plans. Their personal lists, recipes and account are kept.",AppLocalization.Text("Remove"),AppLocalization.Text("Cancel")))return;
                    CheckAccount();await HouseholdService.CallAsync<object>(new {action="remove",member=member.Id});expanded.Remove("member:"+member.Id);await Refresh();
                }));
                if(self&&!owner)options.Add(Action("Leave household",async()=>{
                    if(!await DisplayAlert("Leave household?","You will lose access to shared shopping and meal plans. Your personal lists, recipes and account are kept.","Leave",AppLocalization.Text("Cancel")))return;
                    CheckAccount();await HouseholdService.CallAsync<object>(new {action="leave"});expanded.Clear();await Refresh();
                }));
                if(!isOwner&&!self)options.Add(ShoppingUi.Text("Only the household owner can remove members.",13));
                content.Add(Drawer("member:"+member.Id,member.Label,(owner?"Owner":member.Role=="restricted"?"Restricted member":"Adult")+(self?" · You":""),options));
            }
            if(isOwner)
            {
                var invitations=new VerticalStackLayout {Spacing=10};
                invitations.Add(ShoppingUi.Text("Each invitation is single-use and lasts seven days. A new code replaces any unused code.",14));
                if(household.FamilyEnabled&&household.Members.Count<household.MemberLimit)
                    invitations.Add(Action("Share a family invitation",()=>Invite(household)));
                else invitations.Add(ShoppingUi.Text(!household.FamilyEnabled?"Restore Premium to invite someone.":"Your household is full. Manage places in your subscription options, or remove a member before inviting someone.",14));
                invitations.Add(Action("Revoke unused invitation",async()=>{
                    if(!await DisplayAlert("Revoke invitation?","An unused code will stop working. Existing members keep their access.","Revoke",AppLocalization.Text("Cancel")))return;
                    CheckAccount();await HouseholdService.CallAsync<object>(new {action="revoke"});status.Text="Unused invitation revoked.";
                }));
                content.Add(Drawer("invitations","Invite family","Share or revoke an invitation",invitations));
            }
            var membership=new VerticalStackLayout {Spacing=9};
            membership.Add(ShoppingUi.Text(isOwner?"Your Premium membership covers this household. Each member keeps their own login and personal lists.":"The owner's Premium membership covers you. You do not need a separate subscription for household sharing.",14));
            if(isOwner)membership.Add(Action("Rename household list",async()=>{
                if(!household.CanRenameHousehold){await DisplayAlert("Household update needed","The household backend needs updating before names can be changed.","OK");return;}
                var name=await DisplayPromptAsync("Rename household list","This updates the household name used for shared shopping and meal plans. Items and members stay the same.","Save",AppLocalization.Text("Cancel"),maxLength:80,initialValue:household.Name);
                if(name==null)return;
                name=name.Trim();
                if(name.Length==0){await DisplayAlert("Name needed","Enter a name for your household list.","OK");return;}
                if(name==household.Name)return;
                CheckAccount();await HouseholdService.CallAsync<object>(new {action="rename",householdId=household.HouseholdId,name});await Refresh();
            }));
            if(isOwner)membership.Add(Action("Manage Premium & household places",()=>AppNavigation.Open(new PlanPage())));
            content.Add(Drawer("membership","Household membership",household.FamilyEnabled?"Sharing active":"Sharing paused",membership));
            if(isOwner)content.Add(Drawer("delete","Delete household","Permanent removal · Owner only",new VerticalStackLayout {Spacing=9,Children={
                ShoppingUi.Text("Removes shared shopping, meal plans and household access. Everyone keeps their personal account and lists. This does not cancel your subscription.",14),
                Action("Delete household permanently",async()=>{
                    if(!await DisplayAlert("Delete household permanently?","The shared shopping list and meal plans will be deleted for everyone. Personal accounts and lists are kept. This cannot be undone and does not cancel your subscription.","Delete household",AppLocalization.Text("Cancel")))return;
                    CheckAccount();await HouseholdService.CallAsync<object>(new {action="delete"});expanded.Clear();await Refresh();
                })
            }}));
        }
        var deviceOptions=new VerticalStackLayout {Spacing=9};
        deviceOptions.Add(ShoppingUi.Text("Switch lists using the scrolling tabs in Shopping. This optional import copies the original guest list into your main personal list.",14));
        deviceOptions.Add(Action("Copy original device list to my personal list", async () => {
            if (!await DisplayAlert("Copy device list?", "The original list may have been used by other people on this device. Copy its items into your private personal list?", "Copy", AppLocalization.Text("Cancel"))) return;
            CheckAccount();
            var saved = Preferences.Get("ShoppingList.v2", "");
            var source = string.IsNullOrWhiteSpace(saved) ? new ShoppingListDocument() : ShoppingListDocument.Deserialize(saved);
            if (string.IsNullOrWhiteSpace(saved)) source.MergeLegacyText(Preferences.Get("ShoppingListText", ""));
            var targetJson = Preferences.Get(ShoppingListStore.StateKey, "[]");
            var target = ShoppingListDocument.Deserialize(targetJson);
            foreach (var item in source.Items) if (!target.Items.Any(i => i.Id == item.Id)) target.Add(item);
            ShoppingListStore.Save(target); status.Text = "Copied to your personal list. Open it to sync.";
        }));
        content.Add(Drawer("device","Device list import","Optional · Keeps the original list",deviceOptions));
        status.SetDynamicResource(Label.TextColorProperty, "ThemeText"); content.Add(status);
    }
}
