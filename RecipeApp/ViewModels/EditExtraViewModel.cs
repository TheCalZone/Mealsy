using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels
{
    public class EditExtraViewModel
    {
        public Snack Snack { get; set; }
        public Snack OgSnack { get; set; }

        public Staple Staple { get; set; }
        public Staple OgStaple { get; set; }

        public AsyncCommand SaveCommand { get; }

        public bool StapleNull => Snack == null;
        public bool SnackNull => Staple == null;



        public EditExtraViewModel(Snack snack)
        {
            OgSnack = snack;
            Snack = snack;
            SaveCommand = new AsyncCommand(SaveExtra);
        }
        public EditExtraViewModel(Staple staple)
        {
            OgStaple = staple;
            Staple = staple;
            SaveCommand = new AsyncCommand(SaveExtra);
        }
        public EditExtraViewModel()
        {
        }

        public async Task SaveExtra()
        {
            if (Snack == null)
            {
                await RecipeService.UpdateStaple(Staple);
            }
            if (Staple == null)
            {
                await RecipeService.UpdateSnack(Snack);
                await FirestoreService.UpdateSnack(Snack);
            }

            await Shell.Current.Navigation.PopAsync();
        }

    }
}
