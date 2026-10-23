#if MEALSY_ADS
using Google.Android.Gms.Ads;
using Android.Widget;
using Android.Views;
using Microsoft.Maui.Handlers;
using RecipeApp.Controls;
using RecipeApp.Services;

namespace RecipeApp.Platforms.Android;

public class AdBannerHandler : ViewHandler<AdBanner, FrameLayout>
{
    private AdView ad;
    private bool connected;
    private bool active = true;
    private int generation;
    public AdBannerHandler() : base(ViewHandler.ViewMapper) { }
    protected override FrameLayout CreatePlatformView() => new(Context);
    protected override void ConnectHandler(FrameLayout platformView)
    {
        base.ConnectHandler(platformView); connected = true; PlanService.Changed += PlanChanged; AdConsent.ChoicesChanged += ConsentChanged; _ = Refresh();
    }
    protected override void DisconnectHandler(FrameLayout platformView)
    {
        connected = false; generation++; PlanService.Changed -= PlanChanged; AdConsent.ChoicesChanged -= ConsentChanged; Remove(); base.DisconnectHandler(platformView);
    }
    public void Pause() { active = false; generation++; Remove(); }
    public void Resume() { active = true; _ = Refresh(); }
    private void ConsentChanged() => MainThread.BeginInvokeOnMainThread(() => { generation++; Remove(); if (active) _ = Refresh(); });
    private void PlanChanged() => MainThread.BeginInvokeOnMainThread(() => { if (PlanService.AdFree) Remove(); });
    private void Remove()
    {
        ad?.Destroy(); ad?.Dispose(); ad = null; PlatformView?.RemoveAllViews(); if (VirtualView != null) VirtualView.HeightRequest = 0;
    }
    private async Task Refresh()
    {
        var version = ++generation;
        Remove();
        try
        {
#if !DEBUG
            if (!MonetizationConfig.LiveAdsConfigured || string.IsNullOrWhiteSpace(MonetizationConfig.PrivacyPolicyUrl)) return;
            var appId = Context.PackageManager.GetApplicationInfo(Context.PackageName, global::Android.Content.PM.PackageInfoFlags.MetaData).MetaData?.GetString("com.google.android.gms.ads.APPLICATION_ID");
            if (appId == "ca-app-pub-3940256099942544~3347511713" || string.IsNullOrWhiteSpace(MonetizationConfig.LiveBannerId)) return;
#endif
            if (HouseholdService.UserId != null)
            {
                try { await PlanService.RefreshAsync(); }
                catch (Exception) { if (!PlanService.AdFree) return; } // Avoid ads when a signed-in paid status is unknown.
            }
            if (!connected || !active || version != generation || PlanService.AdFree || !await AdConsent.CanRequest()) return;
            if (!connected || !active || version != generation || PlanService.AdFree) return;
            MobileAds.RequestConfiguration = new RequestConfiguration.Builder().SetTagForChildDirectedTreatment(AdConsent.Child ? 1 : 0)
                .SetTagForUnderAgeOfConsent(AdConsent.UnderAge ? 1 : 0).Build();
            MobileAds.Initialize(Context);
            ad = new AdView(Context) { AdSize = AdSize.Banner,
#if DEBUG
                AdUnitId = MonetizationConfig.TestBannerId
#else
                AdUnitId = MonetizationConfig.LiveBannerId
#endif
            };
            ad.AdListener = new Listener(() => { if (connected && version == generation && !PlanService.AdFree) VirtualView.HeightRequest = 70; else Remove(); });
            var label = new TextView(Context) { Text = "Advertisement", TextSize = 11, Gravity = GravityFlags.CenterHorizontal };
            PlatformView.AddView(label, new FrameLayout.LayoutParams(-1, -2) { Gravity = GravityFlags.Top });
            PlatformView.AddView(ad, new FrameLayout.LayoutParams(-2, -2) { Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal });
            var extras = new global::Android.OS.Bundle(); extras.PutString("npa", "1");
            var request = new AdRequest.Builder();
            request.AddNetworkExtrasBundle(Java.Lang.Class.ForName("com.google.ads.mediation.admob.AdMobAdapter"), extras);
            ad.LoadAd(request.Build());
        }
        catch (Exception) { Remove(); }
    }
    private sealed class Listener(Action loaded) : AdListener
    { public override void OnAdLoaded() => loaded(); }
}

#endif
