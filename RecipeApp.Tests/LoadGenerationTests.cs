using RecipeApp.Services;
using Xunit;
public class LoadGenerationTests
{
    [Fact]
    public void NewerRefreshPreventsTheOlderResponseFromReplacingAccountDetails()
    {
        var loads=new LoadGeneration();var old=loads.Begin("alice");var current=loads.Begin("alice");
        Assert.False(loads.IsCurrent(old,"alice"));Assert.True(loads.IsCurrent(current,"alice"));
    }
    [Fact]
    public void LeavingPageInvalidatesPendingResponsesEvenForTheSameAccount()
    {
        var loads=new LoadGeneration();var old=loads.Begin("alice");loads.Invalidate();Assert.False(loads.IsCurrent(old,"alice"));
        var returned=loads.Begin("alice");Assert.True(loads.IsCurrent(returned,"alice"));
    }
    [Fact]
    public void SwitchingOrSigningOutCannotApplyPreviousAccountsProfileOrAdminRole()
    {
        var loads=new LoadGeneration();var alice=loads.Begin("alice");
        Assert.False(loads.IsCurrent(alice,"bob"));Assert.False(loads.IsCurrent(alice,null));
        var bob=loads.Begin("bob");Assert.True(loads.IsCurrent(bob,"bob"));Assert.False(loads.IsCurrent(alice,"alice"));
    }
    [Fact]
    public void SignedOutViewIsSupersededAsSoonAsInlineAuthenticationCompletes()
    {
        var loads=new LoadGeneration();var guest=loads.Begin(null);var signedIn=loads.Begin("alice");
        Assert.False(loads.IsCurrent(guest,null));Assert.True(loads.IsCurrent(signedIn,"alice"));
    }
}
