using Plugin.Firebase.Auth;
using RecipeApp.Services;
namespace RecipeApp;
public partial class App : Application
{
    public static bool IsForeground{get;private set;}=true;
    public static bool InitialAccountReady{get;private set;}
    public App(){
        var language=AppLocalization.Resolve(AppSettings.Current.Language,System.Globalization.CultureInfo.CurrentUICulture);
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture=language;
        System.Globalization.CultureInfo.CurrentUICulture=language;
        InitializeComponent();
#if ANDROID
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(this,Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
        PageAppearing+=(_,page)=>RecipeApp.Views.PageOptions.Configure(page);ThemeService.Initialize();ShoppingListStore.AccountId=()=>AccountSession.UserId;MainPage=new AppShell();}
    protected override async void OnStart()
    {
        base.OnStart();
        try{await RecipeService.GetRecipesForLocal();await RecipeService.GetSnacksForLocal();await RecipeService.GetStaplesForLocal();}
        catch(Exception){await MainPage.DisplayAlert("Local data unavailable","Your local data couldn't be loaded. Restart the app before editing it.","OK");}
        finally{AccountSession.ClearOldPassword();InitialAccountReady=true;}
        // Firebase's native SDK restores its own session; passwords are never stored/replayed by Mealsy.
        if(AccountSession.UserId!=null){await HouseholdWarmup.Start();await AccountSession.SyncSafely();}
    }
    protected override void OnResume(){base.OnResume();IsForeground=true;_=HouseholdWarmup.Start();}
    protected override void OnSleep(){IsForeground=false;base.OnSleep();}
}
