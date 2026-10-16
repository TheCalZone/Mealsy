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
    private ProductDetails product;
    private ProductDetails.SubscriptionOfferDetails offer;
    private TaskCompletionSource<string> pending;
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
    public async Task<string> LoadPrice()
    {
        await Connect();
        var query = QueryProductDetailsParams.NewBuilder().SetProductList(new[] {
            QueryProductDetailsParams.Product.NewBuilder().SetProductId(MonetizationConfig.ProductId).SetProductType("subs").Build() }).Build();
        var response = await client.QueryProductDetailsAsync(query).WaitAsync(TimeSpan.FromSeconds(30));
        product = response.ProductDetailsList.FirstOrDefault(p => p.ProductId == MonetizationConfig.ProductId);
        offer = product?.GetSubscriptionOfferDetails()?.FirstOrDefault(o => o.BasePlanId == MonetizationConfig.BasePlanId && o.OfferId == null);
        var phase = offer?.PricingPhases.PricingPhaseList.LastOrDefault();
        if (phase == null || phase.BillingPeriod != "P1Y") throw new InvalidOperationException("The annual plan is not available from Google Play yet.");
        return Price = phase.FormattedPrice;
    }
    public async Task Buy()
    {
        var uid = HouseholdService.UserId ?? throw new InvalidOperationException("Sign in before subscribing.");
        if (pending != null) throw new InvalidOperationException("A purchase is already in progress.");
        pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await LoadPrice();
            if (uid != HouseholdService.UserId) throw new InvalidOperationException("Your account changed. Please try again.");
            var parameters = BillingFlowParams.NewBuilder().SetObfuscatedAccountId(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant())
                .SetProductDetailsParamsList(new[] { BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(product).SetOfferToken(offer.OfferToken).Build() }).Build();
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
        foreach (var purchase in result.Purchases.Where(p => p.Products.Contains(MonetizationConfig.ProductId)))
        {
            if (uid != HouseholdService.UserId) throw new InvalidOperationException("Your account changed.");
            if ((int)purchase.PurchaseState == 1) await PlanService.RefreshAsync(purchase.PurchaseToken);
        }
        await PlanService.RefreshAsync();
    }
    public void OnPurchasesUpdated(BillingResult result, IList<Purchase> purchases)
    {
        var completion = pending;
        if (completion == null) return; // Restore handles completed/pending purchases after app restarts.
        if ((int)result.ResponseCode == 1) { completion.TrySetException(new InvalidOperationException("Purchase cancelled. No plan was activated.")); return; }
        if ((int)result.ResponseCode != 0) { completion.TrySetException(new InvalidOperationException("Google Play could not complete the purchase. Try restoring purchases if you already paid.")); return; }
        var purchase = purchases?.FirstOrDefault(p => p.Products.Contains(MonetizationConfig.ProductId));
        if (purchase == null) completion.TrySetException(new InvalidOperationException("No subscription was returned. Try Restore purchases."));
        else if ((int)purchase.PurchaseState != 1) completion.TrySetException(new InvalidOperationException("Payment is pending. Family access starts after Google Play confirms payment. Restore purchases after confirmation."));
        else completion.TrySetResult(purchase.PurchaseToken);
    }
    private static void Check(BillingResult result)
    {
        if ((int)result.ResponseCode != 0) throw new InvalidOperationException("Google Play Billing is unavailable. Install the app from Google Play and try again.");
    }
}
