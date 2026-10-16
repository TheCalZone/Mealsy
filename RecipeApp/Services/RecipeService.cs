using MvvmHelpers;
using Plugin.Firebase.Auth;
using RecipeApp.Models;
using SQLite;
using SQLiteNetExtensionsAsync.Extensions;
using System.Diagnostics;

namespace RecipeApp.Services
{
    public static class RecipeService
    {
        public static List<Recipe> Recipes = new();
        public static List<Snack> Snacks = new();
        public static List<Staple> Staples = new();

        public const string DatabaseFilename = "MyData.db3";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        static SQLiteAsyncConnection db;

        private static readonly SemaphoreSlim initializationLock = new(1, 1);
        private static bool initialized;

        static async Task Init()
        {
            await initializationLock.WaitAsync();
            try
            {
                if (initialized)
                    return;

                db ??= new SQLiteAsyncConnection(DatabasePath, Flags);
                await db.CreateTableAsync<Recipe>();
                await db.CreateTableAsync<Ingredient>();
                await db.CreateTableAsync<MealPlan>();
                await db.CreateTableAsync<MealPlanDay>();
                await db.CreateTableAsync<Snack>();
                await db.CreateTableAsync<Staple>();
                await db.CreateTableAsync<ShoppingList>();
                initialized = true;
            }
            finally
            {
                initializationLock.Release();
            }
        }

        public static List<Ingredient> MasterIngredientList = new List<Ingredient>
        {
            // Tins & Cans
            new Ingredient { Name = "salt", Tag= "Tins"},
            new Ingredient { Name = "black pepper", Tag= "Tins" },
            new Ingredient { Name = "olive oil", Tag = "Tins" },
            new Ingredient { Name = "beans", Tag = "Tins" },
            new Ingredient { Name = "corn", Tag = "Tins" },
            new Ingredient { Name = "tuna", Tag = "Tins" },
            new Ingredient { Name = "baking soda", Tag = "Tins" },
            new Ingredient { Name = "baking powder", Tag = "Tins" },

            // Vegetables
            new Ingredient { Name = "onion" , Tag = "Veg"},
            new Ingredient { Name = "garlic" ,Tag="Veg"},
            new Ingredient { Name = "tomatoes", Tag = "Veg" },
            new Ingredient { Name = "peppers", Tag = "Veg" },
            new Ingredient { Name = "potatoes", Tag = "Veg" },
            new Ingredient { Name = "carrots", Tag = "Veg" },
            new Ingredient { Name = "broccoli", Tag = "Veg" },
            new Ingredient { Name = "zucchini", Tag = "Veg" },
            new Ingredient { Name = "spinach", Tag = "Veg" },
            new Ingredient { Name = "lettuce", Tag = "Veg" },
            new Ingredient { Name = "cabbage", Tag = "Veg" },
            new Ingredient { Name = "mushrooms", Tag = "Veg" },
    
            // Meat & Poultry
            new Ingredient { Name = "chicken", Tag = "Meat" },
            new Ingredient { Name = "beef", Tag = "Meat"  },
            new Ingredient { Name = "pork",  Tag = "Meat"  },
            new Ingredient { Name = "bacon", Tag = "Meat"  },
            new Ingredient { Name = "sausages", Tag = "Meat"  },
            new Ingredient { Name = "lamb", Tag = "Meat"  },
            new Ingredient { Name = "turkey", Tag = "Meat" },

            // Pantry
            new Ingredient { Name = "rice", Tag = "Pantry" },
            new Ingredient { Name = "pasta", Tag = "Pantry" },
            new Ingredient { Name = "sugar" , Tag = "Pantry"},
            new Ingredient { Name = "cereal", Tag = "Pantry" },
            new Ingredient { Name = "honey", Tag = "Pantry" },
            new Ingredient { Name = "oats", Tag = "Pantry" },
            new Ingredient { Name = "bread crumbs", Tag = "Pantry" },

            // Bakery
            new Ingredient { Name = "flour", Tag = "Bakery" },
            new Ingredient { Name = "eggs" , Tag = "Bakery"},
            new Ingredient { Name = "bread", Tag = "Bakery" },
            new Ingredient { Name = "rolls", Tag = "Bakery" },
            new Ingredient { Name = "bagels", Tag = "Bakery" },
            new Ingredient { Name = "muffins", Tag = "Bakery" },

            // Dairy
            new Ingredient { Name = "milk" , Tag = "Dairy"},
            new Ingredient { Name = "yogurt", Tag = "Dairy" },
            new Ingredient { Name = "cheese", Tag = "Dairy" },
            new Ingredient { Name = "butter", Tag = "Dairy" },
            new Ingredient { Name = "cream", Tag = "Dairy" },

            // Fruits
            new Ingredient { Name = "apples", Tag = "Fruits" },
            new Ingredient { Name = "bananas", Tag = "Fruits" },
            new Ingredient { Name = "berries", Tag = "Fruits" },
            new Ingredient { Name = "oranges", Tag = "Fruits" },
            new Ingredient { Name = "pears", Tag = "Fruits" },
            new Ingredient { Name = "grapes", Tag = "Fruits" },
            new Ingredient { Name = "kiwi", Tag = "Fruits" },

            // Frozen
            new Ingredient { Name = "ice", Tag = "Frozen" },
            new Ingredient { Name = "frozen", Tag = "Frozen" },

            // Beverages
            new Ingredient { Name = "water", Tag = "Beverages" },
            new Ingredient { Name = "juice", Tag = "Beverages" },
            new Ingredient { Name = "tea", Tag = "Beverages" },
            new Ingredient { Name = "coffee", Tag = "Beverages" },
            new Ingredient { Name = "soda", Tag = "Beverages" },
        };


