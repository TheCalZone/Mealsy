using RecipeApp.Views;
namespace RecipeApp.Services;
public static class AppNavigation
{
    private static readonly SemaphoreSlim navigation=new(1,1);
    public static async Task Root(string route)
    {
        if(!NavigationCatalog.Roots.Contains(route))throw new InvalidOperationException("That destination isn't available.");
        if(!await navigation.WaitAsync(0))return;try{await Shell.Current.GoToAsync("//"+route);}finally{navigation.Release();}
    }
    public static async Task Open(Page page)
    {
        if(!await navigation.WaitAsync(0))return;try{await Shell.Current.Navigation.PushAsync(page);}finally{navigation.Release();}
    }
    public static Task Recipe(long id)=>Open(new RecipeDetailsPage{RecipeId=id.ToString()});
    public static Task Back()=>Shell.Current.Navigation.NavigationStack.Count>1?Shell.Current.Navigation.PopAsync():Root("Homepage");
}
