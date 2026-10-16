using RecipeApp.Helpers;
using RecipeApp.Views;
using Firebase.Auth;
using Plugin.Firebase.Auth;
using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.Firebase.Core.Exceptions;

namespace RecipeApp.ViewModels
{
    internal class AccountPageViewModel : CustomViewModelBase
    {
        private IFirebaseUser user;
        public IFirebaseUser User
        {
            get => user;
            set => SetProperty(ref user, value);
        }

        private AppUser appUser;
        public AppUser CurrentAppUser
        { get => appUser; set => SetProperty(ref appUser, value); }

        private string emailEntryText;
        public string EmailEntryText
        {
            get => emailEntryText;
            set => SetProperty(ref emailEntryText, value);
        }

        private string passwordEntryText;
        public string PasswordEntryText
        {
            get => passwordEntryText;
            set => SetProperty(ref passwordEntryText, value);
        }
        private bool isAuthenticated;

        public MvvmHelpers.Commands.Command SignOutCommand { get; set; }
        public MvvmHelpers.Commands.Command SignInCommand { get; set; }

        public MvvmHelpers.Commands.Command SignupCommand { get; set; }

        public MvvmHelpers.Commands.Command ResetPasswordCommand { get; set; }
        public AsyncCommand EditDetailsCommand { get; set; }

        public bool IsAuthenticated { get => isAuthenticated; set => SetProperty(ref isAuthenticated, value); }

        public AccountPageViewModel()
        {
            GetUserInfo();
            SignOutCommand = new MvvmHelpers.Commands.Command(SignOut);
            SignInCommand = new MvvmHelpers.Commands.Command(SignIn);
            SignupCommand = new MvvmHelpers.Commands.Command(SignUp);
            ResetPasswordCommand = new MvvmHelpers.Commands.Command(ResetPassword);
            EditDetailsCommand = new AsyncCommand(EditDetails);
        }


        private async void SignUp()
        {
            await Shell.Current.GoToAsync(nameof(SignupPage));
        }

        private async Task EditDetails()
        {
            await Shell.Current.GoToAsync(nameof(EditdetailsPage));
        }

        private async void SignIn(object obj)
        {
            try
            {
                var email = EmailEntryText;
                var password = PasswordEntryText;
                var authResult = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password);

                // Save the email and password to secure storage
                await SecureStorage.SetAsync("email", email);
                await SecureStorage.SetAsync("password", password);

                // Update AuthHelper with user's information
                AuthHelper.UserId = authResult.Uid;
                AuthHelper.Email = authResult.Email;
                // Update other properties as needed

                PasswordEntryText = string.Empty;

                User = authResult;
                IsAuthenticated = true;

                RecipeService.AsignFireBaseUIDToRecipes();
                GetUserInfo();
                await LoadDataAsync();
            }
            catch (Plugin.Firebase.Core.Exceptions.FirebaseAuthException ex)
            {
                // Handle specific FirebaseAuthException errors
                string errorMessage;
                switch (ex.Reason)
                {
                    case FIRAuthError.InvalidEmail:
                        errorMessage = "The email address is invalid.";
                        break;
                    case FIRAuthError.Undefined:
                        errorMessage = "An error occurred. Please try again later.";
                        break;
                    case FIRAuthError.WrongPassword:
                        errorMessage = "The password is incorrect.";
                        break;
                    case FIRAuthError.WeakPassword:
                        errorMessage = "The password is too weak.";
                        break;
                    case FIRAuthError.EmailAlreadyInUse:
                        errorMessage = "The email address is already in use.";
                        break;
                    case FIRAuthError.UserNotFound:
                        errorMessage = "The email address is not associated with any account.";
                        break;
                    case FIRAuthError.UserTokenExpired:
                        errorMessage = "The user's token has expired.";
                        break;
                    case FIRAuthError.InvalidCredential:
                        errorMessage = "The credential is invalid.";
                        break;
                    case FIRAuthError.UserDisabled:
                        errorMessage = "The user account has been disabled.";
                        break;
                    case FIRAuthError.AccountExistsWithDifferentCredential:
                        errorMessage = "An account already exists with the same email address but different sign-in credentials.";
                        break;
                    default:
                        errorMessage = "An error occurred. Please try again later.";
                        break;
                }
                await Shell.Current.DisplayAlert("Authentication Failed", errorMessage, "OK");
            }
            catch (Exception)
            {
                // Handle other unexpected errors
                await Shell.Current.DisplayAlert("Error", "An unexpected error occurred. Please try again later.", "OK");
            }
        }

