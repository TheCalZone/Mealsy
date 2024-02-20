using MvvmHelpers;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace RecipeApp.Models
{
    public class MealPlan : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        private List<MealPlanDay> days;

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<MealPlanDay> Days { get => days; set => SetProperty(ref days, value); }

        public MealPlan()
        {
            Days = new List<MealPlanDay>();
        }

    }

}
