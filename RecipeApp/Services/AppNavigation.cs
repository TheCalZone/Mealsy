using RecipeApp.Views;
namespace RecipeApp.Services;
public static class AppNavigation
{
    private static readonly SemaphoreSlim navigation=new(1,1);
    public static void DismissKeyboard()
    {
#if ANDROID
        var activity=Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var focus=activity?.CurrentFocus;
        if(focus!=null){(activity.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager)?.HideSoftInputFromWindow(focus.WindowToken,Android.Views.InputMethods.HideSoftInputFlags.None);focus.ClearFocus();}
#elif IOS
        foreach(var scene in UIKit.UIApplication.SharedApplication.ConnectedScenes.OfType<UIKit.UIWindowScene>())
            foreach(var window in scene.Windows) window.EndEditing(true);
#endif
    }
    public static async Task Root(string route)
    {
        DismissKeyboard();
        if(!NavigationCatalog.Roots.Contains(route))throw new InvalidOperationException("That destination isn't available.");
        if(!await navigation.WaitAsync(0))return;try{await Shell.Current.GoToAsync("//"+route);await Shell.Current.Navigation.PopToRootAsync(false);}finally{navigation.Release();}
    }
    public static async Task Open(Page page)
    {
        DismissKeyboard();
        if(!await navigation.WaitAsync(0))return;try{await Shell.Current.Navigation.PushAsync(page);}finally{navigation.Release();}
    }
    public static Task Recipe(long id)=>Open(new RecipeDetailsPage{RecipeId=id.ToString()});
    public static async Task ShoppingList(string route="//ShoppingListPage"){
        if(route!="//ShoppingListPage"&&!route.StartsWith("//ShoppingListPage?",StringComparison.Ordinal))throw new InvalidOperationException("Invalid shopping destination.");
        DismissKeyboard();await Shell.Current.GoToAsync(route);await Shell.Current.Navigation.PopToRootAsync(false);
    }
    public static Task Back()=>Shell.Current.Navigation.NavigationStack.Count>1?Shell.Current.Navigation.PopAsync():Root("Homepage");
}
