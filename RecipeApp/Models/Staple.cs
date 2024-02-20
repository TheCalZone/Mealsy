using MvvmHelpers;
using Plugin.Firebase.Firestore;
using RecipeApp.Interfaces;
using SQLite;

namespace RecipeApp.Models
{
    public class Staple : ObservableObject, IIdentifiable
    {
        private bool isSelected;

        [PrimaryKey, AutoIncrement]
        [FirestoreProperty("sqlId")]
        public long Id { get; set; }

        [Ignore]
        [FirestoreDocumentId]
        public string FirestoreID { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }
        [FirestoreProperty("tag")]
        public string Tag { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        [FirestoreServerTimestamp("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }

        [FirestoreProperty("firebaseUID")]
        public string FirebaseUID { get; set; }
    }
}
