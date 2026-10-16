using MvvmHelpers;
using Plugin.Firebase.Firestore;
using RecipeApp.Interfaces;
using SQLite;
using SQLiteNetExtensions.Attributes;
using System.Runtime.InteropServices;

namespace RecipeApp.Models
{
    public class Recipe : ObservableObject, IFirestoreObject, IIdentifiable
    {
        [Ignore]
        // Explicit interface implementation for the Name property
        string IIdentifiable.Name
        {
            get { return this.RecipeName; }
            // Optional setter, if your interface allows setting the Name
            set { this.RecipeName = value; }
        }


        [PrimaryKey, AutoIncrement]
        [FirestoreProperty("sqlId")]
        public long Id { get; set; }

        [Ignore]
        [FirestoreDocumentId]
        public string FirestoreID { get; set; }
        [FirestoreProperty("cloud_key")] public string CloudKey { get; set; }
        [Ignore, FirestoreProperty("deleted")] public bool IsDeleted { get; set; }
        private string recipeName;
        [FirestoreProperty("recipe_name")]
        public string RecipeName { get => recipeName; set => SetProperty(ref recipeName, value); }
        [FirestoreProperty("recipe_method")]
        public string RecipeMethod { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; } = "";
        [FirestoreProperty("cook_time")]
        public long CookTime { get; set; }
        [FirestoreProperty("source_url")]
        public string SourceUrl { get; set; } = "";
        [FirestoreProperty("notes")]
        public string Notes { get; set; } = "";
        [Ignore] public long TotalTime => PrepTime + CookTime;

        private string recipeImage;
        [FirestoreProperty("recipe_image")]
        public string RecipeImage { get => recipeImage; set => recipeImage = value; }
        private long serving;
        [FirestoreProperty("serving")]
        public long Serving { get => serving; set => SetProperty(ref serving, value); }
        private long prepTime;
        [FirestoreProperty("prep_time")]
        public long PrepTime { get => prepTime; set => SetProperty(ref prepTime, value); }
        private long rating;
        [FirestoreProperty("rating")]
        public long Rating { get => rating; set => SetProperty(ref rating, value); }

        //private ImageSource imageSource;
        //public ImageSource ImageSource
        //{
        //    get
        //    {
        //        if (imageSource == null)
        //        {
        //            SetProperty(ref imageSource, !string.IsNullOrEmpty(RecipeImage) ? ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(RecipeImage))) : null);
        //        }
        //        return imageSource;
        //    }
        //    set => SetProperty(ref imageSource, value);
        //}

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private bool isSelected;

        private bool isAddedToList;
        public bool IsAddedToList
        {
            get => isAddedToList;
            set => SetProperty(ref isAddedToList, value);
        }

        //private List<IngredientGroup> ingredientGroups;
        //[OneToMany(CascadeOperations = CascadeOperation.All)]
        //public List<IngredientGroup> IngredientGroups
        //{
        //    get => ingredientGroups;
        //    set => SetProperty(ref ingredientGroups, value);
        //}

        private List<Ingredient> ingredients = new();

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Ingredient> Ingredients
        {
            get => ingredients;
            set => SetProperty(ref ingredients, value);
        }

        private List<Ingredient> firebaseIngredients;

        [Ignore]
        [FirestoreProperty("ingredients")]
        public List<Ingredient> FirebaseIngredients
        {
            get => firebaseIngredients;
            set => SetProperty(ref firebaseIngredients, value);
        }

        [FirestoreProperty("tags")]
        public string Tags { get; set; }

        [FirestoreServerTimestamp("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [FirestoreProperty("firebaseUID")]
        public string FirebaseUID { get; set; }

        public Recipe()
        {

        }


    }
}
