using Android.BillingClient.Api;
using System.Security.Cryptography;
using System.Text;
using RecipeApp.Services;
using NativeBilling = Android.BillingClient.Api.BillingClient;

namespace RecipeApp.Platforms.Android;

public sealed class PlayBillingService : Java.Lang.Object, IPurchasesUpdatedListener
{
    public static PlayBillingService Instance { get; } = new();
    private NativeBilling client;
    private TaskCompletionSource<string> pending;
    private string pendingProduct=MonetizationConfig.ProductId;
    private readonly SemaphoreSlim connection = new(1, 1);
    public string Price { get; private set; }
    private async Task Connect()
    {
        await connection.WaitAsync();
        try
        {
            if (client?.IsReady == true) return;
            client?.EndConnection();
            client = NativeBilling.NewBuilder(global::Android.App.Application.Context).SetListener(this)
                .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
                .EnableAutoServiceReconnection().Build();
            Check(await client.StartConnectionAsync().WaitAsync(TimeSpan.FromSeconds(30)));
        }
        finally { connection.Release(); }
    }
    private async Task<(ProductDetails Product,ProductDetails.SubscriptionOfferDetails Offer)> LoadOffer(string basePlanId,string productId)
    {
        await Connect();
        var query = QueryProductDetailsParams.NewBuilder().SetProductList(new[] {
            QueryProductDetailsParams.Product.NewBuilder().SetProductId(productId).SetProductType("subs").Build() }).Build();
        var response = await client.QueryProductDetailsAsync(query).WaitAsync(TimeSpan.FromSeconds(30));
        var product = response.ProductDetailsList.FirstOrDefault(p => p.ProductId == productId);
        var seats=productId=="mealsy_extra_places"&&System.Text.RegularExpressions.Regex.IsMatch(basePlanId,@"^places-([1-9]|1[0-6])$");
        if ((!seats&&basePlanId is not (MonetizationConfig.BasePlanId or MonetizationConfig.MonthlyBasePlanId)) || product==null) throw new InvalidOperationException("That plan is not available from the app store yet.");
        var offers=product.GetSubscriptionOfferDetails()?.Where(o=>o.BasePlanId==basePlanId).ToList()??new();
        var quote=StoreSubscriptionOffer.Select(offers.Select(o=>new StoreOffer(o.OfferId,o.OfferToken,
            o.PricingPhases.PricingPhaseList.Select(p=>new StorePricePhase(p.PriceAmountMicros,p.FormattedPrice,p.PriceCurrencyCode,p.BillingPeriod,(int)p.RecurrenceMode,p.BillingCycleCount)).ToList())),
            seats||basePlanId==MonetizationConfig.MonthlyBasePlanId?"P1M":"P1Y",productId==MonetizationConfig.ProductId?MonetizationConfig.TrialOfferId:null);
        return (product,offers.Single(o=>o.OfferToken==quote.Token));
    }
    private static SubscriptionQuote Quote(ProductDetails.SubscriptionOfferDetails offer)
    {
        var phases=offer.PricingPhases.PricingPhaseList;var last=phases.Last();
        return new(offer.OfferToken,last.FormattedPrice,last.BillingPeriod,phases.Count==2&&phases[0].PriceAmountMicros==0&&phases[0].BillingPeriod=="P7D",last.PriceAmountMicros,last.PriceCurrencyCode);
    }
    public async Task<SubscriptionQuote> LoadQuote(string basePlanId)
    {
        var offer=await LoadOffer(basePlanId,MonetizationConfig.ProductId);
        return Quote(offer.Offer);
    }
    public async Task<string> LoadPrice(string basePlanId = MonetizationConfig.BasePlanId,string productId=MonetizationConfig.ProductId)
    {
        var details=await LoadOffer(basePlanId,productId);
        return Price=details.Offer.PricingPhases.PricingPhaseList.Last().FormattedPrice;
    }
    public async Task Buy(string basePlanId = MonetizationConfig.BasePlanId,string productId=MonetizationConfig.ProductId,SubscriptionQuote expected=null)
    {
        var uid = HouseholdService.UserId ?? throw new InvalidOperationException("Sign in before subscribing.");
        if (pending != null) throw new InvalidOperationException("A purchase is already in progress.");
        pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingProduct=productId;
        try
        {
            var selected=await LoadOffer(basePlanId,productId);
            if (uid != HouseholdService.UserId) throw new InvalidOperationException("Your account changed. Please try again.");
            if(expected!=null&&expected!=Quote(selected.Offer))throw new InvalidOperationException("Your store offer or price changed. Refresh plans and review the updated terms before subscribing.");
            var parameters = BillingFlowParams.NewBuilder().SetObfuscatedAccountId(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant())
                .SetProductDetailsParamsList(new[] { BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(selected.Product).SetOfferToken(selected.Offer.OfferToken).Build() }).Build();
            var result = client.LaunchBillingFlow(Platform.CurrentActivity, parameters); Check(result);
            var token = await pending.Task.WaitAsync(TimeSpan.FromMinutes(5));
            if (uid != HouseholdService.UserId) throw new InvalidOperationException("Sign in with the account used to subscribe, then restore purchases.");
            await PlanService.RefreshAsync(token);
        }
        finally { pending = null; }
    }
    public async Task Restore()
    {
        var uid = HouseholdService.UserId ?? throw new InvalidOperationException("Sign in before restoring purchases.");
        await Connect();
        var result = await client.QueryPurchasesAsync(QueryPurchasesParams.NewBuilder().SetProductType("subs").Build()).WaitAsync(TimeSpan.FromSeconds(30));
        Check(result.Result);
        foreach (var purchase in result.Purchases.Where(p => p.Products.Contains(MonetizationConfig.ProductId)||p.Products.Contains("mealsy_extra_places")))
        {
            if (uid != HouseholdService.UserId) throw new InvalidOperationException("Your account changed.");
            if ((int)purchase.PurchaseState == 1) await PlanService.RefreshAsync(purchase.PurchaseToken);
        }
        await PlanService.RefreshAsync();
    }
    public async Task BuyPromotionCredits(string businessId,Func<string,Task> verify)
    {
        var uid=HouseholdService.UserId??throw new InvalidOperationException("Sign in before buying credits.");
        if(pending!=null)throw new InvalidOperationException("A purchase is already in progress.");
        pending=new(TaskCreationOptions.RunContinuationsAsynchronously);pendingProduct="mealsy_promotion_credit_5";
        try{
            await Connect();var response=await client.QueryProductDetailsAsync(QueryProductDetailsParams.NewBuilder().SetProductList(new[]{QueryProductDetailsParams.Product.NewBuilder().SetProductId(pendingProduct).SetProductType("inapp").Build()}).Build()).WaitAsync(TimeSpan.FromSeconds(30));
            var credits=response.ProductDetailsList.FirstOrDefault(p=>p.ProductId==pendingProduct)??throw new InvalidOperationException("Promotion credits aren't available in the app store yet.");
            if(uid!=HouseholdService.UserId)throw new InvalidOperationException("Your account changed.");
            var parameters=BillingFlowParams.NewBuilder().SetObfuscatedAccountId(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant()).SetObfuscatedProfileId(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(businessId))).ToLowerInvariant()).SetProductDetailsParamsList(new[]{BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(credits).Build()}).Build();
            Check(client.LaunchBillingFlow(Platform.CurrentActivity,parameters));var token=await pending.Task.WaitAsync(TimeSpan.FromMinutes(5));AccountSession.Check(uid);await verify(token);
        }finally{pending=null;}
    }
    public async Task RestorePromotionCredits(Func<string,string,Task> verify)
    {
        var uid=HouseholdService.UserId??throw new InvalidOperationException("Sign in before restoring credits.");await Connect();
        var result=await client.QueryPurchasesAsync(QueryPurchasesParams.NewBuilder().SetProductType("inapp").Build()).WaitAsync(TimeSpan.FromSeconds(30));Check(result.Result);
        foreach(var p in result.Purchases.Where(p=>p.Products.Contains("mealsy_promotion_credit_5")&&(int)p.PurchaseState==1)){
            AccountSession.Check(uid);await verify(p.PurchaseToken,p.AccountIdentifiers?.ObfuscatedProfileId);
        }
    }
    public void OnPurchasesUpdated(BillingResult result, IList<Purchase> purchases)
    {
        var completion = pending;
        if (completion == null) return; // Restore handles completed/pending purchases after app restarts.
        if ((int)result.ResponseCode == 1) { completion.TrySetException(new InvalidOperationException("Purchase cancelled. No plan was activated.")); return; }
        if ((int)result.ResponseCode != 0) { completion.TrySetException(new InvalidOperationException("The app store could not complete the purchase. Try restoring purchases if you already paid.")); return; }
        var purchase = purchases?.FirstOrDefault(p => p.Products.Contains(pendingProduct));
        if (purchase == null) completion.TrySetException(new InvalidOperationException("No subscription was returned. Try Restore purchases."));
        else if ((int)purchase.PurchaseState != 1) completion.TrySetException(new InvalidOperationException("Payment is pending. Premium access starts after the app store confirms payment. Restore purchases after confirmation."));
        else completion.TrySetResult(purchase.PurchaseToken);
    }
    private static void Check(BillingResult result)
    {
        if ((int)result.ResponseCode != 0) throw new InvalidOperationException("Store billing is unavailable. Install the app from the app store and try again.");
    }
}
