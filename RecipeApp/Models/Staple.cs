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

        [Ignore]
        [FirestoreProperty("deleted")]
        public bool IsDeleted { get; set; }

        [FirestoreProperty("cloudKey")]
        public string CloudKey { get; set; }

        [FirestoreProperty("quantity")]
        public string Quantity { get; set; } = "";
        [FirestoreProperty("note")]
        public string Note { get; set; } = "";

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
