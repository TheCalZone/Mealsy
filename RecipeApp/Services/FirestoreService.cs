using RecipeApp.Models;
using Plugin.Firebase.Firestore;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Plugin.Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace RecipeApp.Services
{
    public static class FirestoreService
    {
        public static string UserId
        {
            get { return CrossFirebaseAuth.Current.CurrentUser?.Uid;}


        }

        public static async Task SetUserData(string uid, Dictionary<object,object> data)
        {
            await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(uid).SetDataAsync(data);
        }


        public static async Task<AppUser> GetUserAsync(string uid)
        {

            if (UserId != null)
            {
                var documentRef = CrossFirebaseFirestore.Current.GetCollection("users");

                //GetDocumentsAsync("users").GetDocumentSnapshotAsync<Dictionary<object, object>>();

                var userdict = await documentRef.GetDocument(uid).GetDocumentSnapshotAsync<AppUser>();


                try
                {
                    if (documentRef != null)
                    {
                        return new AppUser
                        {
                            FirstName = userdict.Data.FirstName,
                            LastName = userdict.Data.LastName,
                            Email = userdict.Data.Email,
                            DietRestrictions = userdict.Data.DietRestrictions,
                            FavouriteCuisines = userdict.Data.FavouriteCuisines,
                            FirebaseUID = userdict.Data.FirebaseUID
                        };
                    }
                    else
                    {
                        return new AppUser();
                    }
                }
                catch (Exception ex)
                {
                    // Handle or log the exception as appropriate
                    // For example:
                    Console.WriteLine($"Error fetching user data: {ex.Message}");
                    return new AppUser();
                }
            }

            return new();

        }

        public static async Task AddRecipe(Recipe recipe)
        {
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                var recipeToAdd = new Dictionary<object, object>
                {
                    {"recipe_name", recipe.RecipeName },
                    {"recipe_method", recipe.RecipeMethod},
                    {"description", recipe.Description ?? ""},
                    {"cook_time", recipe.CookTime},
                    {"source_url", recipe.SourceUrl ?? ""},
                    {"notes", recipe.Notes ?? ""},
                    {"recipe_image", recipe.RecipeImage},
                    {"tags", recipe.Tags},
                    {"serving", recipe.Serving },
                    {"prep_time", recipe.PrepTime },
                    {"rating",recipe.Rating },
                    {"sqlId", recipe.Id },
                    {"last_updated", recipe.LastUpdated },
                    {"ingredients", recipe.Ingredients },
                    {"firestoreUID", CrossFirebaseAuth.Current.CurrentUser?.Uid ?? ""},
                    {"firebaseUID", CrossFirebaseAuth.Current.CurrentUser?.Uid ?? ""}
                };
                //await CrossCloudFirestore.Current
                //                            .Instance
                //                            .Collection("users")
                //                            .Document(userId)
                //                            .Collection("recipes")
                //                            .AddAsync(recipe);

                    await CrossFirebaseFirestore
                    .Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("recipes")
                    .GetDocument(recipe.Id.ToString())
                    .SetDataAsync(recipeToAdd);

            }
            else
            {
                var toast = Toast.Make("Unable to save recipes to cloud, please sign in first.", ToastDuration.Short, 14);
                await toast.Show();
            }
        }

        public static async Task UpdateRecipe(Recipe recipe)
        {
            if (UserId != null)
            {
                var updatedRecipe = new Dictionary<object, object>
                {
                    {"recipe_name", recipe.RecipeName },
                    {"recipe_method", recipe.RecipeMethod},
                    {"description", recipe.Description ?? ""},
                    {"cook_time", recipe.CookTime},
                    {"source_url", recipe.SourceUrl ?? ""},
                    {"notes", recipe.Notes ?? ""},
                    {"recipe_image", recipe.RecipeImage},
                    {"tags", recipe.Tags},
                    {"serving", recipe.Serving },
                    {"prep_time", recipe.PrepTime },
                    {"rating",recipe.Rating },
                    {"sqlId", recipe.Id },
                    {"last_updated", recipe.LastUpdated },
                    {"ingredients", recipe.Ingredients },
                    {"firestoreUID", CrossFirebaseAuth.Current.CurrentUser?.Uid ?? ""},
                {"firebaseUID", CrossFirebaseAuth.Current.CurrentUser?.Uid ?? ""}
                };

                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("recipes").GetDocument(recipe.Id.ToString()).SetDataAsync(updatedRecipe);
            }
        }

        public static async Task<List<Recipe>> GetAllRecipesAsync()
        {
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                List<Recipe> recipes = new List<Recipe>();

                var snapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("recipes")
                    .GetDocumentsAsync<Recipe>();

                foreach (var document in snapshot.Documents)
                {

                    var recipe = new Recipe()
                    {
                        FirestoreID = document.Data.FirestoreID,
                        Tags = document.Data.Tags,
                        RecipeName = document.Data.RecipeName,
                        RecipeMethod = document.Data.RecipeMethod,
                        Description = document.Data.Description,
                        CookTime = document.Data.CookTime,
                        SourceUrl = document.Data.SourceUrl,
                        Notes = document.Data.Notes,
                        RecipeImage = document.Data.RecipeImage,
                        Serving = document.Data.Serving,
                        PrepTime = document.Data.PrepTime,
                        Rating = document.Data.Rating,
                        Ingredients = document.Data.FirebaseIngredients ?? new(),
                        LastUpdated = document.Data.LastUpdated,
                        FirebaseUID = document.Data.FirebaseUID,
                        Id = document.Data.Id,
                    };
                    recipes.Add(recipe);
                }

                return recipes;

            }
            return new();
        }





        public static async Task<List<Snack>> GetAllSnacks()
        {
            if (UserId != null)
            {
                List<Snack> snacks = new();

                var snapshot = await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocumentsAsync<Snack>();

                foreach (var doc in snapshot.Documents)
                {
                    var snack = new Snack
                    {
                        FirebaseUID = doc.Data.FirebaseUID, LastUpdated = doc.Data.LastUpdated,
                        Name = doc.Data.Name,
                        Tag = doc.Data.Tag,
                        Id = doc.Data.Id,
                    };

                    snacks.Add(snack);
                }
                return snacks;

            }
            else
            {
                var toast = Toast.Make("Unable to retrieve snacks, please sign in first.", ToastDuration.Short, 14);
                await toast.Show();
                return new();
            }

        }

        public static async Task AddSnack(Snack snack)
        {
            if (UserId != null)
            {
                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocument(snack.Name).SetDataAsync(snack);
            }
        }

        public static async Task DeleteSnack(Snack snack)
        {
            if (UserId != null)
            {
                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocument(snack.Name).DeleteDocumentAsync();
            }
        }

        public static async Task UpdateSnack( Snack snack)
        {
            if (UserId != null)
            {
                var updatedSnack = new Dictionary<object, object> {
                { "name", snack.Name},
                { "tag",snack.Tag},
                {"lastUpdated",snack.LastUpdated },
                {"sqlId",snack.Id},
                {"firebaseUID",UserId}
                };

                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocument(snack.Name).SetDataAsync(updatedSnack);
            }
        }

        public static async Task<List<Staple>> GetAllEssentials()
        {
            if (UserId != null)
            {
                string userId = CrossFirebaseAuth.Current.CurrentUser.Uid;
                List<Staple> essentials = new();

                var snapshot = await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("staples").GetDocumentsAsync<Staple>();

                foreach (var doc in snapshot.Documents)
                {
                    var essential = new Staple
                    {
                        FirebaseUID = doc.Data.FirebaseUID, LastUpdated = doc.Data.LastUpdated,
                        Name = doc.Data.Name,
                        Tag = doc.Data.Tag,
                        Id = doc.Data.Id,
                    };

                    essentials.Add(essential);
                }

                return essentials;
            }
                else
                {
                    var toast = Toast.Make("Unable to retrieve essentials, please sign in first.", ToastDuration.Short, 14);
                    await toast.Show();
                }

                return new();
        }

        public static async Task AddStaple(Staple staple)
        {
            if (UserId != null)
            {
                await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("staples")
                    .GetDocument(staple.Name)
                    .SetDataAsync(staple);
            }
        }

        public static async Task DeleteStaple(Staple staple)
        {
            if (UserId != null)
            {
                await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("staples")
                    .GetDocument(staple.Name)
                    .DeleteDocumentAsync();
            }
        }

        public static async Task UpdateStaple(Staple staple)
        {
            if (UserId != null)
            {
                var updatedStaple = new Dictionary<object, object>
            {
                { "name", staple.Name },
                { "tag", staple.Tag },
                { "lastUpdated", staple.LastUpdated },
                { "sqlId", staple.Id },
                { "firebaseUID", UserId }
            };

                await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("staples")
                    .GetDocument(staple.Name)
                    .SetDataAsync(updatedStaple);

            }
        }

        public static async Task DeleteAllRecipes()
        {
            if (UserId != null)
            {
                var snapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("recipes")
                    .GetDocumentsAsync<Recipe>();

                foreach (var doc in snapshot.Documents)
                {
                    await CrossFirebaseFirestore.Current
                        .GetCollection("users")
                        .GetDocument(UserId)
                        .GetCollection("recipes")
                        .GetDocument(doc.Data.Id.ToString())
                        .DeleteDocumentAsync();
                }
            }
        }

        public static async Task DeleteRecipe(long recipeSqlId)
        {
            if (UserId != null)
            {
                var snapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("recipes")
                    .GetDocumentsAsync<Recipe>();

                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Data.Id == recipeSqlId)
                    {
                        await CrossFirebaseFirestore.Current
                            .GetCollection("users")
                            .GetDocument(UserId)
                            .GetCollection("recipes")
                            .GetDocument(doc.Data.Id.ToString())
                            .DeleteDocumentAsync();
                    }
                }
            }
        }

    }
}
