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
        ShoppingListStore.AccountId = () => CrossFirebaseAuth.Current.CurrentUser?.Uid;
        MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            base.OnStart();
            try
            {
                await RecipeService.GetRecipesForLocal();
                await RecipeService.GetSnacksForLocal();
                await RecipeService.GetStaplesForLocal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await MainPage.DisplayAlert("Local data unavailable", "Your local data could not be loaded. Please restart the app.", "OK");
                return;
            }
            try
            {
                var user = CrossFirebaseAuth.Current.CurrentUser;
                if (user == null)
                {
                    var email = await SecureStorage.GetAsync("email");
                    var password = await SecureStorage.GetAsync("password");
                    if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
                        user = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password);
                }
                if (user != null)
                {
                    AuthHelper.UserId = user.Uid;
                    AuthHelper.Email = user.Email;
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                // A network/secure-storage failure must not make local recipes unusable.
                Debug.WriteLine(ex);
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
            var sqlRecipes = RecipeService.Recipes.Where(r => r.FirebaseUID == AuthHelper.UserId || string.IsNullOrEmpty(r.FirebaseUID)).ToList();

            await UpdateLocalData(recipes.ToList(), sqlRecipes.ToList(), recipe => RecipeService.UpdateRecipeAndIngredientsAsync(recipe, recipe.Ingredients), recipe => RecipeService.AddRecipe(recipe), recipe => FirestoreService.AddRecipe(recipe));

            await FirestoreService.SyncShoppingLibrary();

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            // Keep the local data available when remote sync fails.
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
                    // Check for matching Name and FirebaseUID in addition to Id
                    if (item.Name != sqlItem.Name || item.FirebaseUID != sqlItem.FirebaseUID)
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

