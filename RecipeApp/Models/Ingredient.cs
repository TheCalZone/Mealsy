using MvvmHelpers;
using Plugin.Firebase.Firestore;
using SQLite;
using System.ComponentModel;
using SQLiteNetExtensions.Attributes;
using System.Runtime.CompilerServices;

namespace RecipeApp.Models
{
    public class Ingredient : ObservableObject, IFirestoreObject
    {

        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [FirestoreDocumentId]
        public string FirestoreId { get; set; }

        private string name;
        [FirestoreProperty("name")]
        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                {
                    OnNameChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        private bool hasNote;
        [FirestoreProperty("has_note")]
        [Ignore]
        public bool HasNote
        {
            get
            {
                if (string.IsNullOrEmpty(Note))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            set => SetProperty(ref hasNote, value);
        }

        [FirestoreProperty("note")]
        public string Note { get; set; }

        private string quantity;

        [FirestoreProperty("quantity")]
        public string Quantity
        {
            get => quantity;
            set => SetProperty(ref quantity, value);
        }



        [ForeignKey(typeof(Recipe))]
        [FirestoreProperty("recipe_id")]
        public long RecipeId { get; set; }

        [FirestoreProperty("ingredient_section")]
        public string IngredientSection { get; set; }


        private string tag;
        [FirestoreProperty("tag")]
        public string Tag
        {
            get => tag;
            set => SetProperty(ref tag, value);
        }

        private bool mainIngredient;

        [FirestoreProperty ("main_ingredient")]
        public bool MainIngredient
        {
            get { return mainIngredient; }
            set { SetProperty(ref mainIngredient, value); }
        }

        private bool inBasket;

        public bool InBasket
        {
            get { return inBasket; }
            set { SetProperty(ref inBasket, value); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Ingredient other = (Ingredient)obj;
            return Name == other.Name && Tag == other.Tag;
        }

        public override int GetHashCode()
        {
            return (Name, Tag).GetHashCode();
        }


        public event EventHandler OnNameChanged;


    }
}
