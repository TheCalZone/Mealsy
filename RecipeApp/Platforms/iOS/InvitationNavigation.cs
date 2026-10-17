using RecipeApp.Services;
using RecipeApp.Views;
namespace RecipeApp.Platforms.iOS;

internal static class InvitationNavigation
{
    private static bool opening;
    public static bool Handle(string raw)
    {
        if(string.IsNullOrWhiteSpace(raw)||raw.Length>2000)return false;
        string code=null;
        try
        {
            if(raw.StartsWith("mealsy://join",StringComparison.OrdinalIgnoreCase)){code=InvitationLinks.Code(raw);OnboardingState.Remember(code);}
            else if(raw!="mealsy://welcome")return false;
        }
        catch(InvalidOperationException){return false;}
        if(!opening)_=Open(code);
        return true;
    }
    private static async Task Open(string code)
    {
        opening=true;
        try
        {
            for(var i=0;i<200&&(!App.InitialAccountReady||Shell.Current==null);i++)await Task.Delay(100);
            if(!App.InitialAccountReady||Shell.Current==null)return;
            await MainThread.InvokeOnMainThreadAsync(()=>AppNavigation.Open(new OnboardingPage(code,startAt:code==null?0:2)));
        }
        catch(Exception error){AppDiagnostics.Report(error,"navigation.invitation","OPEN_FAILURE");}
        finally{opening=false;}
    }
}