        private async void SignOut(object obj)
        {
            try
            {
                await CrossFirebaseAuth.Current.SignOutAsync();
                SecureStorage.Remove("email");
                SecureStorage.Remove("password");
                AuthHelper.Reset();
                User = null;
                CurrentAppUser = null;
                PasswordEntryText = string.Empty;
                IsAuthenticated = false;
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Sign out failed", "Could not sign out. Please try again.", "OK");
            }
        }

        private async void ResetPassword()
        {
            if (string.IsNullOrWhiteSpace(EmailEntryText))
            {
                await Shell.Current.DisplayAlert("Email required", "Enter your email address first.", "OK");
                return;
            }
            try
            {
                await CrossFirebaseAuth.Current.SendPasswordResetEmailAsync(EmailEntryText.Trim());
                await Shell.Current.DisplayAlert("Password reset", "Check your email for password reset instructions.", "OK");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Reset failed", "Could not request a password reset. Check the address and your connection.", "OK");
            }
        }

        public async void GetUserInfo()
        {
            try
            {
                User = CrossFirebaseAuth.Current.CurrentUser;
                IsAuthenticated = User != null;
                CurrentAppUser = User == null ? null : await FirestoreService.GetUserAsync(User.Uid);
            }
            catch (Exception)
            {
                CurrentAppUser = User == null ? null : new AppUser { Email = User.Email };
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var recipes = await FirestoreService.GetAllRecipesAsync();
                var sqlRecipes = RecipeService.Recipes.Where(r => r.FirebaseUID == AuthHelper.UserId || string.IsNullOrEmpty(r.FirebaseUID)).ToList();

                await UpdateLocalData(recipes.ToList(), sqlRecipes.ToList(), recipe => RecipeService.UpdateRecipeAndIngredientsAsync(recipe, recipe.Ingredients), recipe => RecipeService.AddRecipe(recipe), recipe => FirestoreService.AddRecipe(recipe));

                await FirestoreService.SyncShoppingLibrary();

            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Sync unavailable", "You are signed in, but cloud data could not be synchronized. Local data is still available.", "OK");
            }
        }
        private async Task UpdateLocalData<T>(List<T> firestoreList, List<T> sqlList, Func<T, Task> updateFunc, Func<T, Task> addSqlFunc, Func<T, Task> addFirebaseFunc) where T : IIdentifiable
        {
            // Convert the list of SQL items into a dictionary
            var sqlItemsDict = sqlList.ToDictionary(r => r.Id);

            // Convert the list of Firestore items into a dictionary
            var firestoreItemsDict = firestoreList.ToDictionary(r => r.Id);

            // Iterate over the Firestore items
            foreach (var item in firestoreList)
            {
                // Try to find the corresponding SQL item
                if (sqlItemsDict.TryGetValue(item.Id, out var sqlItem))
                {
                    // Now you have both the Firestore item and the corresponding SQL item
                    // You can compare their LastUpdated property or perform any other comparison you need
                    if (item.LastUpdated > sqlItem.LastUpdated)
                    {
                        // The Firestore item is more recent, so update the SQL item
                        await updateFunc(item);
                    }
                }
                else
                {
                    // There is no corresponding SQL item for this Firestore item
                    // You can handle this case as needed, for example, add the Firestore item to the SQL database
                    await addSqlFunc(item);
                }
            }

            // Iterate over the SQL items
            foreach (var item in sqlList)
            {
                // If the SQL item doesn't exist in Firestore
                if (!firestoreItemsDict.ContainsKey(item.Id))
                {
                    // Add the item to Firestore
                    // For this, you need to pass an appropriate function to this method
                    // (similar to the updateFunc) that knows how to add a new item to Firestore
                    await addFirebaseFunc(item);
                }
            }
        }

    }
}
