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
        public static List<Recipe> Recipes = null;
        public static List<Snack> Snacks = null;
        public static List<Staple> Staples = null;

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

        static async Task Init()
        {
            if (db != null)
            {
                return;
            }

            //if (!File.Exists(databasePath))
            //{
            //    // Log a message or take appropriate action
            //    Console.WriteLine("Database file not found: " + databasePath);
            //    return;
            //}


            db = new SQLiteAsyncConnection(DatabasePath, Flags);
            try
            {
                    await db.CreateTableAsync<Recipe>();
                    try
                    {
                        await db.CreateTableAsync<Ingredient>();
                        // Consider adding this for all related tables, not just Recipe
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create Ingredient table: {ex.Message}");
                        // Handle the exception appropriately
                    }
                    await db.CreateTableAsync<MealPlan>();
                    await db.CreateTableAsync<MealPlanDay>();
                    await db.CreateTableAsync<Snack>();
                    await db.CreateTableAsync<Staple>();
                    await db.CreateTableAsync<ShoppingList>();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating tables" + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
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

            var prexisintRecipe = await db.Table<Recipe>().Where(x => x.Id == recipeToAdd.Id).FirstOrDefaultAsync();

            if (prexisintRecipe == null)
            {
                try
                {
                    await db.InsertWithChildrenAsync(recipeToAdd);
                    Recipes.Add(recipeToAdd);
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
            }
            else
            {
                Debug.WriteLine("Recipe already exists");
            }    
            //await db.ExecuteAsync("ALTER TABLE Ingredient ADD COLUMN Quantity STRING");

            //// Save recipe to the local SQLite database
            //var recipe = new Recipe
            //{
            //    RecipeName = recipeToAdd.RecipeName,
            //    Ingredients = recipeToAdd.Ingredients,
            //    RecipeMethod = recipeToAdd.RecipeMethod,
            //    Tags = recipeToAdd.Tags,
            //    RecipeImage = recipeToAdd.RecipeImage,
            //    Serving = recipeToAdd.Serving,
            //    PrepTime = recipeToAdd.PrepTime,
            //    Rating = recipeToAdd.Rating
            //};
        }




        public static async Task AddSnack(string name)
        {
            await Init();
            var snack = new Snack { Name = name };
            await db.InsertAsync(snack);
        }

        public static async Task AddStaple(string name)
        {
            await Init();
            var staple = new Staple { Name = name };
            await db.InsertAsync(staple);
        }

        public static async Task CreateMealPlan()
        {
            await Init();

            await db.CreateTableAsync<MealPlan>();
            await db.CreateTableAsync<MealPlanDay>();

            // Attempt to load the existing meal plan with its days eagerly loaded
            var existingMealPlan = await db.Table<MealPlan>().FirstOrDefaultAsync();
            if (existingMealPlan != null)
            {
                // Eagerly load the MealPlanDay entities related to this MealPlan
                await db.GetChildrenAsync(existingMealPlan, true);
            }

            if (existingMealPlan != null && existingMealPlan.Days != null && existingMealPlan.Days.Count == 7)
            {
                // Meal plan already exists and has 7 days, no further action needed
                return;
            }
            else
            {
                // If there's no existing meal plan or it doesn't have 7 days
                // Consider clearing any existing incomplete meal plans if necessary
                // await db.DeleteAsync(existingMealPlan); // Be cautious with delete operations

                // Create and insert the new MealPlan and its Days
                var mealPlan = new MealPlan { Name = "Default 7 days" };
                await db.InsertAsync(mealPlan); // Insert MealPlan to generate Id

                var days = Enumerable.Range(0, 7).Select(index =>
                    new MealPlanDay
                    {
                        MealDay = ((DayOfWeek)index).ToString(),
                        MealDayIndex = index,
                        MealPlanID = mealPlan.Id // Assign generated Id
                    }).ToList();

                foreach (var day in days)
                {
                    await db.InsertAsync(day); // Insert each MealPlanDay
                }
            }
        }

        public static async Task UpdateRecipeAndIngredientsAsync(Recipe recipe, List<Ingredient> ingredients)
        {
            await Init();

            await db.CreateTableAsync<Recipe>();
            try
            {
                try
                {
                    await db.UpdateAsync(recipe); // Update the recipe
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


                // Delete the existing ingredients for the recipe
                await db.ExecuteAsync("DELETE FROM Ingredient WHERE RecipeId = ?", recipe.Id);

                // Insert the updated ingredients
                foreach (var ingredient in ingredients)
                {
                    ingredient.Tag ??= String.Empty;
                    ingredient.RecipeId = recipe.Id; // Ensure the RecipeId is set
                    await db.InsertAsync(ingredient);
                }
            }
            catch (Exception ex)
            {
                // Log the exception to see if there are any errors during the update process
                Debug.WriteLine("Error updating recipe and ingredients: " + ex.Message);
            }
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
            await db.DeleteAsync<Recipe>(id);
            Recipes.Remove(Recipes.FirstOrDefault(r => r.Id == id));
        }
        public static async Task<Recipe> GetRecipe(long id)
        {
            await Init();
            await db.CreateTableAsync<Recipe>();

            var recipe = await db.Table<Recipe>().FirstOrDefaultAsync(c => c.Id == id);
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
                    var recipeIds = mealPlanDays.SelectMany(d => new[] { d.BreakfastRecipeId, d.LunchRecipeId, d.DinnerRecipeId }).Distinct();
                    var recipes = await db.Table<Recipe>().Where(r => recipeIds.Contains(r.Id)).ToListAsync();

                    foreach (var mealPlanDay in mealPlanDays)
                    {
                        mealPlanDay.Breakfast = recipes.FirstOrDefault(r => r.Id == mealPlanDay.BreakfastRecipeId)?.RecipeName ?? "Recipe not found";
                        mealPlanDay.BreakfastNotesJson = mealPlanDay.BreakfastNotesJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.BreakfastNotesJson))
                        {
                            mealPlanDay.BreakfastNotesJson = "[]";
                        }
                        mealPlanDay.BreakfastNotes.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<string>>(mealPlanDay.BreakfastNotesJson));
                        mealPlanDay.BreakfastIDJson = mealPlanDay.BreakfastIDJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.BreakfastIDJson))
                        {
                            mealPlanDay.BreakfastIDJson = "[]";
                        }
                        mealPlanDay.BreakfastRecipesIDs.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<long>>(mealPlanDay.BreakfastIDJson));
                        mealPlanDay.BreakfastRecipes.AddRange(recipes.Where(r => mealPlanDay.BreakfastRecipesIDs.Contains(r.Id)));

                        mealPlanDay.Lunch = recipes.FirstOrDefault(r => r.Id == mealPlanDay.LunchRecipeId)?.RecipeName ?? "Recipe not found";
                        mealPlanDay.LunchNotesJson = mealPlanDay.LunchNotesJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.LunchNotesJson))
                        {
                            mealPlanDay.LunchNotesJson = "[]";
                        }

                        mealPlanDay.LunchNotes.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<string>>(mealPlanDay.LunchNotesJson));
                        mealPlanDay.LunchIDJson = mealPlanDay.LunchIDJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.LunchIDJson))
                        {
                            mealPlanDay.LunchIDJson = "[]";
                        }

                        mealPlanDay.LunchRecipesIDs.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<long>>(mealPlanDay.LunchIDJson));
                        mealPlanDay.LunchRecipes.AddRange(recipes.Where(r => mealPlanDay.LunchRecipesIDs.Contains(r.Id)));

                        mealPlanDay.Dinner = recipes.FirstOrDefault(r => r.Id == mealPlanDay.DinnerRecipeId)?.RecipeName ?? "Recipe not found";
                        mealPlanDay.DinnerNotesJson = mealPlanDay.DinnerNotesJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.DinnerNotesJson))
                        {
                            mealPlanDay.DinnerNotesJson = "[]";
                        }
                        mealPlanDay.DinnerNotes.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<string>>(mealPlanDay.DinnerNotesJson));
                        mealPlanDay.DinnerIDJson = mealPlanDay.DinnerIDJson ?? "[]";
                        if (string.IsNullOrWhiteSpace(mealPlanDay.DinnerIDJson))
                        {
                            mealPlanDay.DinnerIDJson = "[]";
                        }
                        mealPlanDay.DinnerRecipesIDs.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<long>>(mealPlanDay.DinnerIDJson));
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
                if (!string.IsNullOrEmpty(recipe.FirebaseUID))
                {
                    recipe.FirebaseUID ??= CrossFirebaseAuth.Current.CurrentUser.Uid;
                }
            }
        }

        public static async Task DeleteAllRecipes()
        {
            await Init();

            await db.ExecuteAsync($"DELETE FROM {nameof(Recipe)}");
            Recipes.Clear();
        }

    }
}
