using Plugin.Firebase.Core.Exceptions;

namespace RecipeApp.Services;

// Auth v5 preserves native errors instead of the old normalized Reason enum.
// Convert only known codes to our existing safe copy; never show native messages.
public static class AccountAuthErrors
{
    public static string Reason(CrossPlatformFirebaseAuthException error) => error.NativeErrorCode switch
    {
        "ERROR_USER_DISABLED" => "UserDisabled",
        "ERROR_INVALID_EMAIL" => "InvalidEmail",
        "ERROR_NETWORK_REQUEST_FAILED" => "NetworkError",
        "ERROR_USER_TOKEN_EXPIRED" or "ERROR_INVALID_USER_TOKEN" => "UserTokenExpired",
        _ => FirebaseAuthErrorClassifier.TryClassify(error) switch
        {
            FirebaseAuthFailure.InvalidCredentials => "InvalidCredential",
            FirebaseAuthFailure.UserNotFound => "UserNotFound",
            FirebaseAuthFailure.UserCollision => "EmailAlreadyInUse",
            FirebaseAuthFailure.WeakPassword => "WeakPassword",
            FirebaseAuthFailure.RequiresRecentLogin => "UserTokenExpired",
            FirebaseAuthFailure.TooManyRequests => "TooManyRequests",
            _ => "Unknown"
        }
    };
}
