using Plugin.Firebase.Auth;
using RecipeApp.Services;
namespace RecipeApp;
public partial class App : Application
{
    public static bool InitialAccountReady{get;private set;}
    public App(){InitializeComponent();ThemeService.Initialize();ShoppingListStore.AccountId=()=>AccountSession.UserId;MainPage=new AppShell();}
    protected override async void OnStart()
    {
        base.OnStart();
        try{await RecipeService.GetRecipesForLocal();await RecipeService.GetSnacksForLocal();await RecipeService.GetStaplesForLocal();}
        catch(Exception){await MainPage.DisplayAlert("Local data unavailable","Your local data couldn't be loaded. Restart the app before editing it.","OK");}
        finally{AccountSession.ClearOldPassword();InitialAccountReady=true;}
        // Firebase's native SDK restores its own session; passwords are never stored/replayed by Mealsy.
        if(AccountSession.UserId!=null)await AccountSession.SyncSafely();
    }
}
