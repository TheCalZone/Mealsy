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
        private static readonly SemaphoreSlim recipeSync=new(1,1);
        public static string UserId
        {
            get { return AccountSession.UserId;}


        }

        public static async Task SetUserData(string uid, Dictionary<object,object> data)
        {
            AccountSession.Check(uid);
            await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(uid).SetDataAsync(data).WaitAsync(TimeSpan.FromSeconds(20));
            AccountSession.Check(uid);
        }


        public static async Task<AppUser> GetUserAsync(string uid)
        {
            AccountSession.Check(uid);
            var snapshot=await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(uid).GetDocumentSnapshotAsync<AppUser>().WaitAsync(TimeSpan.FromSeconds(20));AccountSession.Check(uid);
            var profile=snapshot.Data??new AppUser();profile.FirebaseUID=uid;profile.Email=AccountSession.Email;return profile;
        }
        public static Task AddRecipe(Recipe recipe)=>UpdateRecipe(recipe);
        public static async Task UpdateRecipe(Recipe recipe)
        {
            if(UserId==null)return;AccountSession.Check(recipe.FirebaseUID);var uid=UserId;
            var data=new Dictionary<object,object>{
                {"recipe_name",recipe.RecipeName??""},{"recipe_method",recipe.RecipeMethod??""},{"description",recipe.Description??""},{"cook_time",recipe.CookTime},
                {"source_url",recipe.SourceUrl??""},{"notes",recipe.Notes??""},{"recipe_image",recipe.RecipeImage??""},{"tags",recipe.Tags??"all"},
                {"serving",recipe.Serving},{"prep_time",recipe.PrepTime},{"rating",recipe.Rating},{"sqlId",recipe.Id},{"last_updated",recipe.LastUpdated},
                {"ingredients",recipe.Ingredients},{"firebaseUID",uid},{"cloud_key",recipe.CloudKey??recipe.Id.ToString()},{"deleted",false}};
            await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(uid).GetCollection("recipes").GetDocument(recipe.CloudKey??recipe.Id.ToString()).SetDataAsync(data).WaitAsync(TimeSpan.FromSeconds(20));AccountSession.Check(uid);
        }
        public static async Task SyncRecipes()
        {
            var uid=UserId;if(uid==null)return;await recipeSync.WaitAsync();try{AccountSession.Check(uid);var remote=await GetAllRecipesAsync();AccountSession.Check(uid);await RecipeService.MergeCloudRecipes(uid,remote);AccountSession.Check(uid);
            foreach(var local in RecipeService.Recipes.Where(r=>r.FirebaseUID==uid).ToList())
            {
                var match=remote.FirstOrDefault(r=>(r.CloudKey??r.Id.ToString())==(local.CloudKey??local.Id.ToString()));
                if(match==null||(!match.IsDeleted&&local.LastUpdated>match.LastUpdated))await UpdateRecipe(local);
            }
            }finally{recipeSync.Release();}
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
                    .GetDocumentsAsync<Recipe>().WaitAsync(TimeSpan.FromSeconds(20));

                foreach (var document in snapshot.Documents)
                {

                    var recipe = new Recipe()
                    {
                        FirestoreID = document.Data.FirestoreID,
                        CloudKey=document.Data.CloudKey??document.Data.FirestoreID??document.Data.Id.ToString(),IsDeleted=document.Data.IsDeleted,
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
                        FirebaseUID = UserId,
                        Id = document.Data.Id,
                    };
                    recipes.Add(recipe);
                }

                return recipes;

            }
            return new();
        }





        public static async Task SyncShoppingLibrary()
        {
            var account=UserId; if (account==null) return;
            var snacks=await GetAllSnacks(); var staples=await GetAllEssentials();
            if (account!=UserId) throw new InvalidOperationException("Your account changed during sync.");
            await RecipeService.MergeShoppingLibrary(account,snacks,staples);
            foreach (var snack in RecipeService.Snacks.Where(s => s.FirebaseUID==account).ToList())
            {
                if (account!=UserId) throw new InvalidOperationException("Your account changed during sync.");
                var remote=snacks.FirstOrDefault(s => (s.CloudKey ?? s.Name)==(snack.CloudKey ?? snack.Name));
                if (remote==null || (!remote.IsDeleted && snack.LastUpdated>remote.LastUpdated)) await UpdateSnack(snack);
            }
            foreach (var staple in RecipeService.Staples.Where(s => s.FirebaseUID==account).ToList())
            {
                if (account!=UserId) throw new InvalidOperationException("Your account changed during sync.");
                var remote=staples.FirstOrDefault(s => (s.CloudKey ?? s.Name)==(staple.CloudKey ?? staple.Name));
                if (remote==null || (!remote.IsDeleted && staple.LastUpdated>remote.LastUpdated)) await UpdateStaple(staple);
            }
        }

        public static async Task<List<Snack>> GetAllSnacks()
        {
            if (UserId != null)
            {
                List<Snack> snacks = new();

                var snapshot = await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocumentsAsync<Snack>().WaitAsync(TimeSpan.FromSeconds(20));

                foreach (var doc in snapshot.Documents)
                {
                    var snack = new Snack
                    {
                        FirebaseUID = doc.Data.FirebaseUID, LastUpdated = doc.Data.LastUpdated,
                        Name = doc.Data.Name,
                        Tag = doc.Data.Tag,
                        Quantity = doc.Data.Quantity, Note = doc.Data.Note, IsDeleted = doc.Data.IsDeleted, CloudKey = doc.Data.CloudKey ?? doc.Data.FirestoreID ?? doc.Data.Name,
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
                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocument(snack.CloudKey ?? snack.FirestoreID ?? snack.Name).SetDataAsync(snack).WaitAsync(TimeSpan.FromSeconds(20));
            }
        }

        public static async Task DeleteSnack(Snack snack)
        {
            if (UserId != null)
            {
                var key=snack.CloudKey ?? snack.FirestoreID ?? snack.Name;
                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocument(key)
                    .SetDataAsync(new Dictionary<object,object> { {"name",snack.Name}, {"cloudKey",key}, {"firebaseUID",UserId}, {"deleted",true}, {"lastUpdated",DateTimeOffset.UtcNow} }).WaitAsync(TimeSpan.FromSeconds(20));
            }
        }

        public static async Task UpdateSnack( Snack snack)
        {
            if (UserId != null)
            {
                var updatedSnack = new Dictionary<object, object> {
                { "name", snack.Name}, { "deleted",false},
                { "tag",snack.Tag},
                { "cloudKey",snack.CloudKey ?? snack.FirestoreID ?? snack.Name},
                { "quantity",snack.Quantity ?? ""}, { "note",snack.Note ?? ""},
                {"lastUpdated",snack.LastUpdated },
                {"sqlId",snack.Id},
                {"firebaseUID",UserId}
                };

                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("snacks").GetDocument(snack.CloudKey ?? snack.FirestoreID ?? snack.Name).SetDataAsync(updatedSnack).WaitAsync(TimeSpan.FromSeconds(20));
            }
        }

        public static async Task<List<Staple>> GetAllEssentials()
        {
            if (UserId != null)
            {
                string userId = CrossFirebaseAuth.Current.CurrentUser.Uid;
                List<Staple> essentials = new();

                var snapshot = await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("staples").GetDocumentsAsync<Staple>().WaitAsync(TimeSpan.FromSeconds(20));

                foreach (var doc in snapshot.Documents)
                {
                    var essential = new Staple
                    {
                        FirebaseUID = doc.Data.FirebaseUID, LastUpdated = doc.Data.LastUpdated,
                        Name = doc.Data.Name,
                        Tag = doc.Data.Tag,
                        Quantity = doc.Data.Quantity, Note = doc.Data.Note, IsDeleted = doc.Data.IsDeleted, CloudKey = doc.Data.CloudKey ?? doc.Data.FirestoreID ?? doc.Data.Name,
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
                    .GetDocument(staple.CloudKey ?? staple.FirestoreID ?? staple.Name)
                    .SetDataAsync(staple).WaitAsync(TimeSpan.FromSeconds(20));
            }
        }

        public static async Task DeleteStaple(Staple staple)
        {
            if (UserId != null)
            {
                var key=staple.CloudKey ?? staple.FirestoreID ?? staple.Name;
                await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(UserId).GetCollection("staples").GetDocument(key)
                    .SetDataAsync(new Dictionary<object,object> { {"name",staple.Name}, {"cloudKey",key}, {"firebaseUID",UserId}, {"deleted",true}, {"lastUpdated",DateTimeOffset.UtcNow} }).WaitAsync(TimeSpan.FromSeconds(20));
            }
        }

        public static async Task UpdateStaple(Staple staple)
        {
            if (UserId != null)
            {
                var updatedStaple = new Dictionary<object, object>
            {
                { "name", staple.Name }, { "deleted",false },
                { "tag", staple.Tag },
                { "cloudKey", staple.CloudKey ?? staple.FirestoreID ?? staple.Name },
                { "quantity", staple.Quantity ?? "" }, { "note", staple.Note ?? "" },
                { "lastUpdated", staple.LastUpdated },
                { "sqlId", staple.Id },
                { "firebaseUID", UserId }
            };

                await CrossFirebaseFirestore.Current
                    .GetCollection("users")
                    .GetDocument(UserId)
                    .GetCollection("staples")
                    .GetDocument(staple.CloudKey ?? staple.FirestoreID ?? staple.Name)
                    .SetDataAsync(updatedStaple).WaitAsync(TimeSpan.FromSeconds(20));

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
                    .GetDocumentsAsync<Recipe>().WaitAsync(TimeSpan.FromSeconds(20));

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
            var recipe=await RecipeService.GetRecipe(recipeSqlId);if(recipe!=null)await DeleteRecipe(recipe);
        }
        public static async Task DeleteRecipe(Recipe recipe)
        {
            if(UserId==null)return;AccountSession.Check(recipe.FirebaseUID);var uid=UserId;await recipeSync.WaitAsync();try{AccountSession.Check(uid);
            await CrossFirebaseFirestore.Current.GetCollection("users").GetDocument(uid).GetCollection("recipes").GetDocument(recipe.CloudKey??recipe.Id.ToString())
                .SetDataAsync(new Dictionary<object,object>{{"cloud_key",recipe.CloudKey??recipe.Id.ToString()},{"firebaseUID",uid},{"deleted",true},{"last_updated",DateTimeOffset.UtcNow}}).WaitAsync(TimeSpan.FromSeconds(20));AccountSession.Check(uid);
            }finally{recipeSync.Release();}
        }

    }
}