        public static async Task AddRecipe(Recipe recipeToAdd)
        {
            await Init();
            var existing = recipeToAdd.Id == 0 ? null :
                await db.Table<Recipe>().FirstOrDefaultAsync(recipe => recipe.Id == recipeToAdd.Id);
            if (existing != null)
            {
                // Device-local numeric IDs cannot identify records from another account.
                if (existing.FirebaseUID != recipeToAdd.FirebaseUID)
                    throw new InvalidOperationException("Recipe ID conflicts with another account's local data.");
                return;
            }
            await db.InsertWithChildrenAsync(recipeToAdd);
            Recipes.Add(recipeToAdd);
        }

        public static async Task AddSnack(string name)
        {
            await Init();
            var snack = new Snack { Name = name };
            await db.InsertAsync(snack);
            Snacks.Add(snack);
        }

        public static async Task AddStaple(string name)
        {
            await Init();
            var staple = new Staple { Name = name };
            await db.InsertAsync(staple);
            Staples.Add(staple);
        }

        public static async Task CreateMealPlan()
        {
            await Init();
            await db.RunInTransactionAsync(connection =>
            {
                var plan = connection.Table<MealPlan>().FirstOrDefault();
                if (plan == null)
                {
                    plan = new MealPlan { Name = "Default 7 days" };
                    connection.Insert(plan);
                }

                var days = connection.Table<MealPlanDay>()
                    .Where(day => day.MealPlanID == plan.Id).ToList();
                for (int index = 0; index < 7; index++)
                {
                    if (!days.Any(day => day.MealDayIndex == index))
                        connection.Insert(new MealPlanDay
                        {
                            MealDay = ((DayOfWeek)index).ToString(),
                            MealDayIndex = index,
                            MealPlanID = plan.Id
                        });
                }
            });
        }

        public static async Task UpdateRecipeAndIngredientsAsync(Recipe recipe, List<Ingredient> ingredients)
        {
            await Init();
            ingredients ??= new List<Ingredient>();
            recipe.LastUpdated = DateTimeOffset.UtcNow;
            await db.RunInTransactionAsync(connection =>
            {
                connection.Update(recipe);
                connection.Execute("DELETE FROM Ingredient WHERE RecipeId = ?", recipe.Id);
                foreach (var ingredient in ingredients)
                {
                    // Ingredient IDs belong to this device, including after a cloud import.
                    ingredient.Id = 0;
                    ingredient.Tag ??= string.Empty;
                    ingredient.RecipeId = recipe.Id;
                    connection.Insert(ingredient);
                }
            });
            recipe.Ingredients = ingredients;
            var index = Recipes.FindIndex(item => item.Id == recipe.Id);
            if (index >= 0)
                Recipes[index] = recipe;
        }

        public static async Task UpdateRecipeFavouriteState(Recipe recipe)
        {
            await Init();
            await db.CreateTableAsync<Recipe>();

            await db.UpdateAsync(recipe);
        }

