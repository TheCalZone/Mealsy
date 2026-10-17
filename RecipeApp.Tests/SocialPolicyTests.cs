using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;
public class SocialPolicyTests
{
    [Theory]
    [InlineData("friends")][InlineData("onlyMe")]
    public void NonPublicAudiencesAlwaysRequirePrivacyAwareService(string audience)=>Assert.True(SocialPolicy.NeedsPrivacyService(audience));
    [Fact]
    public void ExplicitPublicPostingCanUseExistingPublicService()=>Assert.False(SocialPolicy.NeedsPrivacyService("public"));
    [Theory]
    [InlineData(null)][InlineData("")][InlineData("Everyone")][InlineData("PUBLIC")]
    public void UnknownAudiencesNeverDefaultToPublic(string audience)=>Assert.Throws<InvalidOperationException>(()=>SocialPolicy.Audience(audience));
    [Fact]
    public void CapabilityVersionMustExplicitlyPromisePrivacyEnforcement()
    {
        Assert.False(SocialPolicy.PrivacyReady(null));Assert.False(SocialPolicy.PrivacyReady(new(1,0)));Assert.False(SocialPolicy.PrivacyReady(new(0,1)));Assert.True(SocialPolicy.PrivacyReady(new(1,1)));
    }
    [Fact]
    public void UnverifiedPrivatePostsCannotBePresentedAsProtected()
    {
        var post=new FoodPost{Id="post",AuthorId="alice",Audience="friends"};Assert.Throws<InvalidOperationException>(()=>SocialPolicy.ValidatePost(post));post.PrivacyManaged=true;SocialPolicy.ValidatePost(post);
    }
    [Fact]
    public void LegacyPostsRemainExplicitlyPublicAndDoNotClaimPrivacyManagement()
    {
        var post=new FoodPost{Id="legacy",AuthorId="alice"};Assert.Equal("public",post.Audience);Assert.False(post.PrivacyManaged);SocialPolicy.ValidatePost(post);
    }
    [Fact]
    public void HouseholdMealsValidateCalendarDatesSlotsAndPortions()
    {
        var meal=new HouseholdMeal(Guid.NewGuid().ToString("N"),"2026-10-17","dinner","Pasta",4,"Home at seven",null);SocialPolicy.ValidateMeal(meal);
        Assert.Throws<InvalidOperationException>(()=>SocialPolicy.ValidateMeal(meal with{Date="2026-02-30"}));Assert.Throws<InvalidOperationException>(()=>SocialPolicy.ValidateMeal(meal with{Slot="snack"}));Assert.Throws<InvalidOperationException>(()=>SocialPolicy.ValidateMeal(meal with{Servings=0}));Assert.Throws<InvalidOperationException>(()=>SocialPolicy.ValidateMeal(meal with{Note=new string('x',501)}));
        Assert.Equal("2026-10-17",SocialPolicy.DateKey(new DateTime(2026,10,17,23,59,0)));
    }
}
