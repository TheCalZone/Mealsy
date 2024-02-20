using RecipeApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeApp.Interfaces
{
    public interface IFirebaseService
    {
        Task AddRecipe(Recipe recipe);
        // Other Firebase-related methods go here...
    }
}
