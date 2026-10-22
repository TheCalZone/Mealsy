using RecipeApp.Services;
using Xunit;

public class StoreSubscriptionOfferTests
{
    private static StorePricePhase Paid(string period="P1M")=>new(4990000,"£4.99","GBP",period,1,0);
    private static StoreOffer Standard()=>new(null,"base",new[]{Paid()});
    private static StoreOffer Trial(string period="P7D",int cycles=1,int recurrence=2)=>new("seven-day-trial","trial",new[]{new StorePricePhase(0,"£0.00","GBP",period,recurrence,cycles),Paid()});
    [Fact] public void ChoosesEligibleConfiguredTrialOverBase()
    {
        var quote=StoreSubscriptionOffer.Select(new[]{Standard(),Trial()},"P1M","seven-day-trial");
        Assert.True(quote.SevenDayTrial);Assert.Equal("trial",quote.Token);
        Assert.Contains("£4.99 per month",quote.Disclosure);Assert.Contains("Cancel",quote.Disclosure);
    }
    [Fact] public void MissingTrialFallsBackToNormalPrice()
    {
        var quote=StoreSubscriptionOffer.Select(new[]{Standard()},"P1M","seven-day-trial");
        Assert.False(quote.SevenDayTrial);Assert.Equal("£4.99 per month",quote.DisplayPrice);
    }
    [Theory]
    [InlineData("P3D",1,2)] [InlineData("P7D",2,2)] [InlineData("P7D",1,1)]
    public void RejectsUnexpectedTrialStructure(string period,int cycles,int recurrence)
        =>Assert.False(StoreSubscriptionOffer.Select(new[]{Standard(),Trial(period,cycles,recurrence)},"P1M","seven-day-trial").SevenDayTrial);
    [Fact] public void DoesNotUseUnconfiguredOffers()
        =>Assert.False(StoreSubscriptionOffer.Select(new[]{Standard(),Trial()},"P1M","other-offer").SevenDayTrial);
    [Fact] public void ExtraPlacesCannotGetPremiumTrial()
        =>Assert.False(StoreSubscriptionOffer.Select(new[]{Standard(),Trial()},"P1M").SevenDayTrial);
    [Fact] public void RejectsWrongRenewalPeriod()
        =>Assert.Throws<InvalidOperationException>(()=>StoreSubscriptionOffer.Select(new[]{Trial()},"P1Y","seven-day-trial"));
    [Fact] public void RejectsAdditionalDiscountPhase()
    {
        var trial=Trial() with {Phases=new[]{Trial().Phases[0],Paid() with {Recurrence=2,Cycles=2},Paid()}};
        Assert.False(StoreSubscriptionOffer.Select(new[]{Standard(),trial},"P1M","seven-day-trial").SevenDayTrial);
    }
    [Fact] public void RejectsCurrencyMismatch()
    {
        var trial=Trial() with {Phases=new[]{Trial().Phases[0] with {Currency="USD"},Paid()}};
        Assert.False(StoreSubscriptionOffer.Select(new[]{Standard(),trial},"P1M","seven-day-trial").SevenDayTrial);
    }
    [Fact] public void AnnualDisclosureMatchesRenewal()
    {
        var trial=Trial() with {Phases=new[]{Trial().Phases[0],Paid("P1Y") with {Price="£39.99"}}};
        Assert.Contains("£39.99 per year",StoreSubscriptionOffer.Select(new[]{trial},"P1Y","seven-day-trial").Disclosure);
    }
    [Fact] public void UnavailableStoreCannotAdvertiseTrial()
        =>Assert.Throws<InvalidOperationException>(()=>StoreSubscriptionOffer.Select(Array.Empty<StoreOffer>(),"P1M","seven-day-trial"));
}
