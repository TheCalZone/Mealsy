using Firebase.Auth;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Core.Exceptions;
using Plugin.Firebase.Firestore;
using RecipeApp.Helpers;
using RecipeApp.Interfaces;
using RecipeApp.Models;
using RecipeApp.Services;
using System;
using System.Collections.Generic;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SignupPage : ContentPage
    {
        public SignupPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                var email = emailEntry.Text;
                var password = passwordEntry.Text;

                await CrossFirebaseAuth.Current.CreateUserAsync(email, password);

                var firstname = firstnameEntry.Text;
                var lastname = lastnameEntry.Text;
                var dietRestrictions = dietPicker.Text;
                var otherdietRes = otherDietInfo.Text;
                var favouritecuisine = favouriteCuisine.Text;

                var userdata = new Dictionary<object, object>
                {
                    { "email", email },
                    {"firstname", firstname },
                    {"lastname", lastname },
                    {"dietRestrict", dietRestrictions },
                    {"otherDietRestrict", otherdietRes },
                    {"favouriteCuisine", favouritecuisine },
                };

                AppUser appUser = new AppUser
                {
                    Email = email,
                    FirstName = firstname,
                    LastName = lastname,
                    DietRestrictions = dietRestrictions,
                    FavouriteCuisines = favouritecuisine,
                    FirebaseUID = CrossFirebaseAuth.Current.CurrentUser.Uid
                };

                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(CrossFirebaseAuth.Current.CurrentUser.Uid).SetDataAsync(appUser);



                //////Add user to firestore with info
                //await FirestoreService.SetUserData(CrossFirebaseAuth.Current.CurrentUser.Uid, userdata);

                // Set user info in AuthHelper
                //AuthHelper.UserId = CrossFirebaseAuth.Current.CurrentUser.Uid;
                //AuthHelper.Email = CrossFirebaseAuth.Current.CurrentUser.Email;

                await Shell.Current.GoToAsync("//ShoppingListMakerPage");
                // User registration was successful, navigate to the main app page or perform other actions
            }
            catch (Plugin.Firebase.Core.Exceptions.FirebaseAuthException ex)
            {
                // Handle specific FirebaseAuthException errors
                string errorMessage;
                switch (ex.Reason)
                {
                    case FIRAuthError.EmailAlreadyInUse:
                        errorMessage = "The email address is already in use by another account.";
                        break;
                    case FIRAuthError.InvalidEmail:
                        errorMessage = "The email address is invalid.";
                        break;
                    case FIRAuthError.WeakPassword:
                        errorMessage = "The password is too weak. Please choose a stronger password.";
                        break;
                    default:
                        errorMessage = "An error occurred during registration. Please try again later.";
                        break;
                }
                await DisplayAlert("Registration Failed", errorMessage, "OK");
            }
            catch (Exception ex)
            {
                // Handle other unexpected errors
                await DisplayAlert("Error", "An unexpected error occurred during registration. Please try again later.", "OK");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}