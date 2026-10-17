namespace RecipeApp.Services;
public static class OnboardingState
{
    public static bool Completed => Preferences.Get("Mealsy.onboarding.v1","")=="done";
    public static void Complete()=>Preferences.Set("Mealsy.onboarding.v1","done");
    public static string PendingCode=>Preferences.Get("Mealsy.invitation.pending","");
    public static void Remember(string value)=>Preferences.Set("Mealsy.invitation.pending",InvitationLinks.Code(value));
    public static void ClearCode()=>Preferences.Remove("Mealsy.invitation.pending");
    public static Task ShareApp()=>Share.Default.RequestAsync(new ShareTextRequest{Title="Try Mealsy",Text="Plan meals, save recipes and make shopping easier with Mealsy. Get the Android app and get started here:\n"+InvitationLinks.DownloadPage+"\n\nYour personal recipes, shopping lists and basic meal planning are free. Premium is optional. Google Play availability depends on the release or test you're invited to."});
}