        public static async Task InsertIngredientAsync(Ingredient ingredient)
        {
            await Init();
            await db.CreateTableAsync<Recipe>();

            await db.InsertAsync(ingredient);
        }

        public static async Task RemoveIngredientAsync(long id)
        {
            await Init();
            await db.CreateTableAsync<Recipe>();

            await db.DeleteAsync<Ingredient>(id);
        }

        public static async Task RemoveRecipe(long id)
        {
            await Init();
            await db.CreateTableAsync<Recipe>();
            await db.RunInTransactionAsync(connection =>
            {
                connection.Execute("DELETE FROM Ingredient WHERE RecipeId = ?", id);
                connection.Delete<Recipe>(id);
            });
            Recipes.Remove(Recipes.FirstOrDefault(r => r.Id == id));
        }
        public static async Task<Recipe> GetRecipe(long id)
        {
            await Init();
            await db.CreateTableAsync<Recipe>();

            var recipe = await db.Table<Recipe>().FirstOrDefaultAsync(c => c.Id == id);
            if (recipe == null)
                return null;
            recipe.Ingredients = await db.Table<Ingredient>().Where(i => i.RecipeId == recipe.Id).ToListAsync();
            return recipe;
        }

        public static async Task GetRecipesForLocal()
        {
            await Init();
            if (Recipes != null)
            {
                Recipes.Clear();
            }
            await db.CreateTableAsync<Recipe>();

            Recipes = await db.GetAllWithChildrenAsync<Recipe>();
        }

        public static async Task GetSnacksForLocal()
        {
            await Init();
            await db.CreateTableAsync<Snack>();

            Snacks = await db.Table<Snack>().ToListAsync();
        }

        public static async Task GetStaplesForLocal()
        {
            await Init();
            await db.CreateTableAsync<Staple>();

            Staples = await db.Table<Staple>().ToListAsync();
        }

