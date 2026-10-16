using RecipeApp.Services;

namespace RecipeApp.Views;

/// <summary>Members use their own Firebase credentials; no passwords are shared.</summary>
public class HouseholdPage : ContentPage
{
    private readonly VerticalStackLayout content = new() { Spacing = 14, Padding = 24 };
    private readonly Label status = new() { FontSize = 14 };
    private bool busy;
    private string account;
    public HouseholdPage()
    {
        Title = "Your household";
        ShoppingUi.Theme(this);
        Content = new ScrollView { Content = content };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Refresh); }
    private Button Action(string title, Func<Task> action)
    {
        var button = new Button { Text = title, CornerRadius = 12, TextColor = Colors.White };
        button.SetDynamicResource(VisualElement.BackgroundColorProperty, "ThemeAccent");
        button.Clicked += async (_, _) => await Run(action);
        return button;
    }
    private async Task Run(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        try { await action(); }
        catch (HouseholdException e) { await DisplayAlert("Household", e.Message, "OK"); }
        catch (Exception) { await DisplayAlert("Connection unavailable", "Could not reach the household service. Check your connection. The Firebase household function must be deployed before this feature can be used.", "OK"); }
        finally { busy = false; }
    }
    private void CheckAccount()
    {
        if (account != HouseholdService.UserId) throw new HouseholdException("UNAUTHENTICATED", "Your account changed. Reopen this screen.");
    }
    private async Task Refresh()
    {
        account = HouseholdService.UserId;
        content.Clear();
        content.Add(ShoppingUi.Text("Shop together", 30, true));
        content.Add(ShoppingUi.Text("Each person keeps a private list and their own login. Everyone in your household can add, edit, remove and collect items on the household list."));
        if (account == null) { content.Add(ShoppingUi.Text("Sign in or create an account on the Account tab to get started.")); return; }
        PlanStatus plan;
        try { plan = await PlanService.RefreshAsync(); }
        catch (Exception) { plan = PlanService.Cached ?? new PlanStatus(false, false, false, 0, true, "", ""); }
        // Membership cleanup must remain available even if Play's API is unavailable.
        content.Add(Action(plan.FamilyEnabled ? "Mealsy Family · Manage plan" : "Unlock Family · £3/month or £24/year", async () => await Navigation.PushAsync(new PlanPage())));
        var household = await HouseholdService.StatusAsync();
        CheckAccount();
        if (household.HouseholdId == null)
        {
            if (!plan.OwnPlan) content.Add(ShoppingUi.Text("A subscription held by the owner is required to create a household. You can join an owner who already subscribes."));
            if (plan.OwnPlan) content.Add(Action("Create a household", async () => {
                var name = await DisplayPromptAsync("Create household", "What would you like to call your household?", placeholder: "Our family", maxLength: 80);
                if (string.IsNullOrWhiteSpace(name)) return;
                CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "create", name }); await Refresh();
            }));
            content.Add(Action("Join with invitation code", async () => {
                var code = await DisplayPromptAsync("Join your family", "Paste the invitation code sent by the household owner.", maxLength: 48);
                if (string.IsNullOrWhiteSpace(code)) return;
                if (!await DisplayAlert("Join household?", "Your account email will be visible to the household owner and other members. Your personal list stays private.", "Join", "Cancel")) return;
                CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "join", code }); await Refresh();
            }));
        }
        else
        {
            if (!household.FamilyEnabled) content.Add(ShoppingUi.Text("Family sharing is paused. The household owner needs to renew or restore their Family plan. Your household data has been kept."));
            content.Add(ShoppingUi.Text(household.Name, 24, true));
            content.Add(ShoppingUi.Text($"{household.Members.Count} of 20 members"));
            foreach (var member in household.Members)
            {
                var owner = member.Id == household.Owner;
                content.Add(ShoppingUi.Text(member.Label + (owner ? " · Owner" : "") + (member.Id == account ? " · You" : "")));
                if (household.Owner == account && !owner)
                    content.Add(Action("Remove " + member.Label, async () => {
                        if (!await DisplayAlert("Remove member?", "They will lose access to the shared list. Their personal list will be kept.", "Remove", "Cancel")) return;
                        CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "remove", member = member.Id }); await Refresh();
                    }));
            }
            if (household.Owner == account)
            {
                if (household.FamilyEnabled) content.Add(Action("Invite a family member", async () => {
                    CheckAccount();
                    var invite = await HouseholdService.CallAsync<System.Text.Json.JsonElement>(new { action = "invite" });
                    var code = invite.GetProperty("code").GetString();
                    // Keep the code visible even if the share sheet is dismissed.
                    var choice = await DisplayActionSheet("Invitation ready · expires in 7 days", "Cancel", null, "Share invitation", "Copy code");
                    if (choice == "Copy code") { await Clipboard.Default.SetTextAsync(code); status.Text = "Invitation code copied. Each code can be used once."; }
                    else if (choice == "Share invitation") await Share.Default.RequestAsync(new ShareTextRequest { Title = "Join my Mealsy household", Text = $"Join {household.Name} on Mealsy! Sign in with your own account, open Shopping list → Household → Join with invitation code, and paste:\n\n{code}\n\nThis code is single-use and expires in 7 days. Only share it with the person you want to invite." });
                }));
                content.Add(Action("Delete household", async () => {
                    if (!await DisplayAlert("Delete household permanently?", "This deletes the shared shopping list and removes access for every member. Everyone keeps their personal list. This cannot be undone.", "Delete household", "Cancel")) return;
                    CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "delete" }); await Refresh();
                }));
                content.Add(ShoppingUi.Text("Only the owner can invite or remove members. Each invitation is single-use and lasts 7 days. Creating a new code replaces any unused code."));
                content.Add(Action("Revoke unused invitation", async () => {
                    CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "revoke" }); status.Text = "Unused invitation revoked.";
                }));
            }
            else content.Add(Action("Leave household", async () => {
                if (!await DisplayAlert("Leave household?", "You will lose access to its shopping list. Your personal list stays with your account.", "Leave", "Cancel")) return;
                CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "leave" }); await Refresh();
            }));
        }
        content.Add(ShoppingUi.Text("Switch between Personal and Household from the list selector on the shopping screen. Recipe shortcuts add to your personal list."));
        content.Add(Action("Copy this device's original list to my personal list", async () => {
            if (!await DisplayAlert("Copy device list?", "The original list may have been used by other people on this device. Copy its items into your private personal list?", "Copy", "Cancel")) return;
            CheckAccount();
            var saved = Preferences.Get("ShoppingList.v2", "");
            var source = string.IsNullOrWhiteSpace(saved) ? new ShoppingListDocument() : ShoppingListDocument.Deserialize(saved);
            if (string.IsNullOrWhiteSpace(saved)) source.MergeLegacyText(Preferences.Get("ShoppingListText", ""));
            var targetJson = Preferences.Get(ShoppingListStore.StateKey, "[]");
            var target = ShoppingListDocument.Deserialize(targetJson);
            foreach (var item in source.Items) if (!target.Items.Any(i => i.Id == item.Id)) target.Add(item);
            ShoppingListStore.Save(target); status.Text = "Copied to your personal list. Open it to sync.";
        }));
        status.SetDynamicResource(Label.TextColorProperty, "ThemeText"); content.Add(status);
    }
}
