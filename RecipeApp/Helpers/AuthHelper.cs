using Firebase.Auth;
using Plugin.Firebase.Auth;
using RecipeApp.Services;

namespace RecipeApp.Helpers

{
    public static class AuthHelper
    {
        public static IFirebaseAuth Client => CrossFirebaseAuth.Current;

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
