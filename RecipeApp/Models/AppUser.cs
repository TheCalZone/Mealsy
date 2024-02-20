using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeApp.Models
{
    public class AppUser : IFirestoreObject
    {
        [FirestoreProperty("first_name")]
        public string FirstName { get; set; }
        [FirestoreProperty("last_name")]
        public string LastName { get; set; }
        [FirestoreProperty("email")]
        public string Email { get; set; }
        [FirestoreProperty("diet_restrictions")]
        public string DietRestrictions { get; set; }
        [FirestoreProperty("favourite_cuisines")]
        public string FavouriteCuisines { get; set; }
        [FirestoreProperty("firebase_uid")]
        public string FirebaseUID { get; set; }


        public AppUser() { }
        // Converts a Dictionary to User Object
        public static AppUser FromDictionary(Dictionary<object, object> dict)
        {
            return new AppUser
            {
                FirebaseUID = dict["uid"]?.ToString(),
                FirstName = dict["firstname"]?.ToString(),
                LastName = dict["lastname"]?.ToString(),
                Email = dict["email"]?.ToString(),
                DietRestrictions = dict["dietRestrict"]?.ToString(),
                FavouriteCuisines = dict["favouriteCuisine"]?.ToString()
            };
        }
    }

}
