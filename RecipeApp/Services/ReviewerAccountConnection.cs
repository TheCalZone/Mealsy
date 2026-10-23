using System.Security.Cryptography;
namespace RecipeApp.Services;

internal static class ReviewerAccountConnection
{
    // Uses the app's public Firebase client configuration, not an administrator key.
    public static async Task<CreatedReviewerAccount> Create(string email,string password)
    {
        using var client=new HttpClient(new HttpClientHandler {AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(30)};
        string apiKey;
#if ANDROID
        apiKey=global::Firebase.FirebaseApp.Instance.Options.ApiKey;
        var context=global::Android.App.Application.Context;
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Android-Package",context.PackageName);
#pragma warning disable CA1422, CS0618
        var signature=context.PackageManager.GetPackageInfo(context.PackageName,global::Android.Content.PM.PackageInfoFlags.Signatures)?.Signatures?.FirstOrDefault();
#pragma warning restore CA1422, CS0618
        if(signature!=null)client.DefaultRequestHeaders.TryAddWithoutValidation("X-Android-Cert",Convert.ToHexString(SHA1.HashData(signature.ToByteArray())));
#elif IOS
        apiKey=global::Firebase.Core.App.DefaultInstance.Options.ApiKey;
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Ios-Bundle-Identifier",Foundation.NSBundle.MainBundle.BundleIdentifier);
#else
        throw new InvalidOperationException("Account creation is available in the Android and iOS app.");
#endif
        return await ReviewerAccountSignup.Create(client,apiKey,email,password);
    }
}
