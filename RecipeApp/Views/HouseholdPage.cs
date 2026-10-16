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
        BackgroundColor = Color.FromArgb("#F5F7F2");
        Content = new ScrollView { Content = content };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Refresh); }
    private Button Action(string title, Func<Task> action)
    {
        var button = new Button { Text = title, CornerRadius = 12, BackgroundColor = Color.FromArgb("#214D35"), TextColor = Colors.White };
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
        content.Add(new Label { Text = "Shop together", FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#214D35") });
        content.Add(new Label { Text = "Each person keeps a private list and their own login. Everyone in your household can add, edit, remove and collect items on the household list.", TextColor = Color.FromArgb("#354B3C") });
        if (account == null) { content.Add(new Label { Text = "Sign in or create an account on the Account tab to get started.", TextColor = Colors.Black }); return; }
        var household = await HouseholdService.StatusAsync();
        CheckAccount();
        if (household.HouseholdId == null)
        {
            content.Add(Action("Create a household", async () => {
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
            content.Add(new Label { Text = household.Name, FontSize = 24, TextColor = Color.FromArgb("#214D35"), FontAttributes = FontAttributes.Bold });
            content.Add(new Label { Text = $"{household.Members.Count} of 20 members", TextColor = Colors.Black });
            foreach (var member in household.Members)
            {
                var owner = member.Id == household.Owner;
                content.Add(new Label { Text = member.Label + (owner ? " · Owner" : "") + (member.Id == account ? " · You" : ""), TextColor = Colors.Black });
                if (household.Owner == account && !owner)
                    content.Add(Action("Remove " + member.Label, async () => {
                        if (!await DisplayAlert("Remove member?", "They will lose access to the shared list. Their personal list will be kept.", "Remove", "Cancel")) return;
                        CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "remove", member = member.Id }); await Refresh();
                    }));
            }
            if (household.Owner == account)
            {
                content.Add(Action("Invite a family member", async () => {
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
                content.Add(new Label { Text = "Only the owner can invite or remove members. Each invitation is single-use and lasts 7 days. Creating a new code replaces any unused code.", TextColor = Colors.Black });
                content.Add(Action("Revoke unused invitation", async () => {
                    CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "revoke" }); status.Text = "Unused invitation revoked.";
                }));
            }
            else content.Add(Action("Leave household", async () => {
                if (!await DisplayAlert("Leave household?", "You will lose access to its shopping list. Your personal list stays with your account.", "Leave", "Cancel")) return;
                CheckAccount(); await HouseholdService.CallAsync<object>(new { action = "leave" }); await Refresh();
            }));
        }
        content.Add(new Label { Text = "Switch between Personal and Household from the list selector on the shopping screen. Recipe shortcuts add to your personal list.", TextColor = Colors.Black });
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
        status.TextColor = Color.FromArgb("#214D35"); content.Add(status);
    }
}
