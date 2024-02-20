using Microsoft.Maui.ApplicationModel;
using Plugin.Firebase.Auth;
using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShoppingListMakerPage : ContentPage
    {
        public ShoppingListMakerPage()
        {
            InitializeComponent();
        }


        protected override void OnAppearing()
        {
            ((ShoppingListMakerViewModel)BindingContext).Recipes.Clear();
            ((ShoppingListMakerViewModel)BindingContext).OriginalRecipes.Clear();


            var recipes = RecipeService.Recipes;
            if (CrossFirebaseAuth.Current.CurrentUser != null)
            {
                ((ShoppingListMakerViewModel)BindingContext).Recipes.ReplaceRange(recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
                ((ShoppingListMakerViewModel)BindingContext).OriginalRecipes.ReplaceRange(recipes.Where(r => r.FirebaseUID == CrossFirebaseAuth.Current.CurrentUser.Uid));
            }
            else
            {
                ((ShoppingListMakerViewModel)BindingContext).Recipes.ReplaceRange(recipes);
                ((ShoppingListMakerViewModel)BindingContext).OriginalRecipes.ReplaceRange(recipes);
            }
            LoadFilters();
        }

        private void LoadFilters()
        {
            ((ShoppingListMakerViewModel)BindingContext).Filters.Clear();

            // Get all unique tags in the recipes list
            var recipes = ((ShoppingListMakerViewModel)BindingContext).Recipes;

            var tags = recipes
                .Where(r => r.Tags != null)
                .SelectMany(r => r.Tags.Split(',').Select(tag => tag.Trim().ToLower()))
                .Distinct()
                .ToList();

            // Update the filter list with the unique tags
            ((ShoppingListMakerViewModel)BindingContext).Filters.ReplaceRange(tags);
            if (((ShoppingListMakerViewModel)BindingContext).Filters.Contains("all"))
            {
                ((ShoppingListMakerViewModel)BindingContext).Filters.Add("all");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            if (((ShoppingListMakerViewModel)BindingContext) != null)
            {
                await ((ShoppingListMakerViewModel)BindingContext).UpdateIsSelected();
            }
        }

        void OnTab1Clicked(object sender, EventArgs e)
        {
            Tab1Button.FontAttributes = FontAttributes.Bold;
            Tab2Button.FontAttributes = FontAttributes.None;
            Tab3Button.FontAttributes = FontAttributes.None;

            var currentTheme = Application.Current.RequestedTheme;


            if (currentTheme == AppTheme.Dark)
            {
                Tab1Button.TextColor = Color.FromArgb("#FF6347");
                Tab2Button.TextColor = Color.FromArgb("#ffffff");
                Tab3Button.TextColor = Color.FromArgb("#ffffff");
            }
            else
            {
                Tab1Button.TextColor = Color.FromArgb("#FF6347");
                Tab2Button.TextColor = Color.FromArgb("#373737");
                Tab3Button.TextColor = Color.FromArgb("#373737");
            }



            Tab1Content.IsVisible = true;
            Tab2Content.IsVisible = false;
            Tab3Content.IsVisible = false;

            Tab1Box.IsVisible = true;
            Tab2Box.IsVisible = false;
            Tab3Box.IsVisible = false;
        }

        void OnTab2Clicked(object sender, EventArgs e)
        {
            Tab1Button.FontAttributes = FontAttributes.None;
            Tab2Button.FontAttributes = FontAttributes.Bold;
            Tab3Button.FontAttributes = FontAttributes.None;

            var currentTheme = Application.Current.RequestedTheme;


            if (currentTheme == AppTheme.Dark)
            {
                Tab1Button.TextColor = Color.FromArgb("#ffffff");
                Tab2Button.TextColor = Color.FromArgb("#FF6347");
                Tab3Button.TextColor = Color.FromArgb("#ffffff");
            }
            else
            {
                Tab1Button.TextColor = Color.FromArgb("#373737");
                Tab2Button.TextColor = Color.FromArgb("#FF6347");
                Tab3Button.TextColor = Color.FromArgb("#373737");
            }

           

            Tab1Content.IsVisible = false;
            Tab2Content.IsVisible = true;
            Tab3Content.IsVisible = false;

            Tab1Box.IsVisible = false;
            Tab2Box.IsVisible = true;
            Tab3Box.IsVisible = false;
        }

        void OnTab3Clicked(object sender, EventArgs e)
        {
            Tab1Button.FontAttributes = FontAttributes.None;
            Tab2Button.FontAttributes = FontAttributes.None;
            Tab3Button.FontAttributes = FontAttributes.Bold;

            var currentTheme = Application.Current.RequestedTheme;


            if (currentTheme == AppTheme.Dark)
            {
                Tab1Button.TextColor = Color.FromArgb("#ffffff");
                Tab2Button.TextColor = Color.FromArgb("#ffffff");
                Tab3Button.TextColor = Color.FromArgb("#FF6347");
            }
            else
            {
                Tab1Button.TextColor = Color.FromArgb("#373737");
                Tab2Button.TextColor = Color.FromArgb("#373737");
                Tab3Button.TextColor = Color.FromArgb("#FF6347");
            }



            Tab1Content.IsVisible = false;
            Tab2Content.IsVisible = false;
            Tab3Content.IsVisible = true;

            Tab1Box.IsVisible = false;
            Tab2Box.IsVisible = false;
            Tab3Box.IsVisible = true;
        }

    }
}