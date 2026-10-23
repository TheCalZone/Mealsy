#if MEALSY_ADS
using RecipeApp.Services;
using Xamarin.Google.UserMesssagingPlatform;

namespace RecipeApp.Platforms.Android;

public static class AdConsent
{
    private static readonly SemaphoreSlim gate = new(1, 1);
    private static bool declined;
    public static event Action ChoicesChanged;
    private static string AgeKey => "Ads.ageGroup." + (HouseholdService.UserId ?? "device");
    public static bool UnderAge => Preferences.Get(AgeKey, "") != "16 or over";
    public static bool Child => Preferences.Get(AgeKey, "") == "Under 13";
    public static async Task<bool> CanRequest()
    {
        if (declined) return false;
        await gate.WaitAsync();
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null) return false;
            if (string.IsNullOrEmpty(Preferences.Get(AgeKey, "")))
            {
                var age = await Shell.Current.DisplayActionSheet("Choose an age group for ad privacy", "Not now", null, "Under 13", "13–15", "16 or over");
                if (age is not ("Under 13" or "13–15" or "16 or over")) { declined = true; return false; }
                Preferences.Set(AgeKey, age);
            }
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var information = UserMessagingPlatform.GetConsentInformation(activity);
            information.RequestConsentInfoUpdate(activity, new ConsentRequestParameters.Builder().SetTagForUnderAgeOfConsent(UnderAge).Build(),
                new Success(() => UserMessagingPlatform.LoadAndShowConsentFormIfRequired(activity,
                    new Dismissed(error => completion.TrySetResult(error == null && information.CanRequestAds())))),
                new Failure(_ => completion.TrySetResult(false)));
            return await completion.Task.WaitAsync(TimeSpan.FromMinutes(3));
        }
        catch (Exception) { return false; } // No consent result means no ad request.
        finally { gate.Release(); }
    }
    public static async Task PrivacyChoices()
    {
        var choice = await Shell.Current.DisplayActionSheet("Advertising privacy", "Cancel", null, "Change age group", "Review consent choices");
        if (choice == "Change age group") { Preferences.Set(AgeKey, ""); declined = false; await CanRequest(); ChoicesChanged?.Invoke(); return; }
        if (choice != "Review consent choices") return;
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        UserMessagingPlatform.ShowPrivacyOptionsForm(Platform.CurrentActivity, new Dismissed(error => done.TrySetResult(error == null)));
        var success = await done.Task.WaitAsync(TimeSpan.FromMinutes(3));
        ChoicesChanged?.Invoke();
        if (!success)
            await Shell.Current.DisplayAlert("Ad privacy", "No consent form is available for your current region or age group. Advertising stays subject to your existing choices.", "OK");
    }
    private sealed class Success(Action action) : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateSuccessListener
    { public void OnConsentInfoUpdateSuccess() => action(); }
    private sealed class Failure(Action<FormError> action) : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateFailureListener
    { public void OnConsentInfoUpdateFailure(FormError error) => action(error); }
    private sealed class Dismissed(Action<FormError> action) : Java.Lang.Object, IConsentFormOnConsentFormDismissedListener
    { public void OnConsentFormDismissed(FormError error) => action(error); }
}

#endif
