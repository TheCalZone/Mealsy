using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
namespace RecipeApp.Views;

public class WidgetQuickAddPage : ContentPage
{
    public int WidgetId{get;}
    private readonly string account=HouseholdService.UserId;
    private readonly List<WidgetListTarget> targets=new();
    private readonly Picker picker=new(){Title="Choose a shopping list",HeightRequest=48};
    private readonly Label status=ShoppingUi.Text("Personal items save offline. Household quick-add checks your access online.",13);
    private readonly Entry name,quantity,note;private Guid pendingId=Guid.NewGuid();private bool busy;
    public WidgetQuickAddPage(int id,bool configure=false)
    {
        WidgetId=id;Title="Quick shopping add";ShoppingUi.Theme(this);
        name=ShoppingUi.Entry("","What do you need?",_=>{});quantity=ShoppingUi.Entry("","Quantity · optional",_=>{});note=ShoppingUi.Entry("","Note · optional",_=>{},500);
        var saved=WidgetShoppingStore.Target(id);targets.Add(new(account,null,account==null ? "Device list" : "My personal list"));
        if(saved?.BelongsTo(account)==true&&saved.HouseholdId!=null)targets.Add(saved);
        picker.ItemsSource=targets.Select(t=>t.Name).ToList();picker.SelectedIndex=saved==null?0:saved.BelongsTo(account)?targets.FindIndex(t=>t.HouseholdId==saved.HouseholdId):-1;
        picker.SetDynamicResource(Picker.TextColorProperty, "ThemeText");
        if(saved!=null&&!saved.BelongsTo(account))status.Text="This widget was configured for another account. Explicitly choose a list for your current account.";
        var body=new VerticalStackLayout {Padding=20,Spacing=14,Children={ShoppingUi.Text("Add it before you forget",27,true),picker,
            ShoppingUi.Button("Find my household list",()=>Run(async()=>{Check();var household=await HouseholdService.StatusAsync();Check();if(household.HouseholdId==null)throw new InvalidOperationException("Create or join a household first.");if(!household.FamilyEnabled)throw new InvalidOperationException("Your household needs an active Family plan.");targets.RemoveAll(t=>t.HouseholdId!=null);targets.Add(new(account,household.HouseholdId,household.Name+" · household"));picker.ItemsSource=targets.Select(t=>t.Name).ToList();picker.SelectedIndex=targets.Count-1;}),true),
            ShoppingUi.Button("Remember this list for this widget",()=>Run(async()=>{var target=Selected();await Verify(target);Check();WidgetShoppingStore.SetTarget(WidgetId,target);UpdateWidget();status.Text="Widget list selected: "+target.Name;}),true),
            name,quantity,note,ShoppingUi.Button("Add item",()=>Run(Add)),status,
            ShoppingUi.Button("Open selected shopping list",()=>Run(async()=>{var target=Selected();await Verify(target);Check();await AppNavigation.ShoppingList(target.HouseholdId==null?"//ShoppingListPage?OpenPersonal=True":"//ShoppingListPage?OpenHouseholdId="+Uri.EscapeDataString(target.HouseholdId));}),true)
        }};Content=new ScrollView {Content=body};
        if(!configure)Dispatcher.Dispatch(()=>name.Focus());
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();if(account==HouseholdService.UserId)return;
        Content=ShoppingUi.Text("Your account changed. Reopen quick add.");
        await DisplayAlert("Account changed","Reopen the widget to choose a list for your current account.","OK");await Navigation.PopAsync();
    }
    private void Check(){if(account!=HouseholdService.UserId)throw new InvalidOperationException("Your account changed. Close quick add and reopen the widget.");}
    private WidgetListTarget Selected(){Check();if(picker.SelectedIndex<0)throw new InvalidOperationException("Choose a shopping list first.");return targets[picker.SelectedIndex];}
    private async Task Verify(WidgetListTarget target)
    {
        if(target.HouseholdId==null)return;var household=await HouseholdService.StatusAsync();Check();
        if(household.HouseholdId!=target.HouseholdId||!household.FamilyEnabled)throw new InvalidOperationException("Access to this household list has ended. Choose another list explicitly.");
    }
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;try{await action();}catch(Exception e){status.Text=e is InvalidOperationException or HouseholdException?e.Message:"Quick add couldn't finish. Check your connection and try again; your saved list was kept.";}finally{busy=false;}
    }
    private async Task Add()
    {
        var target=Selected();var item=new ShoppingListItem {Id=pendingId,Name=name.Text?.Trim()??"",Quantity=quantity.Text?.Trim()??"",Note=note.Text?.Trim()??"",Category="Other"};
        if(item.Name.Length==0)throw new InvalidOperationException("Enter an item name.");
        await Verify(target);Check();var sent=WidgetShoppingStore.Add(target,account,item);WidgetShoppingStore.SetTarget(WidgetId,target);UpdateWidget();pendingId=Guid.NewGuid();name.Text=quantity.Text=note.Text="";status.Text="Added to "+target.Name+" · Saved on this device";
        if(account==null)return;
        try
        {
            var baseline=Preferences.Get(target.StorageKey+".synced","[]");var changes=ShoppingListSync.Changes(baseline,sent);
            var result=await HouseholdService.CallAsync<ShoppingSyncResult>(new{action="patch",scope=target.HouseholdId==null?"personal":"household",householdId=target.HouseholdId,changes});Check();
            var server=JsonSerializer.Serialize(result.Items);var merged=ShoppingListSync.Reapply(sent,Preferences.Get(target.StorageKey,sent),server);
            Preferences.Set(target.StorageKey+".synced",server);if(target.HouseholdId==null)ShoppingListStore.Save(merged);else Preferences.Set(target.StorageKey,merged.Serialize());status.Text="Added and synced to "+target.Name;
        }
        catch(Exception){status.Text="Added on this device. Open the selected list to retry sync. Household conflicts may need resolving there.";}
        name.Focus();
    }
    private void UpdateWidget()
    {
#if ANDROID
        RecipeApp.Platforms.Android.ShoppingQuickAddWidget.Update(global::Android.App.Application.Context,WidgetId);
#endif
    }
}

