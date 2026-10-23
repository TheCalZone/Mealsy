using RecipeApp.ViewModels;
using RecipeApp.Services;
using System.Text.Json;

namespace RecipeApp.Views;

[QueryProperty(nameof(IsManualList), nameof(IsManualList))]
[QueryProperty(nameof(OpenPersonal), nameof(OpenPersonal))]
[QueryProperty(nameof(PersonalListId), nameof(PersonalListId))]
[QueryProperty(nameof(OpenHouseholdId), nameof(OpenHouseholdId))]
[QueryProperty(nameof(IngredientsJson), "ingredientsJson")]
public partial class ShoppingListPage : ContentPage
{
    public string PersonalListId { get; set; }
    private string personalId="main";
    private HouseholdInfo availableHousehold;
    private bool openingDefault=true;
    public bool OpenPersonal { get; set; }
    public string OpenHouseholdId { get; set; }
    public bool IsManualList { get; set; }
    public string IngredientsJson { get; set; }
    private ShoppingListViewModel Model => (ShoppingListViewModel)BindingContext;
    private IDispatcherTimer timer;
    private string retryScope;
    private DateTimeOffset retryAfter;
    private string householdId, sharedName, listAccount;
    private int generation;
    private bool syncing, conflict, visible;
    private ShoppingSectionDrag sectionDrag;
    private ShoppingItemDrag itemDrag;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<View,object> itemDragViews=new();
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
    private string CacheKey => householdId == null ? PersonalShoppingLists.StorageKey(personalId) : $"ShoppingList.household.{listAccount}.{householdId}";
    public ShoppingListPage(){InitializeComponent();ShoppingUi.Theme(this);QuickSuggestions.Configure(QuickAddEntry,QuickQuantityEntry,()=>Model.Groups.SelectMany(group=>group).Select(item=>item.Name),()=>Model.SelectedCategory,value=>{if(!Model.Categories.Contains(value))Model.Categories.Add(value);Model.SelectedCategory=value;});ToolbarItems.Add(new ToolbarItem("Checked item settings",null,async()=>await CheckedItemSettings()){Order=ToolbarItemOrder.Secondary});ToolbarItems.Add(new ToolbarItem(AppLocalization.Text("Reorder sections"),null,async()=>await OpenSectionOrder()));ToolbarItems.Add(new ToolbarItem("Saved shopping trips · Premium",null,async()=>await PremiumAccess.OpenShopping()){Order=ToolbarItemOrder.Secondary});}

    private void SectionHeader_Loaded(object sender,EventArgs e)
    {
        if(sender is not View header||sectionHeaders.TryGetValue(header,out _)||header.FindByName<Label>("SectionMoveHandle") is not Label handle)return;
        sectionHeaders.Add(header,new object());
        sectionDrag??=new ShoppingSectionDrag(Model.SectionNames,Model.SaveSectionOrder,()=>visible&&!Model.IsEditing&&!syncing&&listAccount==AccountSession.UserId,()=>Model.SectionOrderContext,()=>Model.SetSectionDragPreview(true),()=>Model.SetSectionDragPreview(false));
        sectionDrag.Attach(header,handle,()=>(header.BindingContext as ShoppingListGroup)?.Title);
    }
    private ShoppingItemDrag ItemDrag=>itemDrag??=new ShoppingItemDrag(Model,()=>visible&&!Model.IsEditing&&!syncing&&listAccount==AccountSession.UserId);
    private void ItemHandle_Loaded(object sender,EventArgs e)
    {
        if(sender is not View handle||itemDragViews.TryGetValue(handle,out _))return;
        itemDragViews.Add(handle,new object());ItemDrag.AttachHandle(handle);
    }
    private void ItemRow_Loaded(object sender,EventArgs e)
    {
        if(sender is not View row||itemDragViews.TryGetValue(row,out _))return;
        itemDragViews.Add(row,new object());ItemDrag.AttachRow(row);
    }
    private async void CheckedItemSettings_Clicked(object sender,EventArgs e)
    {
        try{await CheckedItemSettings();}
        catch(Exception){await DisplayAlert("Settings not changed","Couldn't save this preference. Please try again.",AppLocalization.Text("OK"));}
    }
    private async Task CheckedItemSettings()
    {
        var account=AccountSession.UserId;
        var current=Model.DeleteCollectedOnCheck?"Delete as I check":Model.HideCollected?"Hidden":"Visible";
        var choice=await DisplayActionSheet("Checked items · "+current,AppLocalization.Text("Cancel"),null,
            "Keep checked items visible","Hide checked items","Delete as I check");
        if(choice==null || choice==AppLocalization.Text("Cancel"))return;
        AccountSession.Check(account);
        var remove=choice=="Delete as I check";
        if(remove && !await DisplayAlert("Delete items as you check them?",
            "Items you check on this device will be removed from the list. On a household list, this removes them for everyone when synced. Existing checked items stay. You can Undo while the list stays open.","Enable auto-delete",AppLocalization.Text("Cancel")))return;
        AccountSession.Check(account);
        var hide=choice=="Hide checked items";
        AppSettings.Save(AppSettings.Current with {HideCollected=hide,DeleteCollectedOnCheck=remove});
        Model.DeleteCollectedOnCheck=remove;
        Model.HideCollected=hide;
        Model.StatusText=remove?"Checked items will be deleted · Undo available":hide?"Checked items hidden, not deleted":"Checked items stay visible";
    }
    private async Task OpenSectionOrder()
    {
        if(Model.IsEditing){await DisplayAlert("Finish editing","Save or cancel your item edit before rearranging sections.",AppLocalization.Text("OK"));return;}
        await AppNavigation.Open(new ShoppingSectionOrderPage(Model));
    }
    private async void ReorderSections_Clicked(object sender,EventArgs e)=>await OpenSectionOrder();

