using Android.App;
using Button = Android.Widget.Button;
using ProgressBar = Android.Widget.ProgressBar;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;

namespace RecipeApp.Platforms.Android;

// RemoteViews cannot host an EditText. A dialog activity keeps the launcher behind
// the keyboard without opening the MAUI navigation stack.
[Activity(Exported=false, ExcludeFromRecents=true, ConfigurationChanges=global::Android.Content.PM.ConfigChanges.Orientation|global::Android.Content.PM.ConfigChanges.ScreenSize|global::Android.Content.PM.ConfigChanges.KeyboardHidden, Theme="@android:style/Theme.Material.Light.Dialog.NoActionBar", WindowSoftInputMode=SoftInput.AdjustResize)]
public class WidgetQuickAddActivity : Activity
{
    private EditText entry;
    private TextView status;
    private Button add;
    private ProgressBar progress;
    private WidgetListTarget target;
    private string account;
    private int widgetId;
    private Guid pendingId=Guid.NewGuid();
    private bool busy;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        var dark=AppSettings.Current.IsDark(ShoppingQuickAddWidget.SystemDark(this));
        SetTheme(dark?global::Android.Resource.Style.ThemeMaterialDialogNoActionBar:global::Android.Resource.Style.ThemeMaterialLightDialogNoActionBar);
        base.OnCreate(savedInstanceState);
        try { Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(this, () => this); }
        catch (Exception) { Toast.MakeText(this,"Open Mealsy once before using quick add.",ToastLength.Long).Show();Finish();return; }
        ShoppingListStore.AccountId=()=>AccountSession.UserId;
        widgetId=Intent?.GetIntExtra("mealsy_widget_id",-1)??-1;
        if(!ShoppingQuickAddWidget.IsValid(widgetId)){Finish();return;}
        account=AccountSession.UserId;
        target=WidgetShoppingStore.Target(widgetId);
        if(Guid.TryParse(savedInstanceState?.GetString("pendingId"),out var restored))pendingId=restored;
        var body=new LinearLayout(this){Orientation=Orientation.Vertical};
        var padding=(int)(20*Resources.DisplayMetrics.Density);body.SetPadding(padding,padding,padding,padding);
        var title=new TextView(this){Text="Mealsy · Quick add",TextSize=22};body.AddView(title);
        status=new TextView(this){Text=target?.BelongsTo(account)==true?target.Name:"Choose a shopping list for this widget first."};body.AddView(status);
        entry=new EditText(this){Hint="e.g. 500g minced beef",Text=savedInstanceState?.GetString("item")??"",InputType=global::Android.Text.InputTypes.ClassText|global::Android.Text.InputTypes.TextFlagCapSentences,ImeOptions=ImeAction.Done};
        entry.SetSingleLine(true);entry.SetFilters(new global::Android.Text.IInputFilter[]{new global::Android.Text.InputFilterLengthFilter(200)});body.AddView(entry);
        progress=new ProgressBar(this){Indeterminate=true,Visibility=ViewStates.Gone};body.AddView(progress,new LinearLayout.LayoutParams(40,40));
        var actions=new LinearLayout(this){Orientation=Orientation.Horizontal};
        var cancel=new Button(this){Text="Cancel"};cancel.Click+=(_,_)=>{if(!busy)Finish();};actions.AddView(cancel);
        add=new Button(this){Text=target?.BelongsTo(account)==true?"Add item":"Choose list"};actions.AddView(add);body.AddView(actions);
        add.Click+=async(_,_)=>await Save();
        entry.EditorAction+=async(_,e)=>{if(e.ActionId==ImeAction.Done){e.Handled=true;await Save();}};
        var theme=ThemeColors.For(AppSettings.Current,ShoppingQuickAddWidget.SystemDark(this));
        global::Android.Graphics.Color Color(string hex)=>global::Android.Graphics.Color.ParseColor(hex);
        body.SetBackgroundColor(Color(theme.Surface));title.SetTextColor(Color(theme.Text));status.SetTextColor(Color(theme.Muted));
        entry.SetTextColor(Color(theme.Text));entry.SetHintTextColor(Color(theme.Muted));entry.BackgroundTintList=global::Android.Content.Res.ColorStateList.ValueOf(Color(theme.Accent));
        add.BackgroundTintList=global::Android.Content.Res.ColorStateList.ValueOf(Color(theme.Accent));add.SetTextColor(global::Android.Graphics.Color.White);
        cancel.SetTextColor(Color(theme.Text));
        progress.IndeterminateTintList=global::Android.Content.Res.ColorStateList.ValueOf(Color(theme.Accent));
        Window.SetBackgroundDrawable(new global::Android.Graphics.Drawables.ColorDrawable(Color(theme.Surface)));
        SetContentView(body);
        Window.SetLayout(ViewGroup.LayoutParams.MatchParent,ViewGroup.LayoutParams.WrapContent);
        if(target?.BelongsTo(account)==true){entry.RequestFocus();Window.SetSoftInputMode(SoftInput.StateAlwaysVisible|SoftInput.AdjustResize);}else entry.Visibility=ViewStates.Gone;
    }
    protected override void OnSaveInstanceState(Bundle state)
    {
        state.PutString("item",entry?.Text??"");state.PutString("pendingId",pendingId.ToString());base.OnSaveInstanceState(state);
    }
    private void Check()
    {
        AccountSession.Check(account);
        if(!ShoppingQuickAddWidget.IsValid(widgetId)||WidgetShoppingStore.Target(widgetId)!=target)throw new InvalidOperationException("The widget selection changed. Close quick add and try again.");
    }
    private async Task Save()
    {
        if(busy)return;
        if(target?.BelongsTo(account)!=true)
        {
            var intent=new Intent(this,typeof(MainActivity));intent.PutExtra("mealsy_widget_id",widgetId);intent.PutExtra("mealsy_widget_configure",true);intent.AddFlags(ActivityFlags.ClearTop|ActivityFlags.SingleTop);StartActivity(intent);Finish();return;
        }
        if(string.IsNullOrWhiteSpace(entry.Text)){entry.Error="Enter an item name.";return;}
        busy=true;add.Enabled=false;entry.Enabled=false;progress.Visibility=ViewStates.Visible;
        var saved=false;var requested=false;
        try
        {
            Check();
            if(target.HouseholdId!=null)
            {
                status.Text="Checking household access…";
                var household=await HouseholdService.StatusAsync(true);Check();
                if(household.HouseholdId!=target.HouseholdId||!household.FamilyEnabled)throw new InvalidOperationException("Household access ended. Choose another list in the widget settings.");
            }
            Check();
            var item=new ShoppingListItem{Id=pendingId,Name=entry.Text.Trim(),Quantity="",Note="",Category="Other"};
            var sent=WidgetShoppingStore.Add(target,account,item);saved=true;
            IngredientSuggestionStore.Remember(account,item.Name);
            if(account!=null&&target.PersonalListId==null)
            {
                status.Text="Saved on this device · Syncing…";
                var baseline=Preferences.Get(target.StorageKey+".synced","[]");
                var changes=ShoppingListSync.Changes(baseline,sent);
                var result=await HouseholdService.CallAsync<ShoppingSyncResult>(new{action="patch",scope=target.HouseholdId==null?"personal":"household",householdId=target.HouseholdId,changes});
                Check();requested=result.RequestedCount>0;
                var server=JsonSerializer.Serialize(result.Items);
                var merged=ShoppingListSync.Reapply(sent,Preferences.Get(target.StorageKey,sent),server);
                Preferences.Set(target.StorageKey+".synced",server);
                if(target.HouseholdId==null)ShoppingListStore.Save(merged);else Preferences.Set(target.StorageKey,merged.Serialize());
                WidgetShoppingStore.NotifyChanged(target.StorageKey);
            }
            Toast.MakeText(this,requested?"Request sent · Waiting for approval":"Added to "+target.Name,ToastLength.Short).Show();Finish();
        }
        catch(Exception error)
        {
            if(saved){Toast.MakeText(this,"Saved on this device. Open the list to retry sync.",ToastLength.Long).Show();Finish();}
            else status.Text=error is InvalidOperationException or HouseholdException?error.Message:"Couldn't add the item. Check your connection and try again.";
        }
        finally{busy=false;if(!IsFinishing&&!IsDestroyed){add.Enabled=true;entry.Enabled=true;progress.Visibility=ViewStates.Gone;}}
    }
    public override void OnBackPressed(){if(!busy)base.OnBackPressed();}
}