public class WidgetHelpPage : ContentPage
{
    public WidgetHelpPage()
    {
        if(!ClientPlatform.HomeScreenWidgetAvailable){Title="Quick shopping";ShoppingUi.Theme(this);Content=new VerticalStackLayout{Padding=20,Spacing=16,Children={ShoppingUi.Text("Shopping on iPhone",26,true),ShoppingUi.Text("A home-screen widget isn't included in this iOS build. Add items from the Shopping tab, including offline personal items."),ShoppingUi.Button("Open shopping",()=>AppNavigation.Root("ShoppingListPage"))}};return;}
        Title="Home-screen quick add";ShoppingUi.Theme(this);
        var body=new VerticalStackLayout {Padding=20,Spacing=16,Children={ShoppingUi.Text("Catch a shopping thought",28,true),ShoppingUi.Text("Add the Mealsy quick-add widget to your Android home screen. Tap + Add item to open a focused entry screen, without finding the shopping tab first."),ShoppingUi.Text("1. Long-press an empty area on your home screen.\n2. Choose Widgets, then Mealsy quick add.\n3. Drag it onto your home screen.\n4. Tap Choose list to save a personal or household target."),ShoppingUi.Text("Each widget remembers its own selection. Household access needs an active Family plan and an online permission check; personal items can be saved offline. The widget shows no shopping items on your home screen. After switching accounts, explicitly choose a list again.",14)}};
        body.Add(ShoppingUi.Button("Add widget to home screen",async()=>{
#if ANDROID
            var context=global::Android.App.Application.Context;var manager=global::Android.Appwidget.AppWidgetManager.GetInstance(context);
            if(OperatingSystem.IsAndroidVersionAtLeast(26)&&manager.IsRequestPinAppWidgetSupported)
            {
                manager.RequestPinAppWidget(new global::Android.Content.ComponentName(context.PackageName,"com.companyname.recipeapp.ShoppingQuickAddWidget"),null,null);return;
            }
#endif
            await DisplayAlert("Add from your launcher","Long-press your home screen, choose Widgets, and look for Mealsy quick add.","OK");
        }));Content=new ScrollView {Content=body};
    }
}