        public static async Task<List<Recipe>> GetAllRecipes()
        {
            await Init();
            await db.CreateTableAsync<Recipe>();

            var recipes = new List<Recipe>();
            try
            {
                try
                {
                    await Init();
                }
                catch (SQLiteException ex)
                {
                    // Check for SQLite constraint violation (unique constraint on RecipeId)
                    Debug.WriteLine("Error with init: " + ex.Message);
                    // Handle the case where there's a constraint violation (e.g., duplicate RecipeId)
                }
                catch (Exception ex)
                {
                    // Rethrow the exception if it's not a constraint violation
                    Debug.WriteLine("Error with init: " + ex.Message);
                    throw;
                }

                try
                {
                    recipes = await db.GetAllWithChildrenAsync<Recipe>();
                }
                catch (SQLiteException ex)
                {
                    // Check for SQLite constraint violation (unique constraint on RecipeId)
                    Debug.WriteLine("Error updating recipe and ingredients: " + ex.Message);
                    // Handle the case where there's a constraint violation (e.g., duplicate RecipeId)
                }
                catch (Exception ex)
                {
                    // Rethrow the exception if it's not a constraint violation
                    Debug.WriteLine("Error updating recipe and ingredients: " + ex.Message);
                    throw;
                }

                Debug.WriteLine("Calling GetAllWithChildrenAsync in GetAllRecipes()");

                Debug.WriteLine($"GetAllRecipes() returning {recipes.Count} recipes");

                return recipes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetAllRecipes(): {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return new List<Recipe>();
            }
        }


        #region Snack Actions
        public static async Task DeleteSnack(Snack snack)
        {
            await Init();
            await db.DeleteAsync(snack);
            Snacks.RemoveAll(item => item.Id == snack.Id);
        }
        public static async Task InsertSnack(Snack snack)
        {
            await Init();
            await db.InsertAsync(snack);
        }
        public static async Task<List<Snack>> GetAllSnacks()
        {
            await Init();
            var snacks = await db.Table<Snack>().ToListAsync();
            return snacks;
        }

        public static async Task UpdateSnack(Snack snack)
        {
            await Init();
            try
            {
                await db.UpdateAsync(snack); // Update the snack

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error updating snack " + ex.Message);
            }
        }

        public static async Task<Snack> GetSnackByName(string snackName)
        {
            await Init();
            var tempSnack = await db.Table<Snack>().Where(d => d.Name == snackName).FirstOrDefaultAsync();
            return tempSnack;
        }
        #endregion

        #region Staple Actions
        public static async Task DeleteStaple(Staple staple)
        {
            await Init();
            await db.DeleteAsync(staple);
            Staples.RemoveAll(item => item.Id == staple.Id);
        }
        public static async Task InsertStaple(Staple staple)
        {
            await Init();
            await db.InsertAsync(staple);
        }
        public static async Task<List<Staple>> GetAllStaples()
        {
            await Init();
            var staples = await db.Table<Staple>().ToListAsync();
            return staples;
        }
        public static async Task UpdateStaple(Staple staple)
        {
            await Init();
            try
            {
                await db.UpdateAsync(staple); // Update the recipe
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error updating staple " + ex.Message);
            }
        }
        public static async Task<Staple> GetStapleByName(string stapleName)
        {
            await Init();
            var tempStaple = await db.Table<Staple>().Where(d => d.Name == stapleName).FirstOrDefaultAsync();
            return tempStaple;
        }
        #endregion

        public static async Task<MealPlan> GetMealPlan()
        {
            await Init();

            MealPlan mealPlan = null;
            try
            {
                mealPlan = await db.Table<MealPlan>().FirstOrDefaultAsync();
                if (mealPlan != null)
                {
                    var mealPlanDays = await db.Table<MealPlanDay>().Where(d => d.MealPlanID == mealPlan.Id).ToListAsync();
                    // Extra recipes are stored in JSON, so loading only the primary IDs loses them.
                    var recipes = await db.GetAllWithChildrenAsync<Recipe>();
                    mealPlanDays = mealPlanDays.OrderBy(day => day.MealDayIndex).ToList();

                    foreach (var mealPlanDay in mealPlanDays)
                    {
                        mealPlanDay.Breakfast = recipes.FirstOrDefault(r => r.Id == mealPlanDay.BreakfastRecipeId)?.RecipeName ?? string.Empty;
                        mealPlanDay.BreakfastNotesJson = mealPlanDay.BreakfastNotesJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.BreakfastNotesJson))
                        {
                            mealPlanDay.BreakfastNotesJson = "[]";
                        }
                        mealPlanDay.BreakfastNotes.AddRange(ReadList<string>(mealPlanDay.BreakfastNotesJson));
                        mealPlanDay.BreakfastIDJson = mealPlanDay.BreakfastIDJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.BreakfastIDJson))
                        {
                            mealPlanDay.BreakfastIDJson = "[]";
                        }
                        mealPlanDay.BreakfastRecipesIDs.AddRange(ReadList<long>(mealPlanDay.BreakfastIDJson));
                        mealPlanDay.BreakfastRecipes.AddRange(recipes.Where(r => mealPlanDay.BreakfastRecipesIDs.Contains(r.Id)));

                        mealPlanDay.Lunch = recipes.FirstOrDefault(r => r.Id == mealPlanDay.LunchRecipeId)?.RecipeName ?? string.Empty;
                        mealPlanDay.LunchNotesJson = mealPlanDay.LunchNotesJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.LunchNotesJson))
                        {
                            mealPlanDay.LunchNotesJson = "[]";
                        }

