using Plugin.Firebase.Core.Exceptions;
using RecipeApp.Services;
using Xunit;

public class AccountAuthErrorsTests
{
    [Theory]
    [InlineData("ERROR_USER_DISABLED", "UserDisabled")]
    [InlineData("ERROR_INVALID_EMAIL", "InvalidEmail")]
    [InlineData("ERROR_NETWORK_REQUEST_FAILED", "NetworkError")]
    [InlineData("ERROR_USER_TOKEN_EXPIRED", "UserTokenExpired")]
    public void NativeFailuresRetainSafeActionableCopy(string code, string reason)
    {
        var error = new CrossPlatformFirebaseAuthException("UnknownNativeType", "secret-native-detail", new Exception(), null, code);
        Assert.Equal(reason, AccountAuthErrors.Reason(error));
        Assert.DoesNotContain("secret-native-detail", AccountInput.Error(AccountAuthErrors.Reason(error)));
    }

    [Fact]
    public void UnknownFailureNeverExposesNativeMessage()
    {
        var error = new CrossPlatformFirebaseAuthException("UnknownNativeType", "secret-native-detail", new Exception(), null, "UNKNOWN");
        Assert.Equal("Unknown", AccountAuthErrors.Reason(error));
        Assert.DoesNotContain("secret-native-detail", AccountInput.Error(AccountAuthErrors.Reason(error)));
    }
}
