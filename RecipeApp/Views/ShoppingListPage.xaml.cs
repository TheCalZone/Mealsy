using RecipeApp.ViewModels;
using RecipeApp.Services;
using System.Text.Json;

namespace RecipeApp.Views;

[QueryProperty(nameof(IsManualList), nameof(IsManualList))]
[QueryProperty(nameof(OpenPersonal), nameof(OpenPersonal))]
[QueryProperty(nameof(OpenHouseholdId), nameof(OpenHouseholdId))]
[QueryProperty(nameof(IngredientsJson), "ingredientsJson")]
public partial class ShoppingListPage : ContentPage
{
    public bool OpenPersonal { get; set; }
    public string OpenHouseholdId { get; set; }
    public bool IsManualList { get; set; }
    public string IngredientsJson { get; set; }
    private ShoppingListViewModel Model => (ShoppingListViewModel)BindingContext;
    private IDispatcherTimer timer;
    private string householdId, sharedName, listAccount;
    private int generation;
    private bool syncing, conflict, visible;
    private ShoppingSectionDrag sectionDrag;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<View,object> sectionHeaders=new();
    private bool shoppingMode,previousNav,previousTabs;
    private void SetShoppingMode(bool active){
        if(active==shoppingMode)return;
        if(active){previousNav=Shell.GetNavBarIsVisible(this);previousTabs=Shell.GetTabBarIsVisible(this);}
        shoppingMode=active;ShoppingHeader.IsVisible=!active;ShoppingTools.IsVisible=!active;ShoppingFooterActions.IsVisible=!active;ShoppingModeBar.IsVisible=active;
        AppNavigation.DismissKeyboard();
        Shell.SetNavBarIsVisible(this,active?false:previousNav);Shell.SetTabBarIsVisible(this,active?false:previousTabs);
        Unfocus();QuickAddEntry.Unfocus();
    }
    private void GoShopping_Clicked(object sender,EventArgs e)=>SetShoppingMode(true);
    private void ExitShopping_Clicked(object sender,EventArgs e)=>SetShoppingMode(false);
    private void ShoppingTools_Clicked(object sender,EventArgs e)=>ShoppingTools.IsVisible=!ShoppingTools.IsVisible;
    private string CacheKey => householdId == null ? ShoppingListStore.StateKey : $"ShoppingList.household.{listAccount}.{householdId}";
    public ShoppingListPage(){InitializeComponent();ToolbarItems.Add(new ToolbarItem("Reorder sections",null,async()=>await OpenSectionOrder()));ToolbarItems.Add(new ToolbarItem("Saved shopping trips · Premium",null,async()=>await PremiumAccess.OpenShopping()){Order=ToolbarItemOrder.Secondary});}

    private void SectionHeader_Loaded(object sender,EventArgs e)
    {
        if(sender is not View header||sectionHeaders.TryGetValue(header,out _))return;
        sectionHeaders.Add(header,new object());
        sectionDrag??=new ShoppingSectionDrag(Model.SectionNames,Model.SaveSectionOrder,()=>visible&&!Model.IsEditing&&!syncing&&listAccount==AccountSession.UserId);
        sectionDrag.Attach(header,()=>(header.BindingContext as ShoppingListGroup)?.Title);
    }
    private async void MoveItem_Clicked(object sender,EventArgs e)
    {
        if(sender is not BindableObject { BindingContext:RecipeApp.Models.ShoppingListItem item }||Model.IsEditing)return;
        var uid=AccountSession.UserId;var context=Model.SectionOrderContext;
        var group=Model.Groups.FirstOrDefault(g=>g.Contains(item));if(group==null)return;var index=group.IndexOf(item);var options=new List<string>();
        if(index>0)options.Add("Move up");if(index<group.Count-1)options.Add("Move down");options.Add("Edit item / change section");
        var choice=await DisplayActionSheet("Move "+item.Name+" within "+item.Category,"Cancel",null,options.ToArray());
        try{
            AccountSession.Check(uid);if(context!=Model.SectionOrderContext)return;
            if(choice=="Move up")Model.MoveItem(item,-1);else if(choice=="Move down")Model.MoveItem(item,1);else if(choice=="Edit item / change section")Model.EditItemCommand.Execute(item);
        }catch(Exception error){await DisplayAlert("Item order",error is InvalidOperationException?error.Message:"Couldn't save the item order. Please try again.","OK");}
    }
    private async Task OpenSectionOrder()
    {
        if(Model.IsEditing){await DisplayAlert("Finish editing","Save or cancel your item edit before rearranging sections.","OK");return;}
        await AppNavigation.Open(new ShoppingSectionOrderPage(Model));
    }
    private async void ReorderSections_Clicked(object sender,EventArgs e)=>await OpenSectionOrder();

