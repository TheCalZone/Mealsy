using RecipeApp.Services;
namespace RecipeApp.Views;
public record CampaignWallet(long BalanceMicro,long CpmMicro,bool CheckoutEnabled);
public class CampaignWalletPage:BusinessPage
{
    public const string Endpoint="https://us-central1-mealsy-b732b.cloudfunctions.net/promotions";
    private readonly string businessId;
    public CampaignWalletPage(string id):base("Promotion credits"){businessId=id;}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(Load);}
    private async Task Load(){
        var wallet=await HouseholdService.CallEndpointAsync<CampaignWallet>(Endpoint,new{action="wallet",businessId});Check();Reset("Your promotion balance");
        Body.Add(ShoppingUi.Text($"£{wallet.BalanceMicro/1000000m:0.00} available",25,true));Body.Add(ShoppingUi.Text("£1 per 1,000 qualifying impressions · 0.1p each. Impressions are capped at one per signed-in viewer per campaign per day. Team members' views aren't charged. Credits are prepaid; spending stops at your daily limit, total limit or available balance.",14));
        Body.Add(ShoppingUi.Text("An impression requires at least half the post visible for one continuous second on an app-verified device. Engagement counts in the dashboard are separate from charges.",13));
        if(!wallet.CheckoutEnabled)Body.Add(ShoppingUi.Text("Purchasing and live campaign delivery are awaiting store and device-verification configuration. Draft campaigns are available; no payment will be taken here.",14));
        else if(ClientPlatform.StorePurchasesAvailable)Body.Add(Button("Buy £5 promotion credit · store confirms local price",async()=>{
#if ANDROID
            await RecipeApp.Platforms.Android.PlayBillingService.Instance.BuyPromotionCredits(businessId,token=>HouseholdService.CallEndpointAsync<object>(Endpoint,new{action="verifyCredit",businessId,token}));await Load();
#else
            throw new InvalidOperationException("Credit purchases are not configured for this platform yet.");
#endif
        }));
        if(!ClientPlatform.StorePurchasesAvailable){Body.Add(ShoppingUi.Text("App Store credit purchases and device-verified paid campaign delivery are not available in this build. You can view your balance and manage drafts. No payment will be taken here.",14));return;}
        Body.Add(Button("Restore unfinished credit purchase",async()=>{
#if ANDROID
            var expected=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(businessId))).ToLowerInvariant();
            await RecipeApp.Platforms.Android.PlayBillingService.Instance.RestorePromotionCredits(async(token,profile)=>{if(profile==expected)await HouseholdService.CallEndpointAsync<object>(Endpoint,new{action="verifyCredit",businessId,token});});await Load();
#else
            throw new InvalidOperationException("Credit restore is not configured for this platform yet.");
#endif
        }));
    }
}