                        mealPlanDay.LunchNotes.AddRange(ReadList<string>(mealPlanDay.LunchNotesJson));
                        mealPlanDay.LunchIDJson = mealPlanDay.LunchIDJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.LunchIDJson))
                        {
                            mealPlanDay.LunchIDJson = "[]";
                        }

                        mealPlanDay.LunchRecipesIDs.AddRange(ReadList<long>(mealPlanDay.LunchIDJson));
                        mealPlanDay.LunchRecipes.AddRange(recipes.Where(r => mealPlanDay.LunchRecipesIDs.Contains(r.Id)));

                        mealPlanDay.Dinner = recipes.FirstOrDefault(r => r.Id == mealPlanDay.DinnerRecipeId)?.RecipeName ?? string.Empty;
                        mealPlanDay.DinnerNotesJson = mealPlanDay.DinnerNotesJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.DinnerNotesJson))
                        {
                            mealPlanDay.DinnerNotesJson = "[]";
                        }
                        mealPlanDay.DinnerNotes.AddRange(ReadList<string>(mealPlanDay.DinnerNotesJson));
                        mealPlanDay.DinnerIDJson = mealPlanDay.DinnerIDJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.DinnerIDJson))
                        {
                            mealPlanDay.DinnerIDJson = "[]";
                        }
                        mealPlanDay.DinnerRecipesIDs.AddRange(ReadList<long>(mealPlanDay.DinnerIDJson));
                        mealPlanDay.DinnerRecipes.AddRange(recipes.Where(r => mealPlanDay.DinnerRecipesIDs.Contains(r.Id)));
                    }

                    mealPlan.Days = mealPlanDays;
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as appropriate
                Debug.WriteLine($"Error retrieving meal plan: {ex.Message}");
            }

            return mealPlan;
        }

        private static List<T> ReadList<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch (System.Text.Json.JsonException)
            {
                return new List<T>();
            }
        }

        public static async Task DeleteMealPlan()
        {
            await Init();

            //await db.ExecuteAsync("DELETE FROM MealPlan");
            //await db.ExecuteAsync("DELETE FROM MealPlanDay");
        }

        public static async Task SaveMealPlanDay(MealPlanDay day, long mealPlanId, long selectedDayIndex)
        {
            await Init();

            // Find the existing day for the selected day index in the meal plan
            var existingDay = await db.Table<MealPlanDay>().Where(d => d.MealPlanID == mealPlanId && d.MealDayIndex == selectedDayIndex).FirstOrDefaultAsync();

            if (existingDay != null)
            {
                // If the day already exists, update its properties
                existingDay.Breakfast = day.Breakfast;
                existingDay.BreakfastRecipeId = day.BreakfastRecipeId;
                existingDay.BreakfastNotesJson = day.BreakfastNotesJson;
                existingDay.BreakfastIDJson = day.BreakfastIDJson;

                existingDay.Lunch = day.Lunch;
                existingDay.LunchRecipeId = day.LunchRecipeId;
                existingDay.LunchNotesJson = day.LunchNotesJson;
                existingDay.LunchIDJson = day.LunchIDJson;

                existingDay.Dinner = day.Dinner;
                existingDay.DinnerRecipeId = day.DinnerRecipeId;
                existingDay.DinnerNotesJson = day.DinnerNotesJson;
                existingDay.DinnerIDJson = day.DinnerIDJson;

                await db.UpdateAsync(existingDay);
            }
            else
            {
                // If the day doesn't exist, create a new one and insert it into the meal plan
                day.MealPlanID = mealPlanId;
                day.MealDayIndex = selectedDayIndex;

                await db.InsertAsync(day);
            }
        }

        public static async Task UpdateRecipeIsSelected(Recipe recipe)
        {
            await Init();

            await db.ExecuteAsync($"UPDATE {nameof(Recipe)} SET {nameof(Recipe.IsSelected)} = ? WHERE {nameof(Recipe.Id)} = ?", recipe.IsSelected, recipe.Id);
        }

        public static async Task UpdateSnackIsSelected(Snack snack)
        {
            await Init();

            await db.ExecuteAsync($"UPDATE {nameof(Snack)} SET {nameof(Snack.IsSelected)} = ? WHERE {nameof(Snack.Id)} = ?", snack.IsSelected, snack.Id);
        }

        public static async Task UpdateEssentialIsSelected(Staple essential)
        {
            await Init();

            await db.ExecuteAsync($"UPDATE {nameof(Staple)} SET {nameof(Staple.IsSelected)} = ? WHERE {nameof(Staple.Id)} = ?", essential.IsSelected, essential.Id);
        }

        public static void AsignFireBaseUIDToRecipes()
        {
            var recipes = Recipes;

            foreach (Recipe recipe in recipes)
            {
                if (string.IsNullOrEmpty(recipe.FirebaseUID) && CrossFirebaseAuth.Current.CurrentUser != null)
                {
                    recipe.FirebaseUID = CrossFirebaseAuth.Current.CurrentUser.Uid;
                }
            }
        }

        public static async Task DeleteAllRecipes()
        {
            await Init();

            await db.RunInTransactionAsync(connection =>
            {
                connection.Execute("DELETE FROM Ingredient");
                connection.Execute("DELETE FROM Recipe");
            });
            Recipes.Clear();
        }

    }
}
