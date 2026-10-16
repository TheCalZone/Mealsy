using MvvmHelpers;
using SQLite;
using SQLiteNetExtensions.Attributes;
using System.Collections.ObjectModel;

namespace RecipeApp.Models
{
    public class MealPlanDay : ObservableObject
    {
        private string breakfast;
        private string lunch;
        private string dinner;
        private List<long> breakfastRecipesIDs;
        private List<long> lunchRecipesIDs;
        private List<long> dinnerRecipesIDs;
        private ObservableRangeCollection<string> breakfastNotes;
        private ObservableRangeCollection<string> lunchNotes;
        private ObservableRangeCollection<string> dinnerNotes;
        private string breakfastIDJson;
        private string lunchIDJson;
        private string dinnerIDJson;
        private string breakfastNotesJson;
        private string lunchNotesJson;
        private string dinnerNotesJson;
        private ObservableRangeCollection<Recipe> breakfastRecipes;
        private ObservableRangeCollection<Recipe> lunchRecipes;
        private ObservableRangeCollection<Recipe> dinnerRecipes;

        private bool isBreakfastExtrasVisible;
        private bool isLunchExtrasVisible;
        private bool isDinnerExtrasVisible;



        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [ForeignKey(typeof(MealPlan))]
        public long MealPlanID { get; set; }
        public string BreakfastPortionsJson { get; set; } = "{}";
        public string LunchPortionsJson { get; set; } = "{}";
        public string DinnerPortionsJson { get; set; } = "{}";
        public string MealDay { get; set; }
        public long MealDayIndex { get; set; }
        public long BreakfastRecipeId { get; set; }
        public long LunchRecipeId { get; set; }
        public long DinnerRecipeId { get; set; }
        public string BreakfastIDJson { get => breakfastIDJson; set => SetProperty(ref breakfastIDJson, value); }
        public string LunchIDJson { get => lunchIDJson; set => SetProperty(ref lunchIDJson, value); }
        public string DinnerIDJson { get => dinnerIDJson; set => SetProperty(ref dinnerIDJson, value); }
        public string BreakfastNotesJson { get => breakfastNotesJson; set => SetProperty(ref breakfastNotesJson, value); }
        public string LunchNotesJson { get => lunchNotesJson; set => SetProperty(ref lunchNotesJson, value); }
        public string DinnerNotesJson { get => dinnerNotesJson; set => SetProperty(ref dinnerNotesJson, value); }



        public string Breakfast { get => breakfast; set => SetProperty(ref breakfast, value); }
        public string Lunch { get => lunch; set => SetProperty(ref lunch, value); }
        public string Dinner { get => dinner; set => SetProperty(ref dinner, value); }

        [Ignore]
        public ObservableRangeCollection<Recipe> BreakfastRecipes { get => breakfastRecipes; set => SetProperty(ref breakfastRecipes, value); }
        [Ignore]
        public ObservableRangeCollection<Recipe> LunchRecipes { get => lunchRecipes; set => SetProperty(ref lunchRecipes, value); }
        [Ignore]
        public ObservableRangeCollection<Recipe> DinnerRecipes { get => dinnerRecipes; set => SetProperty(ref dinnerRecipes, value); }

        // New properties for additional items
        [Ignore]
        public List<long> BreakfastRecipesIDs { get => breakfastRecipesIDs; set => SetProperty(ref breakfastRecipesIDs, value); }
        [Ignore]
        public List<long> LunchRecipesIDs { get => lunchRecipesIDs; set => SetProperty(ref lunchRecipesIDs, value); }
        [Ignore]
        public List<long> DinnerRecipesIDs { get => dinnerRecipesIDs; set => SetProperty(ref dinnerRecipesIDs, value);}
        [Ignore]
        public ObservableRangeCollection<string> BreakfastNotes { get => breakfastNotes; set
            {
                SetProperty(ref breakfastNotes, value);
                IsBreakfastExtrasVisible = BreakfastRecipes?.Count > 0 && breakfastNotes?.Count > 0;
            }
        }
        [Ignore]
        public ObservableRangeCollection<string> LunchNotes { get => lunchNotes; set
            {
                SetProperty(ref lunchNotes, value);
                IsLunchExtrasVisible = LunchRecipes?.Count > 0 && lunchNotes?.Count > 0;
            }
        }
        [Ignore]
        public ObservableRangeCollection<string> DinnerNotes { get => dinnerNotes; set
            {
                SetProperty(ref dinnerNotes, value);
                IsDinnerExtrasVisible = DinnerRecipes?.Count > 0 && dinnerNotes?.Count > 0;
            }
        }
        [Ignore]
        public bool IsBreakfastExtrasVisible { get => isBreakfastExtrasVisible; set => SetProperty(ref isBreakfastExtrasVisible, value); }
        [Ignore]
        public bool IsLunchExtrasVisible { get => isLunchExtrasVisible; set => SetProperty(ref isLunchExtrasVisible, value); }
        [Ignore]
        public bool IsDinnerExtrasVisible { get => isDinnerExtrasVisible; set => SetProperty(ref isDinnerExtrasVisible, value); }


        public MealPlanDay()
        {
            BreakfastNotes = new ObservableRangeCollection<string>();
            LunchNotes = new ObservableRangeCollection<string>();
            DinnerNotes = new ObservableRangeCollection<string>();
            BreakfastRecipesIDs = new List<long>();
            LunchRecipesIDs = new List<long>();
            DinnerRecipesIDs = new List<long>();
            BreakfastRecipes = new ObservableRangeCollection<Recipe>();
            LunchRecipes = new ObservableRangeCollection<Recipe>();
            DinnerRecipes = new ObservableRangeCollection<Recipe>();
            BreakfastRecipes.CollectionChanged += (sender, e) => IsBreakfastExtrasVisible = BreakfastRecipes?.Count > 0 && breakfastNotes?.Count > 0;
            LunchRecipes.CollectionChanged += (sender, e) => IsLunchExtrasVisible = LunchRecipes?.Count > 0 && lunchNotes?.Count > 0;
            DinnerRecipes.CollectionChanged += (sender, e) => IsDinnerExtrasVisible = DinnerRecipes?.Count > 0 && dinnerNotes?.Count > 0;
            breakfastNotes.CollectionChanged += (sender, e) => IsBreakfastExtrasVisible = BreakfastRecipes?.Count > 0 && breakfastNotes?.Count > 0;
            lunchNotes.CollectionChanged += (sender, e) => IsLunchExtrasVisible = LunchRecipes?.Count > 0 && lunchNotes?.Count > 0;
            dinnerNotes.CollectionChanged += (sender, e) => IsDinnerExtrasVisible = DinnerRecipes?.Count > 0 && dinnerNotes?.Count > 0;
        }

    }

}