    protected override async void OnAppearing()
    {
        base.OnAppearing(); visible = true; ShoppingMembership.Refresh();
        WidgetShoppingStore.Changed -= WidgetChanged;
        WidgetShoppingStore.Changed += WidgetChanged;
        try
        {
            var uid = HouseholdService.UserId;
            var explicitPersonal=OpenPersonal||!string.IsNullOrEmpty(IngredientsJson)||!string.IsNullOrEmpty(PersonalListId);
            var requestedPersonal=PersonalListId;PersonalListId=null;
            if(listAccount!=uid){openingDefault=true;availableHousehold=null;}
            if(!string.IsNullOrEmpty(IngredientsJson)&&listAccount==uid)Model.Persist();
            if (listAccount != uid || !string.IsNullOrEmpty(IngredientsJson) || OpenPersonal) SelectPersonal();
            OpenPersonal=false;
            var requestedHousehold=OpenHouseholdId;OpenHouseholdId=null;
            if(!string.IsNullOrEmpty(requestedHousehold))
            {
                var info=await HouseholdService.StatusAsync();
                if(info.HouseholdId!=requestedHousehold||!info.FamilyEnabled)throw new InvalidOperationException("Household access ended.");
                generation++;householdId=info.HouseholdId;sharedName=info.Name;conflict=false;listAccount=uid;Model.SelectList(CacheKey);ListSelector.Text=sharedName+" ▾";
            }
            if (householdId == null) ListSelector.Text = uid == null ? AppLocalization.Text("Device list ▾") : AppLocalization.Text("Personal list ▾");
            if(!string.IsNullOrEmpty(requestedPersonal))
            {
                if(!PersonalShoppingLists.All().Any(l=>l.Id==requestedPersonal))throw new InvalidOperationException("List no longer exists.");
                SelectNamedPersonal(requestedPersonal);
            }
            listAccount = uid;
            if(uid!=null)
            {
                try { availableHousehold=await HouseholdService.StatusAsync();AccountSession.Check(uid); }
                catch { if(listAccount!=AccountSession.UserId)throw;availableHousehold=null; }
            }
            if(openingDefault&&!explicitPersonal&&string.IsNullOrEmpty(requestedHousehold)&&availableHousehold is {FamilyEnabled:true,HouseholdId:not null})
                SelectHousehold(availableHousehold);
            openingDefault=false;
            // Confirm continuing membership before displaying a cached household list.
            if (householdId != null && string.IsNullOrEmpty(requestedHousehold))
            {
                var info = await HouseholdService.StatusAsync();
                if (info.HouseholdId != householdId || !info.FamilyEnabled) SelectPersonal();
            }
            if (!visible) return;
            var incoming = IngredientsJson; IngredientsJson = null;
            if(!string.IsNullOrWhiteSpace(incoming)&&PersonalShoppingLists.All().Count>1)
            {
                var lists=PersonalShoppingLists.All();
                var names=lists.Select((l,i)=>$"{i+1}. {l.Name}").ToArray();
                var choice=await DisplayActionSheet("Send recipe ingredients to", "Keep on main personal list",null,names);
                AccountSession.Check(uid);
                var index=Array.IndexOf(names,choice);
                if(index>=0)SelectNamedPersonal(lists[index].Id);
            }
            Model.Load(IsManualList, incoming);
            _=QuickSuggestions.Refresh();
            RenderListTabs();StartTimer();
            await SyncAsync();
        }
        catch (Exception) { SelectPersonal(); Model.Load(true, null); StartTimer(); Model.StatusText = "Personal list opened. Household access could not be confirmed."; RenderListTabs(); }
    }
    private void SelectPersonal()
    {
        sectionDrag?.Cancel();itemDrag?.Cancel();
        generation++; householdId = sharedName = null; conflict = false;
        listAccount = HouseholdService.UserId;
        personalId="main";Model.SelectList(null); ListSelector.Text = listAccount == null ? AppLocalization.Text("Device list ▾") : AppLocalization.Text("Personal list ▾");
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
        if (!visible || !App.IsForeground) return;
        if (HouseholdService.UserId != listAccount) { SelectPersonal(); Model.Load(true, null); }
        if(personalId!="main"&&householdId==null){Model.StatusText="Only you · Saved on this device";return;}
        if (syncing || conflict || Model.IsEditing || sectionDrag?.IsDragging==true || itemDrag?.IsDragging==true || listAccount == null) return;
        if(!manual&&retryScope==CacheKey&&DateTimeOffset.UtcNow<retryAfter)return;
        syncing = true;
        var failed=false;
        var currentGeneration = generation;
        var uid = listAccount;
        var key = CacheKey;
        var sent = Model.CurrentJson;
        try
        {
            var baseline = Preferences.Get(key + ".synced", "[]");
            var changes = ShoppingListSync.Changes(baseline, sent);
            Model.StatusText = changes.Count > 0 ? AppLocalization.Text("Syncing changes…") : AppLocalization.Text("Checking for updates…");
            var response = await HouseholdService.CallAsync<ShoppingSyncResult>(new {
                action = changes.Count > 0 ? "patch" : "read", scope = householdId == null ? "personal" : "household", householdId, changes
            },bypassReadCache:manual);
            if (generation != currentGeneration || HouseholdService.UserId != uid || Model.IsEditing) return;
            var server = JsonSerializer.Serialize(response.Items);
            var latest=Preferences.Get(key,Model.CurrentJson);
            var merged = ShoppingListSync.Reapply(sent, latest, server);
            // Save acknowledged server state separately so offline edits survive app restarts.
            Preferences.Set(key + ".synced", server);
            if (merged.Serialize() != Model.CurrentJson) Model.AcceptRemote(merged.Serialize());
            Model.StatusText = householdId == null ? AppLocalization.Text("Personal list synced · Only you can access it") : AppLocalization.Text("Household list synced · Checks once a minute");
        }
        catch (ShoppingSyncConflictException)
        {
            if (generation == currentGeneration && HouseholdService.UserId == uid)
            { conflict = true; Model.StatusText = "Conflicting edits kept on this device. Open Resolve sync conflict."; }
        }
        catch (HouseholdException e)
        {
            failed=true;
            if (generation != currentGeneration || HouseholdService.UserId != uid) return;
            if (e.Code == "ABORTED") { conflict = true; Model.StatusText = "Conflicting edits kept on this device. Open Resolve sync conflict."; }
            else if (householdId != null && e.Code is "PERMISSION_DENIED" or "FAILED_PRECONDITION")
            { SelectPersonal(); Model.Load(true, null); Model.StatusText = "Household access ended. Your personal list is open."; }
            else Model.StatusText = "Saved on device · " + e.Message;
            if (manual) await DisplayAlert("Shopping list sync", Model.StatusText, AppLocalization.Text("OK"));
        }
        catch (Exception error)
        {
            failed=true;
            var reference=AppDiagnostics.Report(error,"shopping.sync","SYNC_FAILURE");
            if (generation == currentGeneration) Model.StatusText = "Saved on device · Cloud unavailable. Will retry in a minute while this screen is open.\nReference: "+reference;
            if (manual) await DisplayAlert("Sync unavailable", "Your changes are saved on this device. Check your connection and try syncing again.", AppLocalization.Text("OK"));
        }
        finally
        {
            syncing=false;
            if(failed&&generation==currentGeneration){retryScope=key;retryAfter=DateTimeOffset.UtcNow.AddMinutes(1);}
            else if(!failed)retryScope=null;
        }
    }
    private void SelectNamedPersonal(string id)
    {
        SelectPersonal();personalId=id;
        Model.SelectList(id=="main"?null:PersonalShoppingLists.StorageKey(id));
    }
    private void SelectHousehold(HouseholdInfo info)
    {
        sectionDrag?.Cancel();itemDrag?.Cancel();
        generation++;conflict=false;householdId=info.HouseholdId;sharedName=info.Name;listAccount=HouseholdService.UserId;
        Model.SelectList(CacheKey);
    }
    private void RenderListTabs()
    {
        ListTabs.Clear();
        var account=AccountSession.UserId;
        List<PersonalShoppingList> personalLists;
        try {personalLists=PersonalShoppingLists.All();}
        catch {ListSelector.Text="List options unavailable";ListVisibility.Text="List names couldn't load. Saved data is kept.";return;}
        void Tab(string text,bool selected,Func<Task> action)
        {
            var button=ShoppingUi.Button((selected?"✓ ":"")+text,async()=>{AccountSession.Check(account);if(Model.IsEditing)throw new InvalidOperationException("Save or cancel your item edit first.");await action();},!selected);
            SemanticProperties.SetDescription(button,text+(selected?", selected":""));
            ListTabs.Add(button);
        }
        async Task SwitchPersonal(string id){Model.Persist();SelectNamedPersonal(id);Model.Load(true,null);RenderListTabs();await SyncAsync();}
        if(availableHousehold is {FamilyEnabled:true,HouseholdId:not null})
            Tab("⌂ "+availableHousehold.Name,householdId==availableHousehold.HouseholdId,async()=>{Model.Persist();SelectHousehold(availableHousehold);Model.Load(true,null);RenderListTabs();await SyncAsync();});
        else Tab("⌂ Household",false,()=>ChooseHousehold());
        foreach(var list in personalLists)Tab(list.Name,householdId==null&&personalId==list.Id,()=>SwitchPersonal(list.Id));
        Tab("＋ New list",false,CreatePersonalList);
        var current=householdId!=null?sharedName:personalLists.First(l=>l.Id==personalId).Name;
        ShoppingModeListName.Text=current;
        ListSelector.Text=current+" · Options ▾";
        ListVisibility.Text=householdId!=null?"Shared with your household":personalId=="main"?"Only you · Personal list":"Only you · Saved on this device";
        QuickAddEntry.Placeholder="Add to "+current;
    }
    private async Task CreatePersonalList()
    {
        var account=AccountSession.UserId;
        var premium=PremiumEntitlement.IsActive(PlanService.Cached,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if(PersonalShoppingLists.All().Count>=2&&!premium)
        {
            if(!await PremiumAccess.Require("More than two personal shopping lists"))return;
            AccountSession.Check(account);premium=true;
        }
        var name=await DisplayPromptAsync("New personal list","Additional lists are saved on this device. Free includes two personal lists; Premium lets you create more.",maxLength:40);
        if(name==null)return;AccountSession.Check(account);
        var created=PersonalShoppingLists.Create(name,premium);
        Model.Persist();SelectNamedPersonal(created.Id);Model.Load(true,null);RenderListTabs();
    }
    private async Task ChooseHousehold()
    {
        if(AccountSession.UserId==null){await PremiumAccess.Require("Shared household shopping");return;}
        var account=AccountSession.UserId;var info=await HouseholdService.StatusAsync();AccountSession.Check(account);
        if(info.HouseholdId==null){await AppNavigation.Open(new HouseholdPage());return;}
        if(!info.FamilyEnabled){await AppNavigation.Open(new UpgradeFeaturesPage());return;}
        availableHousehold=info;Model.Persist();SelectHousehold(info);Model.Load(true,null);RenderListTabs();await SyncAsync();
    }
    private async void ListSelector_Clicked(object sender, EventArgs e)
    {
        try
        {
            if(Model.IsEditing)return;
            var account=AccountSession.UserId;
            var choice=await DisplayActionSheet("List options","Cancel",null,"New personal list","Rename personal list","Delete personal list","Manage household");
            AccountSession.Check(account);
            if(choice=="New personal list")await CreatePersonalList();
            else if(choice=="Manage household")await AppNavigation.Open(new HouseholdPage());
            else if(choice=="Rename personal list"||choice=="Delete personal list")
            {
                if(householdId!=null){await DisplayAlert("Household list","Manage shared household details from Account → Household. Select a personal list to rename or delete it.","OK");return;}
                if(choice=="Rename personal list")
                {
                    var name=await DisplayPromptAsync("Rename list","Choose a name",initialValue:PersonalShoppingLists.All().First(l=>l.Id==personalId).Name,maxLength:40);
                    AccountSession.Check(account);if(name==null)return;PersonalShoppingLists.Rename(personalId,name);
                }
                else
                {
                    if(personalId=="main"){await DisplayAlert("Keep your main list","You can rename or clear your main personal list, but it cannot be deleted.","OK");return;}
                    if(!await DisplayAlert("Delete this list?","Permanently remove this personal list and its items from this device? This cannot be undone.","Delete list","Keep"))return;
                    AccountSession.Check(account);PersonalShoppingLists.Delete(personalId);SelectPersonal();Model.Load(true,null);
                }
                RenderListTabs();
            }
        }
        catch(Exception error){await DisplayAlert("Shopping lists",error is InvalidOperationException?error.Message:"Couldn't change lists. Your saved lists are kept.","OK");}
    }
    private async void Widget_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new WidgetHelpPage());
    private async void Creator_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new ShoppingListMakerPage());
    private async void Household_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new HouseholdPage());
    private async void Sync_Clicked(object sender, EventArgs e) => await SyncAsync(true);
    private async void Resolve_Clicked(object sender, EventArgs e)
    {
        if(personalId!="main"&&householdId==null){await DisplayAlert("Device list","This personal list is stored on this device and has no cloud conflict to resolve.","OK");return;}
        if (!conflict)
        {
            var recovery = Preferences.Get(CacheKey + ".recovery", "");
            if (string.IsNullOrEmpty(recovery)) { await DisplayAlert("Sync", "No conflict needs resolving and no recovery copy is stored for this list.", AppLocalization.Text("OK")); return; }
            if (await DisplayAlert("Recovery copy available", "Share the device version kept before the last conflict was resolved?", "Share copy", AppLocalization.Text("Cancel")))
                await Share.Default.RequestAsync(new ShareTextRequest { Title = "Shopping list recovery", Text = ShoppingListDocument.Deserialize(recovery).PlainText() });
            return;
        }
        var choice = await DisplayActionSheet("Keep a copy before reloading", "Cancel", null, "Share my device version", "Reload latest cloud list");
        if (choice == "Share my device version") { Model.ShareCommand.Execute(null); return; }
        if (choice != "Reload latest cloud list" || !await DisplayAlert("Reload cloud list?", "Your device version will be kept as a recovery copy. The screen will load the latest cloud version. You can then add your changes again.", "Reload", AppLocalization.Text("Cancel"))) return;
        try
        {
            var version = generation; var uid = listAccount; var key = CacheKey;
            var result = await HouseholdService.CallAsync<ShoppingSyncResult>(new { action = "read", scope = householdId == null ? "personal" : "household", householdId },bypassReadCache:true);
            if (version != generation || uid != HouseholdService.UserId) return;
            Preferences.Set(key + ".recovery", Model.CurrentJson);
            var server = JsonSerializer.Serialize(result.Items); Preferences.Set(key + ".synced", server);
            Model.AcceptRemote(server); conflict = false; Model.StatusText = "Latest list loaded · Device recovery copy kept";
        }
        catch (Exception) { await DisplayAlert("Unable to reload", "Your device version is still saved. Please try again when connected.", AppLocalization.Text("OK")); }
    }
    private void WidgetChanged(string key) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (!visible || listAccount != HouseholdService.UserId || key != CacheKey) return;
        generation++;
        Model.AcceptRemote(Preferences.Get(key, Model.CurrentJson));
    });
    protected override void OnDisappearing()
    {
        openingDefault=true;
        WidgetShoppingStore.Changed -= WidgetChanged;
        SetShoppingMode(false);
        _ = SyncAsync(); // Flush pending online edits when leaving; failures remain saved for retry.
        visible = false; timer?.Stop();
        // Never persist one account's visible list into another account's storage.
        if (listAccount == HouseholdService.UserId) Model.Persist();
        sectionDrag?.Cancel();itemDrag?.Cancel();
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
