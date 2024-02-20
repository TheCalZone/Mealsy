using Firebase.Auth;
using Plugin.Firebase.Auth;
using RecipeApp.Services;

namespace RecipeApp.Helpers

{
    public static class AuthHelper
    {
        static AuthHelper()
        {

        //    // Configure...
        //    var config = new FirebaseAuthConfig
        //    {
        //        ApiKey = "AIzaSyDrN_Bto6Rr8Vpleu_s-ce_An6reydlWLI",
        //        AuthDomain = "mealsy-b732b.firebaseapp.com",
        //        Providers = new Firebase.Auth.Providers.FirebaseAuthProvider[]
        //        {
        //// Add and configure individual providers
        //new GoogleProvider().AddScopes("email"),
        //new EmailProvider()
        //            // ...
        //        },
        //    };

        //    // ...and create your FirebaseAuthClient
        //    _client = new FirebaseAuthClient(config);
        }

        private static readonly IFirebaseAuth _client;


        public static IFirebaseAuth Client
        {
            get { return _client; }
        }

        public static string UserId { get; set; }
        public static string Email { get; set; }
        // Add any other properties you need to store for the authenticated user

        public static void Reset()
        {
            UserId = null;
            Email = null;
            // Reset other properties as needed
        }

    }

}
