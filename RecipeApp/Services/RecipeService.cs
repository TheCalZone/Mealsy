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
        static readonly SemaphoreSlim initializationLock = new(1, 1);

        static async Task Init()
        {
            await initializationLock.WaitAsync();
            try
            {
                if (db != null) return;
                var connection = new SQLiteAsyncConnection(DatabasePath, Flags);
                try
                {
                    await connection.CreateTableAsync<Recipe>();
                    await connection.CreateTableAsync<Ingredient>();
                    await connection.CreateTableAsync<MealPlan>();
                    await connection.CreateTableAsync<MealPlanDay>();
                    await connection.CreateTableAsync<Snack>();
                    await connection.CreateTableAsync<Staple>();
                    await connection.CreateTableAsync<ShoppingList>();
                    db = connection;
                }
                catch
                {
                    await connection.CloseAsync();
                    throw;
                }
            }
            finally { initializationLock.Release(); }
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
            ArgumentNullException.ThrowIfNull(recipeToAdd);
            await Init();
            if (recipeToAdd.Id != 0 && await db.FindAsync<Recipe>(recipeToAdd.Id) != null) return;
            recipeToAdd.Ingredients ??= new();
            if (recipeToAdd.LastUpdated == default) recipeToAdd.LastUpdated = DateTimeOffset.UtcNow;
            await db.InsertWithChildrenAsync(recipeToAdd);
            if (!Recipes.Any(r => r.Id == recipeToAdd.Id)) Recipes.Add(recipeToAdd);
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

        public static async Task CreateMealPlan(string account = null)
        {
            await Init();
            await db.RunInTransactionAsync(connection =>
            {
                var key=account==null ? "" : "user:"+account;
                var plan = connection.Table<MealPlan>().ToList().FirstOrDefault(p => (p.AccountKey ?? "")==key);
                if (plan == null)
                {
                    plan = new MealPlan { Name = "Weekly plan", AccountKey=key, WeekStart=DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek) };
                    connection.Insert(plan);
                }
                var existing = connection.Table<MealPlanDay>().Where(d => d.MealPlanID == plan.Id).ToList();
                foreach (var index in Enumerable.Range(0, 7))
                    if (!existing.Any(d => d.MealDayIndex == index))
                        connection.Insert(new MealPlanDay { MealPlanID = plan.Id, MealDayIndex = index,
                            MealDay = ((DayOfWeek)index).ToString() });
            });
        }

        public static async Task UpdateRecipeAndIngredientsAsync(Recipe recipe, List<Ingredient> ingredients)
        {
            ArgumentNullException.ThrowIfNull(recipe);
            await Init();
            recipe.LastUpdated = DateTimeOffset.UtcNow;
            var updatedIngredients = ingredients ?? new List<Ingredient>();
            await db.RunInTransactionAsync(connection =>
            {
                if (connection.Find<Recipe>(recipe.Id) == null)
                    throw new InvalidOperationException("The recipe no longer exists.");
                connection.Update(recipe);
                connection.Execute("DELETE FROM Ingredient WHERE RecipeId = ?", recipe.Id);
                foreach (var ingredient in updatedIngredients)
                {
                    ingredient.Id = 0;
                    ingredient.Tag ??= string.Empty;
                    ingredient.RecipeId = recipe.Id;
                    connection.Insert(ingredient);
                }
            });
            recipe.Ingredients = updatedIngredients;
            var index = Recipes.FindIndex(r => r.Id == recipe.Id);
            if (index >= 0) Recipes[index] = recipe;
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
            await db.RunInTransactionAsync(connection =>
            {
                connection.Execute("DELETE FROM Ingredient WHERE RecipeId = ?", id);
                connection.Delete<Recipe>(id);
            });
            Recipes.RemoveAll(r => r.Id == id);
        }
        public static async Task<Recipe> GetRecipe(long id)
        {
            await Init();
            var recipe = await db.FindAsync<Recipe>(id);
            if (recipe != null)
                recipe.Ingredients = await db.Table<Ingredient>().Where(i => i.RecipeId == id).ToListAsync();
            return recipe;
        }

        public static async Task GetRecipesForLocal()
        {
            await Init();
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
            return await db.GetAllWithChildrenAsync<Recipe>();
        }


        public static async Task MergeShoppingLibrary(string account, IEnumerable<Snack> snacks, IEnumerable<Staple> staples)
        {
            if (string.IsNullOrWhiteSpace(account)) throw new ArgumentException("An account is required.", nameof(account));
            await GetSnacksForLocal(); await GetStaplesForLocal();
            foreach (var incoming in snacks)
            {
                var key=incoming.CloudKey ?? incoming.FirestoreID ?? incoming.Name;
                var local=Snacks.FirstOrDefault(s => s.FirebaseUID==account && ((s.CloudKey ?? s.Name)==key));
                if (incoming.IsDeleted) { if (local!=null) await DeleteSnack(local); continue; }
                if (local!=null && local.LastUpdated>incoming.LastUpdated) continue;
                var item=new Snack { Id=local?.Id ?? 0, Name=incoming.Name, Tag=incoming.Tag, Quantity=incoming.Quantity ?? "", Note=incoming.Note ?? "", CloudKey=key, FirebaseUID=account, LastUpdated=incoming.LastUpdated };
                if (local==null) await InsertSnack(item); else await UpdateSnack(item);
            }
            foreach (var incoming in staples)
            {
                var key=incoming.CloudKey ?? incoming.FirestoreID ?? incoming.Name;
                var local=Staples.FirstOrDefault(s => s.FirebaseUID==account && ((s.CloudKey ?? s.Name)==key));
                if (incoming.IsDeleted) { if (local!=null) await DeleteStaple(local); continue; }
                if (local!=null && local.LastUpdated>incoming.LastUpdated) continue;
                var item=new Staple { Id=local?.Id ?? 0, Name=incoming.Name, Tag=incoming.Tag, Quantity=incoming.Quantity ?? "", Note=incoming.Note ?? "", CloudKey=key, FirebaseUID=account, LastUpdated=incoming.LastUpdated };
                if (local==null) await InsertStaple(item); else await UpdateStaple(item);
            }
        }

        #region Snack Actions
        public static async Task DeleteSnack(Snack snack)
        {
            await Init();
            await db.DeleteAsync(snack);
            Snacks.RemoveAll(s => s.Id == snack.Id);
        }
        public static async Task InsertSnack(Snack snack)
        {
            await Init();
            await db.InsertAsync(snack);
            Snacks.Add(snack);
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
            await db.UpdateAsync(snack);
            var index = Snacks.FindIndex(s => s.Id == snack.Id);
            if (index >= 0) Snacks[index] = snack;
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
            Staples.RemoveAll(s => s.Id == staple.Id);
        }
        public static async Task InsertStaple(Staple staple)
        {
            await Init();
            await db.InsertAsync(staple);
            Staples.Add(staple);
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
            await db.UpdateAsync(staple);
            var index = Staples.FindIndex(s => s.Id == staple.Id);
            if (index >= 0) Staples[index] = staple;
        }
        public static async Task<Staple> GetStapleByName(string stapleName)
        {
            await Init();
            var tempStaple = await db.Table<Staple>().Where(d => d.Name == stapleName).FirstOrDefaultAsync();
            return tempStaple;
        }
        #endregion

        public static async Task<MealPlan> GetMealPlan(string account = null)
        {
            await Init();

            MealPlan mealPlan = null;
            try
            {
                var key=account==null ? "" : "user:"+account;
                mealPlan = (await db.Table<MealPlan>().ToListAsync()).FirstOrDefault(p => (p.AccountKey ?? "")==key);
                if (mealPlan != null)
                {
                    var mealPlanDays = await db.Table<MealPlanDay>().Where(d => d.MealPlanID == mealPlan.Id).ToListAsync();
                    var recipeIds = mealPlanDays.SelectMany(d => new[] { d.BreakfastRecipeId, d.LunchRecipeId, d.DinnerRecipeId }).Distinct();
                    var recipes = await db.Table<Recipe>().ToListAsync();

                    foreach (var mealPlanDay in mealPlanDays)
                    {
                        mealPlanDay.Breakfast = recipes.FirstOrDefault(r => r.Id == mealPlanDay.BreakfastRecipeId)?.RecipeName ?? "Recipe not found";
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

                        mealPlanDay.Lunch = recipes.FirstOrDefault(r => r.Id == mealPlanDay.LunchRecipeId)?.RecipeName ?? "Recipe not found";
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

                        mealPlanDay.Dinner = recipes.FirstOrDefault(r => r.Id == mealPlanDay.DinnerRecipeId)?.RecipeName ?? "Recipe not found";
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

                    if (mealPlan.WeekStart==default) mealPlan.WeekStart=DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    foreach (var day in mealPlanDays) MealPlanning.NormalizeLegacy(day, recipes);
                    mealPlan.Days = mealPlanDays.OrderBy(d => d.MealDayIndex).ToList();
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as appropriate
                Debug.WriteLine($"Error retrieving meal plan: {ex.Message}");
            }

            return mealPlan;
        }

        public static async Task SaveMealPlanDays(IEnumerable<MealPlanDay> days,long planId)
        {
            await Init();var copies=days.Select(d=>MealPlanning.Copy(d)).ToList();
            await db.RunInTransactionAsync(connection=>{
                foreach(var day in copies)
                {
                    var existing=connection.Table<MealPlanDay>().FirstOrDefault(d=>d.MealPlanID==planId&&d.MealDayIndex==day.MealDayIndex)??throw new InvalidOperationException("A plan day is missing.");
                    day.Id=existing.Id;day.MealPlanID=planId;connection.Update(day);
                }
            });
        }

        public static async Task SaveMealPlanHeader(MealPlan plan)
        {
            await Init(); await db.UpdateAsync(plan);
        }

        private static List<T> ReadList<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? new(); }
            catch (System.Text.Json.JsonException) { return new(); }
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
                existingDay.BreakfastPortionsJson=day.BreakfastPortionsJson; existingDay.LunchPortionsJson=day.LunchPortionsJson; existingDay.DinnerPortionsJson=day.DinnerPortionsJson;
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
            var uid = CrossFirebaseAuth.Current.CurrentUser?.Uid;
            if (string.IsNullOrEmpty(uid)) return;
            foreach (var recipe in Recipes.Where(r => string.IsNullOrEmpty(r.FirebaseUID)))
                recipe.FirebaseUID = uid;
        }

        public static async Task DeleteAllRecipes()
        {
            await Init();
            await db.RunInTransactionAsync(connection =>
            {
                connection.DeleteAll<Ingredient>();
                connection.DeleteAll<Recipe>();
            });
            Recipes.Clear();
        }

    }
}
