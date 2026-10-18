using RecipeApp.Services;
namespace RecipeApp.Views;

public class SettingsPage : ContentPage
{
    public SettingsPage() { Title = "Settings"; ShoppingUi.Theme(this); Render(); }
    private void Render()
    {
        var body = new VerticalStackLayout { Padding = 16, Spacing = 18 };
        body.Add(ShoppingUi.Text("Make Mealsy yours", 28, true));
        body.Add(ShoppingUi.Button("Get started guide",()=>AppNavigation.Open(new OnboardingPage()),true));
        body.Add(ShoppingUi.Button("Share Mealsy",OnboardingState.ShareApp,true));
        body.Add(ShoppingUi.Text("Saved automatically on this device, for signed-in and device mode alike.", 14));
        var appearance = Section("Appearance", "Changes apply straight away, including screens you've already opened.");
        appearance.Add(Choice("Colour theme", SettingsOptions.Palettes, AppSettings.Current.Palette, value => AppSettings.Current with { Palette = value }));
        var swatches = new HorizontalStackLayout { Spacing = 10 };
        foreach (var color in new[] { "#214D35", "#164E69", "#66344F", "#743E2D", "#334155" })
            swatches.Add(new BoxView { Color = Color.FromArgb(color), WidthRequest = 30, HeightRequest = 8, CornerRadius = 4 });
        appearance.Add(swatches);
        appearance.Add(Choice("Display mode", SettingsOptions.Appearances, AppSettings.Current.Appearance, value => AppSettings.Current with { Appearance = value }));
        appearance.Add(Toggle("Larger reading text", "Increase headings and reading text alongside your phone's font settings.", AppSettings.Current.LargerText, value => AppSettings.Current with { LargerText = value }));
        appearance.Add(Toggle("Show recipe photos", "Show photos on recipe cards and reading screens. Editor photos stay available.", AppSettings.Current.ShowPhotos, value => AppSettings.Current with { ShowPhotos = value }));
        appearance.Add(ShoppingUi.Card(new VerticalStackLayout { Spacing = 8, Children = { ShoppingUi.Text("A little taste of your theme", 20, true), ShoppingUi.Text("Colours and text update here as you choose."), ShoppingUi.Button("Theme preview", () => Task.CompletedTask) } }));
        body.Add(ShoppingUi.Card(appearance));
        var shopping = Section("Shopping", "Defaults apply when you next open a shopping list. You can still change them while shopping.");
        shopping.Add(ShoppingUi.Button("Cupboard ingredients",()=>AppNavigation.Open(new CupboardIngredientsPage()),true));
        shopping.Add(Toggle("Hide collected by default", "Keep the remaining items in view.", AppSettings.Current.HideCollected, value => AppSettings.Current with { HideCollected = value }));
        shopping.Add(Toggle("Show quick-add details", "Open quantity, category and note fields by default.", AppSettings.Current.ShoppingDetails, value => AppSettings.Current with { ShoppingDetails = value }));
        body.Add(ShoppingUi.Card(shopping));
        var recipes = Section("Recipes & cooking", "Choose how your recipe book opens and how you cook.");
        recipes.Add(Choice("Default recipe order", SettingsOptions.Sorts, AppSettings.Current.RecipeSort, value => AppSettings.Current with { RecipeSort = value }));
        recipes.Add(Toggle("Keep screen awake while cooking", "Starts enabled when you open a recipe. Stops when you leave it; you can switch it off inside the recipe.", AppSettings.Current.KeepAwake, value => AppSettings.Current with { KeepAwake = value }));
        body.Add(ShoppingUi.Card(recipes));
        if(ClientPlatform.HomeScreenWidgetAvailable)body.Add(ShoppingUi.Button("Home-screen shopping widget", () => AppNavigation.Open(new WidgetHelpPage()), true));
        body.Add(ShoppingUi.Button("Diagnostics & error reports",()=>AppNavigation.Open(new DiagnosticsPage()),true));
        body.Add(ShoppingUi.Button("Premium plans & features", () => AppNavigation.Open(new UpgradeFeaturesPage()), true));
        body.Add(ShoppingUi.Text("These settings stay on this device. They don't change family members' preferences or your account data. Manage your profile and sync from Account.", 13));
        body.Add(ShoppingUi.Button("Reset preferences…", async () => {
            if (!await DisplayAlert("Reset preferences?", "Restore Forest, system display mode and the original preferences? Recipes, shopping lists, drafts, accounts and meal plans are kept.", "Reset preferences", "Cancel")) return;
            AppSettings.Reset(); Render();
        }, true));
        body.Add(ShoppingUi.Text("Mealsy · " + AppInfo.Current.VersionString, 12));
        Content = new ScrollView { Content = body };
    }
    private static VerticalStackLayout Section(string title, string description) => new() { Spacing = 12, Children = { ShoppingUi.Text(title, 22, true), ShoppingUi.Text(description, 13) } };
    private View Choice(string title, string[] options, string selected, Func<string, SettingsOptions> update)
    {
        var changing = false;
        var picker = new Picker { Title = title, ItemsSource = options, SelectedItem = selected, HeightRequest = 52 };
        picker.SetDynamicResource(Picker.TextColorProperty, "ThemeText");
        picker.SelectedIndexChanged += async (_, _) => {
            if (changing || picker.SelectedItem is not string value) return;
            try { AppSettings.Save(update(value)); selected = value; }
            catch (Exception) { await DisplayAlert("Settings weren't saved", "Try selecting the preference again.", "OK"); changing = true; picker.SelectedItem = selected; changing = false; }
        };
        return new VerticalStackLayout { Children = { ShoppingUi.Text(title, 14, true), picker } };
    }
    private View Toggle(string title, string description, bool value, Func<bool, SettingsOptions> update)
    {
        var toggle = new Switch { IsToggled = value };
        toggle.SetDynamicResource(Switch.OnColorProperty, "ThemeAccent");
        var changing = false;
        toggle.Toggled += async (_, e) => {
            if (changing) return;
            try { AppSettings.Save(update(e.Value)); }
            catch (Exception) { changing = true; toggle.IsToggled = !e.Value; changing = false; await DisplayAlert("Settings weren't saved", "Try changing the preference again.", "OK"); }
        };
        var row = new Grid { ColumnDefinitions = new() { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 8 };
        row.Add(new VerticalStackLayout { Spacing = 4, Children = { ShoppingUi.Text(title, 15, true), ShoppingUi.Text(description, 13) } }, 0);
        row.Add(toggle, 1);
        return row;
    }
}
