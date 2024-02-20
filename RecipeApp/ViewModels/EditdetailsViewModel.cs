using Firebase.Auth;
using MvvmHelpers.Commands;
using Plugin.Firebase.Auth;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels
{
    internal class EditdetailsViewModel : CustomViewModelBase
    {

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

        public void InitializeAsync()
        {
            LoadUsers();
            Email = CurrentAppUser.Email;
            FirstName = CurrentAppUser.FirstName;
            LastName = CurrentAppUser.LastName;
            DietRestrictions = CurrentAppUser.DietRestrictions;
            FavouriteCuisines = CurrentAppUser.FavouriteCuisines;
        }

        public async void LoadUsers()
        {
            CurrentAppUser =  await FirestoreService.GetUserAsync(CrossFirebaseAuth.Current.CurrentUser.Uid);
        }

        private async Task SaveChanges()
        {
            var email = Email;
            var firstname = FirstName;
            var lastname = LastName;
            var dietRestrictions = DietRestrictions;
            var favouritecuisine = FavouriteCuisines;

            var userdata = new Dictionary<object, object>
                {
                    { "email", email },
                    {"firstname", firstname },
                    {"lastname", lastname },
                    {"dietRestrict", dietRestrictions },
                    {"favouriteCuisine", favouritecuisine },
                };


            await CrossFirebaseAuth.Current.CurrentUser.UpdateProfileAsync(firstname + " " + lastname);
            ////Add user to firestore with info
            await FirestoreService.SetUserData(CrossFirebaseAuth.Current.CurrentUser.Uid, userdata);
            await Shell.Current.Navigation.PopAsync();
        }
    }
}
