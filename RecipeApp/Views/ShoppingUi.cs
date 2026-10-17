using RecipeApp.Services;
using RecipeApp.ViewModels;

namespace RecipeApp.Views;

internal static class ShoppingUi
{
    public static Label Text(string text, int size=15, bool bold=false) => RecipeEditorView.Text(text, size, bold);
    public static View Card(View view) => RecipeEditorView.Card(view);
    public static Button Button(string text, Func<Task> action, bool quiet=false)
    {
        var button=new Button { Text=text, HeightRequest=48, MinimumWidthRequest=44, CornerRadius=12, FontSize=14, Padding=new Thickness(12,4) };
        button.SetDynamicResource(Microsoft.Maui.Controls.Button.BackgroundColorProperty, quiet ? "ThemeSubtle" : "ThemeAccent");
        button.SetDynamicResource(Microsoft.Maui.Controls.Button.TextColorProperty, quiet ? "ThemeText" : "ThemeOnAccent");
        button.Clicked += async (_, _) => { button.IsEnabled=false; try { await ActionProgress.Run(button,action); } catch (Exception e) { var reference=e is HouseholdException or InvalidOperationException?null:AppDiagnostics.Report(e,"ui.action"); await Shell.Current.DisplayAlert("Couldn't complete action", e is HouseholdException or InvalidOperationException ? e.Message : "That action couldn't finish. Your saved data is kept. Please retry.\nReference: "+reference, "OK"); } finally { button.IsEnabled=true; } };
        return button;
    }
    public static Entry Entry(string text, string hint, Action<string> changed, int max=200, bool numeric=false)
    {
        var field=new Entry { Text=text, Placeholder=hint, MaxLength=max, FontSize=16, HeightRequest=48, Keyboard=numeric ? Keyboard.Numeric : Keyboard.Default };
        field.SetDynamicResource(InputView.TextColorProperty, "ThemeText");
        field.SetDynamicResource(InputView.PlaceholderColorProperty, "ThemeMuted");
        field.TextChanged += (_, e) => changed(e.NewTextValue); return field;
    }
    public static CheckBox Check(bool selected, Action<bool> changed)
    {
        var check=new CheckBox { IsChecked=selected }; check.CheckedChanged += (_, e) => changed(e.Value); return check;
    }
    public static void Theme(ContentPage page)
    {
        page.Padding=0; page.SetDynamicResource(VisualElement.BackgroundColorProperty, "ThemeBackground");
    }
}

public class ShoppingExtraEditorPage : ContentPage
{
    private bool saved, busy;
    public ShoppingExtraEditorPage(ShoppingListMakerViewModel model, ShoppingLibraryItem original=null, bool snack=false, string suggestedName=null, string suggestedQuantity=null, string suggestedCategory=null)
    {
        var editorAccount=model.Account;
        void CheckEditorAccount() { if (editorAccount!=HouseholdService.UserId) throw new InvalidOperationException("Your account changed. Close this form and reopen your checklist."); }
        var item=original?.Copy() ?? new ShoppingLibraryItem { IsSnack=snack, Name=suggestedName ?? "", Quantity=suggestedQuantity ?? "", Category=suggestedCategory ?? "Other" };
        Title=(original==null ? "New " : "Edit ") + (item.IsSnack ? "snack" : "essential"); ShoppingUi.Theme(this);
        var body=new VerticalStackLayout { Padding=16, Spacing=12 };
        body.Add(ShoppingUi.Text(item.IsSnack ? "Little extras you enjoy" : "Your cupboard checklist", 27, true));
        body.Add(ShoppingUi.Text("Save an item here to check on future trips. It won't automatically be added to every shopping list.",14));
        body.Add(ShoppingUi.Text("Item name",12,true)); body.Add(ShoppingUi.Entry(item.Name,"e.g. Dried oregano",s => item.Name=s));
        body.Add(ShoppingUi.Text("Usual shopping quantity · optional",12,true)); body.Add(ShoppingUi.Entry(item.Quantity,"e.g. 1 jar or 200 g",s => item.Quantity=s));
        body.Add(ShoppingUi.Text("Shopping category",12,true));
        var categories=new List<string> {"Produce","Dairy","Meat","Pantry","Bakery","Frozen","Drinks","Other"}; if (!categories.Contains(item.Category)) categories.Add(item.Category);
        var picker=new Picker { ItemsSource=categories, SelectedItem=item.Category, HeightRequest=48 }; picker.SetDynamicResource(Picker.TextColorProperty, "ThemeText");
        picker.SelectedIndexChanged += (_, _) => item.Category=picker.SelectedItem?.ToString() ?? "Other"; body.Add(picker);
        body.Add(ShoppingUi.Text("Note · optional",12,true)); body.Add(ShoppingUi.Entry(item.Note,"Brand, cupboard location, or a reminder",s => item.Note=s,500));
        body.Add(ShoppingUi.Button("Save to my " + (item.IsSnack ? "snacks" : "essentials"), async () => {
            if (saved || busy) return; busy=true;
            try { CheckEditorAccount(); await model.SaveExtra(item, original); saved=true; await Navigation.PopAsync(); } finally { busy=false; }
        }));
        body.Add(ShoppingUi.Button("Cancel",async () => await Navigation.PopAsync(),true));
        if (original!=null) body.Add(ShoppingUi.Button("Delete saved item",async () => {
            if (saved || busy || !await DisplayAlert("Delete this item?","It will be removed from your reusable checklist. Your shopping list won't change.","Delete","Keep")) return;
            busy=true; try { CheckEditorAccount(); await model.DeleteExtra(original); saved=true; await Navigation.PopAsync(); } finally { busy=false; }
        },true));
        Content=new ScrollView { Content=body };
    }
}
