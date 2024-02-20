using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeApp.Models
{
    public class IngredientGroupWrapper : ObservableObject
    {
        public IngredientGroupWrapper(IngredientGroup group)
        {
            ingredientGroup = group;
        }

        public string Key
        {
            get { return ingredientGroup.Key; }
            set
            {
                if (ingredientGroup.Key != value)
                {
                    ingredientGroup.Key = value;
                    OnPropertyChanged(nameof(Key));
                }
            }
        }

        private IngredientGroup ingredientGroup;
        public IngredientGroup IngredientGroup
        {
            get => ingredientGroup;
            set
            {
                if (ingredientGroup != value)
                {
                    ingredientGroup = value;
                    OnPropertyChanged(nameof(IngredientGroup));
                }
            }
        }


        public bool IsGroupBulkEditorVisible
        {
            get { return ingredientGroup.IsGroupBulkEditorVisible; }
            set
            {
                if (ingredientGroup.IsGroupBulkEditorVisible != value)
                {
                    ingredientGroup.IsGroupBulkEditorVisible = value;
                    OnPropertyChanged(nameof(IsGroupBulkEditorVisible));
                }
            }
        }

        public string BulkEditorText
        {
            get { return ingredientGroup.BulkEditorText; }
            set
            {
                if (ingredientGroup.BulkEditorText != value)
                {
                    ingredientGroup.BulkEditorText = value;
                    OnPropertyChanged(nameof(BulkEditorText));
                }
            }
        }
    }

}
