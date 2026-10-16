using Plugin.Firebase.Auth;
using RecipeApp.Helpers;
using RecipeApp.Interfaces;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Diagnostics;

namespace RecipeApp;

public partial class App : Application
{

        public App()
        {
        InitializeComponent();
        MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
        try
        {
        // Check if email and password exist in secure storage
            string email = await SecureStorage.GetAsync("email");
            string password = await SecureStorage.GetAsync("password");

            await RecipeService.GetRecipesForLocal();
        await RecipeService.GetSnacksForLocal();
        await RecipeService.GetStaplesForLocal();

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
        {
            try
            {
                // Authenticate the user using the stored email and password
                var authResult = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password);
                // Set user info in AuthHelper
                AuthHelper.UserId = authResult.Uid;
                AuthHelper.Email = authResult.Email;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // Handle errors (e.g., invalid email/password)
            }
            
        }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup failed: {ex.Message}");
        }
        }

        protected override void OnResume()
        {

        }

    private async Task LoadDataAsync()
    {
        try
        {

            var recipes = await FirestoreService.GetAllRecipesAsync();
            var sqlRecipes = RecipeService.Recipes.Where(item => item.FirebaseUID == AuthHelper.UserId).ToList();

            await UpdateLocalData(recipes.ToList(), sqlRecipes.ToList(), recipe => RecipeService.UpdateRecipeAndIngredientsAsync(recipe, recipe.Ingredients), recipe => RecipeService.AddRecipe(recipe), recipe => FirestoreService.AddRecipe(recipe));

            // Fetch snacks
            var snacks = await FirestoreService.GetAllSnacks();
            var sqlSnacks = RecipeService.Snacks.Where(item => item.FirebaseUID == AuthHelper.UserId).ToList();

            await UpdateLocalData(snacks.ToList(), sqlSnacks.ToList(), RecipeService.UpdateSnack, snack => RecipeService.InsertSnack(snack), snack => FirestoreService.AddSnack(snack));

            // Fetch staples
            var staples = await FirestoreService.GetAllEssentials();
            var sqlStaples = RecipeService.Staples.Where(item => item.FirebaseUID == AuthHelper.UserId).ToList();

            await UpdateLocalData(staples.ToList(), sqlStaples.ToList(), RecipeService.UpdateStaple, staple => RecipeService.InsertStaple(staple), staple => FirestoreService.AddStaple(staple));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            // Keep local data available when cloud synchronization fails.
        }
    }
    private async Task UpdateLocalData<T>(List<T> firestoreList, List<T> sqlList, Func<T, Task> updateFunc, Func<T, Task> addSqlFunc, Func<T, Task> addFirebaseFunc) where T : IIdentifiable
    {
        // Convert the list of SQL items into a dictionary by SQL Id
        var sqlItemsDict = sqlList.ToDictionary(r => r.Id);

        // Create a composite key based on Name and FirebaseUID for more precise duplicate checking
        var sqlItemsCompositeKeyDict = sqlList
            .GroupBy(r => $"{r.Name}-{r.FirebaseUID}")
            .ToDictionary(g => g.Key, g => g.ToList());

        // Convert the list of Firestore items into a dictionary by SqL ID
        var firestoreItemsDict = firestoreList.ToDictionary(item => item.Id);

        // Iterate over the Firestore items to update SQL database or add new records
        if (firestoreList.Count > 0)
        {
            foreach (var item in firestoreList)
            {
                // Generate the composite key for the current item
                var compositeKey = $"{item.Name}-{item.FirebaseUID}";

                // Use the dictionary for a quick lookup by sql ID
                if (sqlItemsDict.TryGetValue(item.Id, out var sqlItem))
                {
                    // Only update records owned by the same account.
                    if (item.FirebaseUID == sqlItem.FirebaseUID)
                    {
                        if (item.LastUpdated > sqlItem.LastUpdated)
                        {
                            await updateFunc(item);
                        }
                        // Continue to the next item if the current one doesn't need updating
                        continue;
                    }
                }
                else
                {
                    // New check: if UserID is null but an item with the same Name exists, skip adding
                    if (item.FirebaseUID == null && sqlList.Any(sql => sql.Name == item.Name))
                    {
                        continue; // Skip this item, do not add to SQL database
                    }
                    else if (!sqlItemsCompositeKeyDict.ContainsKey(compositeKey) || sqlItemsCompositeKeyDict[compositeKey].Count == 0)
                    {
                        // Item doesn't exist in SQL list by composite key, so add it
                        await addSqlFunc(item);
                    }
                }
            }
            }

        // Consider a similar approach for adding missing SQL items to Firestore if needed
        foreach (var item in sqlList)
        {
            if (!firestoreItemsDict.ContainsKey(item.Id) && !firestoreItemsDict.Any(f => f.Value.Name == item.Name && f.Value.FirebaseUID == item.FirebaseUID))
            {
                await addFirebaseFunc(item);
            }
        }
    }
}

