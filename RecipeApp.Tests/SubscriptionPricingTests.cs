using System.Globalization;
using RecipeApp.Services;
using Xunit;

public class SubscriptionPricingTests
{
    [Fact]
    public void UkLabelsDoNotUseDeviceDecimalSeparatorsAndSavingsMatchBillingPeriods()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("£5.99/month or £49.99/year", SubscriptionPricing.Summary);
            Assert.Contains("£4.17/month", SubscriptionPricing.AnnualDetails);
            Assert.Contains("£21.89/year", SubscriptionPricing.AnnualDetails);
            Assert.Equal("16 extra · £15.84/month UK", SubscriptionPricing.ExtraPlaces(16));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void ExtraPlacesMustMatchSupportedStoreTiers(int count)
        => Assert.Throws<ArgumentOutOfRangeException>(() => SubscriptionPricing.ExtraPlaces(count));
}

public class PremiumEntitlementTests
{
    [Theory]
    [InlineData(true,false,true,2000,true)]
    [InlineData(false,true,true,2000,true)]
    [InlineData(false,false,true,2000,false)]
    [InlineData(true,false,true,1000,false)]
    [InlineData(true,false,false,2000,false)]
    public void PersonalSubscriptionsAndCoveredMembersUnlockPremiumUntilExpiry(bool own,bool household,bool adFree,long expires,bool expected)
        => Assert.Equal(expected,PremiumEntitlement.IsActive(new PlanStatus(own,household,adFree,expires,false,"mealsy_family","monthly"),1000));
    [Fact]
    public void MissingPlanDoesNotUnlockPremium()=>Assert.False(PremiumEntitlement.IsActive(null,1000));
}
