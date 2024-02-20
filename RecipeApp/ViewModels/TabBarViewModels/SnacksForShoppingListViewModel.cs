using MvvmHelpers;
using MvvmHelpers.Commands;
using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Views;
using System.Diagnostics;

namespace RecipeApp.ViewModels.TabBarViewModels
{
    class SnacksForShoppingListViewModel : BaseViewModel
    {
        private ObservableRangeCollection<Snack> snacks;
        public ObservableRangeCollection<Snack> Snacks { get => snacks; set => SetProperty(ref snacks, value); }
        public AsyncCommand<Snack> EditSnackCommand { get; }
        public AsyncCommand<Snack> DeleteSnackCommand { get; }
        public AsyncCommand AddSnackCommand { get; }

        public SnacksForShoppingListViewModel()
        {
            Snacks = new ObservableRangeCollection<Snack>();
            EditSnackCommand = new AsyncCommand<Snack>(EditSnack);
            AddSnackCommand = new AsyncCommand(AddSnack);
            DeleteSnackCommand = new AsyncCommand<Snack>(DeleteSnack);
            LoadDataAsync();

        }


        async Task EditSnack(Snack snack)
        {
            var route = $"{nameof(EditExtraPage)}?SnackName={snack.Name}";
            await Shell.Current.GoToAsync(route);
        }

        async Task AddSnack()
        {
            string name = await Application.Current.MainPage.DisplayPromptAsync("New Snack", "Name:", "Add", "Cancel");
            if (!string.IsNullOrWhiteSpace(name))
            {
                string tag = await Application.Current.MainPage.DisplayPromptAsync("New Snack", "Tag:", "Add", "Cancel");
                Snack snack = new Snack { Name = name, Tag = tag };
                await RecipeService.InsertSnack(snack);
                Snacks.Clear();
                Snacks.AddRange(RecipeService.Snacks);
            }

        }

        async Task DeleteSnack(Snack snack)
        {
            await RecipeService.DeleteSnack(snack);
            await FirestoreService.DeleteSnack(snack);
            Snacks.Clear();
            var snacks = RecipeService.Snacks;
            Snacks.AddRange(snacks);
        }

        private void LoadDataAsync()
        {
            if (Snacks.Count <= 0)
            {
                Snacks.Clear();
                Snacks.AddRange(RecipeService.Snacks);
            }
        }

        public async Task UpdateIsSelected()
        {
            foreach (Snack snack in Snacks)
            {
                await RecipeService.UpdateSnackIsSelected(snack);
            }
        }


        async Task Refresh()
        {
            IsBusy = true;

            await Task.Delay(10);

            await UpdateIsSelected();

            Snacks = null;
            try
            {
                Snacks = new ObservableRangeCollection<Snack>(RecipeService.Snacks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in Refresh() while getting Snacks: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

        }


    }
}
