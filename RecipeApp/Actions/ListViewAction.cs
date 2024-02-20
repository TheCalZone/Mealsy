using RecipeApp.Models;

namespace RecipeApp.Actions
{
    public enum ActionType { AddGroup, RemoveGroup, AddItem, RemoveItem }


        public class ListViewAction
        {
            public ActionType Type { get; set; }
            public IngredientGroup Group { get; set; }
            public Ingredient Item { get; set; }
            public int GroupIndex { get; set; }
            public int ItemIndex { get; set; }
        }

}
