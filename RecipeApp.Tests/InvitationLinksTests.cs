using RecipeApp.Services;
using Xunit;
public class InvitationLinksTests
{
    private static string Code => new('a',48);
    [Fact]
    public void HouseholdSharesContainInstallInstructionsAndRoundTripWithoutLoggingCodesInUrls()
    {
        var url=InvitationLinks.ForHousehold(Code);Assert.Contains("#code=",url);Assert.DoesNotContain("?code=",url);Assert.Equal(Code,InvitationLinks.Code(url));
        var message=InvitationLinks.ShareHousehold("Our family",Code);Assert.Contains(url,message);Assert.Contains("single-use",message);Assert.Contains("7 days",message);Assert.Contains("free",message);
    }
    [Theory]
    [InlineData("https://mealsy-b732b.web.app/join/#code=")]
    [InlineData("https://mealsy-b732b.firebaseapp.com/join/?code=")]
    [InlineData("mealsy://join?code=")]
    public void SupportedLinksAndUppercaseCodesResolveToTheSameInvitation(string prefix)=>Assert.Equal(Code,InvitationLinks.Code(prefix+Code.ToUpperInvariant()));
    [Theory]
    [InlineData("https://evil.example/join/#code=")]
    [InlineData("https://mealsy-b732b.web.app.evil.example/join/#code=")]
    [InlineData("http://mealsy-b732b.web.app/join/#code=")]
    [InlineData("https://user@mealsy-b732b.web.app/join/#code=")]
    [InlineData("mealsy://other?code=")]
    public void ForeignAndMisleadingLinksCannotBeUsedAsInvitations(string prefix)=>Assert.Throws<InvalidOperationException>(()=>InvitationLinks.Code(prefix+Code));
    [Theory]
    [InlineData("")][InlineData("1234")][InlineData("[x] Milk")]
    public void InvalidCodesDoNotBecomeInvitations(string value)=>Assert.Throws<InvalidOperationException>(()=>InvitationLinks.Code(value));
}