    protected override async void OnAppearing()
    {
        base.OnAppearing(); visible = true;
        try
        {
            var uid = HouseholdService.UserId;
            if (listAccount != uid || !string.IsNullOrEmpty(IngredientsJson) || OpenPersonal) SelectPersonal();
            OpenPersonal=false;
            var requestedHousehold=OpenHouseholdId;OpenHouseholdId=null;
            if(!string.IsNullOrEmpty(requestedHousehold))
            {
                var info=await HouseholdService.StatusAsync();
                if(info.HouseholdId!=requestedHousehold||!info.FamilyEnabled)throw new InvalidOperationException("Household access ended.");
                generation++;householdId=info.HouseholdId;sharedName=info.Name;conflict=false;listAccount=uid;Model.SelectList(CacheKey);ListSelector.Text=sharedName+" ▾";
            }
            if (householdId == null) ListSelector.Text = uid == null ? "Device list ▾" : "Personal list ▾";
            listAccount = uid;
            // Confirm continuing membership before displaying a cached household list.
            if (householdId != null && string.IsNullOrEmpty(requestedHousehold))
            {
                var info = await HouseholdService.StatusAsync();
                if (info.HouseholdId != householdId || !info.FamilyEnabled) SelectPersonal();
            }
            if (!visible) return;
            var incoming = IngredientsJson; IngredientsJson = null;
            Model.Load(IsManualList, incoming);
            StartTimer();
            await SyncAsync();
        }
        catch (Exception) { SelectPersonal(); Model.Load(true, null); StartTimer(); Model.StatusText = "Personal list opened. Household access could not be confirmed."; }
    }
    private void SelectPersonal()
    {
        sectionDrag?.Cancel();
        generation++; householdId = sharedName = null; conflict = false;
        listAccount = HouseholdService.UserId;
        Model.SelectList(null); ListSelector.Text = listAccount == null ? "Device list ▾" : "Personal list ▾";
    }
    private void StartTimer()
    {
        timer?.Stop();
        timer = Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromSeconds(5);
        timer.Tick += Timer_Tick; timer.Start();
    }
    private async void Timer_Tick(object sender, EventArgs e) => await SyncAsync();
    private async Task SyncAsync(bool manual = false)
    {
        if (!visible) return;
        if (HouseholdService.UserId != listAccount) { SelectPersonal(); Model.Load(true, null); }
        if (syncing || conflict || Model.IsEditing || sectionDrag?.IsDragging==true || listAccount == null) return;
        syncing = true;
        var currentGeneration = generation;
        var uid = listAccount;
        var key = CacheKey;
        var sent = Model.CurrentJson;
        try
        {
            var baseline = Preferences.Get(key + ".synced", "[]");
            var changes = ShoppingListSync.Changes(baseline, sent);
            Model.StatusText = changes.Count > 0 ? "Syncing changes…" : "Checking for updates…";
            var response = await HouseholdService.CallAsync<ShoppingSyncResult>(new {
                action = changes.Count > 0 ? "patch" : "read", scope = householdId == null ? "personal" : "household", householdId, changes
            });
            if (generation != currentGeneration || HouseholdService.UserId != uid || Model.IsEditing) return;
            var server = JsonSerializer.Serialize(response.Items);
            var latest=Preferences.Get(key,Model.CurrentJson);
            var merged = ShoppingListSync.Reapply(sent, latest, server);
            // Save acknowledged server state separately so offline edits survive app restarts.
            Preferences.Set(key + ".synced", server);
            if (merged.Serialize() != Model.CurrentJson) Model.AcceptRemote(merged.Serialize());
            Model.StatusText = householdId == null ? "Personal list synced · Only you can access it" : "Household list synced · Updates every 5 seconds";
        }
        catch (ShoppingSyncConflictException)
        {
            if (generation == currentGeneration && HouseholdService.UserId == uid)
            { conflict = true; Model.StatusText = "Conflicting edits kept on this device. Open Resolve sync conflict."; }
        }
        catch (HouseholdException e)
        {
            if (generation != currentGeneration || HouseholdService.UserId != uid) return;
            if (e.Code == "ABORTED") { conflict = true; Model.StatusText = "Conflicting edits kept on this device. Open Resolve sync conflict."; }
            else if (householdId != null && e.Code is "PERMISSION_DENIED" or "FAILED_PRECONDITION")
            { SelectPersonal(); Model.Load(true, null); Model.StatusText = "Household access ended. Your personal list is open."; }
            else Model.StatusText = "Saved on device · " + e.Message;
            if (manual) await DisplayAlert("Shopping list sync", Model.StatusText, "OK");
        }
        catch (Exception error)
        {
            var reference=AppDiagnostics.Report(error,"shopping.sync","SYNC_FAILURE");
            if (generation == currentGeneration) Model.StatusText = "Saved on device · Cloud unavailable. Will retry while this screen is open.\nReference: "+reference;
            if (manual) await DisplayAlert("Sync unavailable", "Your changes are saved on this device. Check your connection and try syncing again.", "OK");
        }
        finally { syncing = false; }
    }
    private async void ListSelector_Clicked(object sender, EventArgs e)
    {
        if (Model.IsEditing) return;
        var choice = await DisplayActionSheet("Choose shopping list", "Cancel", null, "Personal list", "Household list", "Manage household");
        if (choice == "Personal list") { Model.Persist(); SelectPersonal(); Model.Load(true, null); await SyncAsync(true); }
        else if (choice == "Manage household") await Navigation.PushAsync(new HouseholdPage());
        else if (choice == "Household list")
        {
            try
            {
                var info = await HouseholdService.StatusAsync();
                if (info.HouseholdId == null) { await Navigation.PushAsync(new HouseholdPage()); return; }
                if (!info.FamilyEnabled) { await Navigation.PushAsync(new PlanPage()); return; }
                Model.Persist(); generation++; conflict = false; householdId = info.HouseholdId; sharedName = info.Name; listAccount = HouseholdService.UserId;
                Model.SelectList(CacheKey); Model.Load(true, null); ListSelector.Text = sharedName + " ▾";
                await SyncAsync(true);
            }
            catch (HouseholdException ex) { await DisplayAlert("Household", ex.Message, "OK"); }
            catch (Exception) { await DisplayAlert("Household unavailable", "Check your connection and try again.", "OK"); }
        }
    }
    private async void Widget_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new WidgetHelpPage());
    private async void Creator_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new ShoppingListMakerPage());
    private async void Household_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new HouseholdPage());
    private async void Sync_Clicked(object sender, EventArgs e) => await SyncAsync(true);
    private async void Resolve_Clicked(object sender, EventArgs e)
    {
        if (!conflict)
        {
            var recovery = Preferences.Get(CacheKey + ".recovery", "");
            if (string.IsNullOrEmpty(recovery)) { await DisplayAlert("Sync", "No conflict needs resolving and no recovery copy is stored for this list.", "OK"); return; }
            if (await DisplayAlert("Recovery copy available", "Share the device version kept before the last conflict was resolved?", "Share copy", "Cancel"))
                await Share.Default.RequestAsync(new ShareTextRequest { Title = "Shopping list recovery", Text = ShoppingListDocument.Deserialize(recovery).PlainText() });
            return;
        }
        var choice = await DisplayActionSheet("Keep a copy before reloading", "Cancel", null, "Share my device version", "Reload latest cloud list");
        if (choice == "Share my device version") { Model.ShareCommand.Execute(null); return; }
        if (choice != "Reload latest cloud list" || !await DisplayAlert("Reload cloud list?", "Your device version will be kept as a recovery copy. The screen will load the latest cloud version. You can then add your changes again.", "Reload", "Cancel")) return;
        try
        {
            var version = generation; var uid = listAccount; var key = CacheKey;
            var result = await HouseholdService.CallAsync<ShoppingSyncResult>(new { action = "read", scope = householdId == null ? "personal" : "household", householdId });
            if (version != generation || uid != HouseholdService.UserId) return;
            Preferences.Set(key + ".recovery", Model.CurrentJson);
            var server = JsonSerializer.Serialize(result.Items); Preferences.Set(key + ".synced", server);
            Model.AcceptRemote(server); conflict = false; Model.StatusText = "Latest list loaded · Device recovery copy kept";
        }
        catch (Exception) { await DisplayAlert("Unable to reload", "Your device version is still saved. Please try again when connected.", "OK"); }
    }
    protected override void OnDisappearing()
    {
        SetShoppingMode(false);
        _ = SyncAsync(); // Flush pending online edits when leaving; failures remain saved for retry.
        visible = false; timer?.Stop();
        // Never persist one account's visible list into another account's storage.
        if (listAccount == HouseholdService.UserId) Model.Persist();
        sectionDrag?.Cancel();
        base.OnDisappearing();
    }
    protected override bool OnBackButtonPressed()
    {
        if (Model.IsEditing) { Model.CancelEditCommand.Execute(null); return true; }
        if (shoppingMode) { SetShoppingMode(false); return true; }
        return base.OnBackButtonPressed();
    }
    private void RemoveEditingItem_Clicked(object sender, EventArgs e) => Model.RemoveEditingItem();
}
