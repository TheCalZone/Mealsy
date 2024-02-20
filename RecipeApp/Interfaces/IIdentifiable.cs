using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeApp.Interfaces
{
    internal interface IIdentifiable
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FirebaseUID{ get; set; }

        public DateTimeOffset LastUpdated { get; set; }
    }
}
