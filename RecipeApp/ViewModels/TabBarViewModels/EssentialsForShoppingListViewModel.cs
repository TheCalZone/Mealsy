using MvvmHelpers;
using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;
using System.Diagnostics;

namespace RecipeApp.ViewModels.TabBarViewModels
{
    class EssentialsForShoppingListViewModel : BaseViewModel
    {
        private ObservableRangeCollection<Staple> staples;
        public ObservableRangeCollection<Staple> Staples { get => staples; set => SetProperty(ref staples, value); }
        public AsyncCommand<Staple> EditStapleCommand { get; }
        public AsyncCommand AddStapleCommand { get; }
        public AsyncCommand<Staple> DeleteStaplesCommand { get; }
        public AsyncCommand RefreshCommand { get; }

        public EssentialsForShoppingListViewModel()
        {
            Staples = new ObservableRangeCollection<Staple>();
            EditStapleCommand = new AsyncCommand<Staple>(EditStaple);
            AddStapleCommand = new AsyncCommand(AddStaple);
            DeleteStaplesCommand = new AsyncCommand<Staple>(DeleteStaple);
            RefreshCommand = new AsyncCommand(Refresh);

        }

        async Task EditStaple(Staple staple)
        {
            var route = $"{nameof(EditExtraPage)}?StapleName={staple.Name}";
            await Shell.Current.GoToAsync(route);
        }

        async Task AddStaple()
        {
            string name = await Application.Current.MainPage.DisplayPromptAsync("New Essential", "Name:", "Add", "Cancel");
            if (!string.IsNullOrWhiteSpace(name))
            {
                string tag = await Application.Current.MainPage.DisplayPromptAsync("New Essential", "Tag:", "Add", "Cancel");
                Staple staple = new Staple { Name = name, Tag = tag };
                await RecipeService.InsertStaple(staple);
                Staples.Clear();
                Staples.AddRange(RecipeService.Staples);
            }
        }

        async Task DeleteStaple(Staple staple)
        {
            await RecipeService.DeleteStaple(staple);
            Staples.Clear();
            var staples = RecipeService.Staples;
            Staples.AddRange(staples);
        }

        public async Task UpdateIsSelected()
        {
            foreach (Staple staple in Staples)
            {
                await RecipeService.UpdateEssentialIsSelected(staple);
            }
        }

        async Task Refresh()
        {
            IsBusy = true;

            await Task.Delay(10);

            await UpdateIsSelected();

            Staples = null;


            try
            {
                Staples = new ObservableRangeCollection<Staple>(RecipeService.Staples);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in Refresh() while getting Staples: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            IsBusy = false;
        }


    }
}
