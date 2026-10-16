using Firebase.Auth;
using MvvmHelpers.Commands;
using Plugin.Firebase.Auth;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels
{
    internal class EditdetailsViewModel : CustomViewModelBase
    {

        private IFirebaseUser user;
        public IFirebaseUser User { get => user; set =>SetProperty(ref user, value); }

        private string email;
        private string firstName;
        private string lastName;
        private string dietRestrictions;
        private string favouriteCuisines;
        private AppUser appUser;

        public AppUser CurrentAppUser { get => appUser; set => SetProperty(ref appUser, value); }
        public string Email { get => email; set => SetProperty(ref email, value); }
        public string FirstName { get => firstName; set => SetProperty(ref firstName, value);}
        public string LastName { get => lastName; set => SetProperty(ref lastName, value);}
        public string DietRestrictions { get => dietRestrictions; set=>SetProperty(ref dietRestrictions, value); }
        public string FavouriteCuisines { get => favouriteCuisines;set=>SetProperty(ref favouriteCuisines, value); }

        public AsyncCommand SaveChangesCommand { get; }

        public EditdetailsViewModel()
        {
            SaveChangesCommand = new AsyncCommand(SaveChanges);
        }

        public async void InitializeAsync()
        {
            User = CrossFirebaseAuth.Current.CurrentUser;
            if (User == null) return;
            try
            {
            CurrentAppUser = await FirestoreService.GetUserAsync(User.Uid) ?? new AppUser { Email = User.Email };
            Email = CurrentAppUser.Email;
            FirstName = CurrentAppUser.FirstName;
            LastName = CurrentAppUser.LastName;
            DietRestrictions = CurrentAppUser.DietRestrictions;
            FavouriteCuisines = CurrentAppUser.FavouriteCuisines;
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Profile unavailable", "Could not load your profile. Please try again when online.", "OK");
            }
        }

        private async Task SaveChanges()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var email = Email;
                var firstname = FirstName;
                var lastname = LastName;
                var dietRestrictions = DietRestrictions;
                var favouritecuisine = FavouriteCuisines;

                var userdata = new Dictionary<object, object>
                    {
                        { "email", email },
                        {"first_name", firstname },
                        {"last_name", lastname },
                        {"diet_restrictions", dietRestrictions },
                        {"favourite_cuisines", favouritecuisine },
                    };


                await CrossFirebaseAuth.Current.CurrentUser.UpdateProfileAsync(firstname + " " + lastname);
                ////Add user to firestore with info
                await FirestoreService.SetUserData(CrossFirebaseAuth.Current.CurrentUser.Uid, userdata);
                await Shell.Current.Navigation.PopAsync();


            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Save failed", "Your changes could not be saved. Please check your connection and try again.", "OK");
            }
            finally { IsBusy = false; }
        }
    }
}
