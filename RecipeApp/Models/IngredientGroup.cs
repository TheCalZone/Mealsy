using MvvmHelpers;
using Plugin.Firebase.Firestore;
using SQLite;
using SQLiteNetExtensions.Attributes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RecipeApp.Models
{
    public class IngredientGroup : ObservableRangeCollection<Ingredient>, INotifyPropertyChanged
    {
        private string key;
        public string Key
        {
            get { return key; }
            set
            {
                if (key != value)
                {
                    key = value;
                    OnPropertyChanged(nameof(Key));
                }
            }
        }

        private List<Ingredient> ingredients;
        public List<Ingredient> Ingredients
        {
            get { return ingredients; }
            set
            {
                if (ingredients != value)
                {
                    ingredients = value;
                    OnPropertyChanged(nameof(Ingredients));
                }
            }
        }

        public IngredientGroup()
        {
            // Parameterless constructor for Xamarin.Forms grouping
        }

        public IngredientGroup(string key, List<Ingredient> ingredients) :base(ingredients)
        {
            Key = key;
        }

        // Add this property to control the visibility of the bulk editor for each group

        private bool isGroupBulkEditorVisible;

        public bool IsGroupBulkEditorVisible
        {
            get { return isGroupBulkEditorVisible; }
            set
            {
                if (isGroupBulkEditorVisible != value)
                {
                    isGroupBulkEditorVisible = value;
                    OnPropertyChanged(nameof(IsGroupBulkEditorVisible));
                }
            }
        }

        private string bulkEditorText;
        public string BulkEditorText
        {
            get { return bulkEditorText; }
            set
            {
                if (bulkEditorText != value)
                {
                    bulkEditorText = value;
                    OnPropertyChanged(nameof(BulkEditorText));
                }
            }
        }

        // Implement INotifyPropertyChanged manually
        public new event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
