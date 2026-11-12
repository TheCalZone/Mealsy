using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace RecipeApp.Views;

/// <summary>One touch-friendly form used by both creation and editing.</summary>
public class RecipeEditorView : ContentView
{
    private readonly RecipeEditorViewModel model;
    private readonly VerticalStackLayout sections = new() { Spacing = 14 }, steps = new() { Spacing = 12 };
    private readonly Image photo = new() { HeightRequest = 210, Aspect = Aspect.AspectFill };
    private readonly VerticalStackLayout body = new() { Padding = 16, Spacing = 18 };
    public RecipeEditorView(RecipeEditorViewModel model)
    {
        this.model = model; BindingContext = model;
        var intro = new VerticalStackLayout { Spacing = 8 };
        var busyIndicator=new LoadingIndicator("Working on your recipe…",false);intro.Add(busyIndicator);
        var title = Text("", 28, true); title.SetBinding(Label.TextProperty, nameof(model.Heading)); intro.Add(title);
        intro.Add(Text(AppLocalization.Text("Capture the details once. Cook it your way."), 15));
        var summary = Text("", 13); summary.SetBinding(Label.TextProperty, nameof(model.Summary)); intro.Add(summary);
        var progress = new ProgressBar { BackgroundColor = Color.FromArgb("#DBE6D8"), HeightRequest = 6 };
        progress.SetDynamicResource(ProgressBar.ProgressColorProperty,"ThemeAccent");
        progress.SetBinding(ProgressBar.ProgressProperty, nameof(model.Progress)); intro.Add(progress); body.Add(intro);
        var basics = new VerticalStackLayout { Spacing = 8 };
        basics.Add(Text(AppLocalization.Text("01  THE BASICS"), 12, true));
        basics.Add(Field("Draft.Recipe.RecipeName", "Recipe name", "e.g. Roasted tomato pasta", 160));
        basics.Add(Field("Draft.Recipe.Description", "A short introduction · optional", "What makes this one special?", 500, true));
        var numbers = new Grid { ColumnDefinitions = new() { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
        numbers.Add(Field("Draft.Servings", "Serves", "2", 4, numeric: true), 0);
        numbers.Add(Field("Draft.PrepMinutes", "Prep · min", "Optional", 5, numeric: true), 1);
        numbers.Add(Field("Draft.CookMinutes", "Cook · min", "Optional", 5, numeric: true), 2); basics.Add(numbers);
        body.Add(Card(basics));
        var pictures = new VerticalStackLayout { Spacing = 10 };
        pictures.Add(photo);
        pictures.Add(Button(AppLocalization.Text("Choose or replace photo"), model.ChoosePhoto));
        pictures.Add(Button(AppLocalization.Text("Remove photo"), () => { model.Draft.Recipe.RecipeImage = ""; UpdatePhoto(); model.Changed(); return Task.CompletedTask; }, true));
        pictures.Add(Text("Photos are resized for quick saving; location metadata is removed.", 12)); body.Add(new DisclosureSection("Recipe photo","Optional · Add, replace or remove a photo",pictures));
        var ingredients = new VerticalStackLayout { Spacing = 10 };
        ingredients.Add(Text(AppLocalization.Text("02  INGREDIENTS"), 12, true)); ingredients.Add(Text("Herbs, spices and saved essentials are recognised as cupboard checks. Choose Buy each time for anything you always need, or Check cupboard for your own staples. Recipe quantities stay unchanged.", 14));
        ingredients.Add(Button("Manage cupboard ingredients",()=>Shell.Current.Navigation.PushAsync(new CupboardIngredientsPage(()=>model.Refresh())),true));
        ingredients.Add(sections);
        ingredients.Add(Button(AppLocalization.Text("+ Add ingredient section"), async () => {
            var name = await Shell.Current.DisplayPromptAsync("New ingredient section", "What is this section called?", placeholder: "Sauce", maxLength: 100);
            if (string.IsNullOrWhiteSpace(name)) return; model.Draft.Sections.Add(new() { Name = name.Trim() }); model.Refresh();
        }, true));
        var paste=new VerticalStackLayout{Spacing=8};
        paste.Add(Field("Draft.PendingPaste", "Paste ingredients · optional", "[Sauce]\n2 tbsp olive oil\n1½ cups tomatoes, chopped\nSalt to taste", 20000, true));
        paste.Add(Button(AppLocalization.Text("Add pasted ingredients"), async () => await model.Run(() => { model.PasteIngredients(model.Draft.PendingPaste); return Task.CompletedTask; })));
        paste.Add(Button(AppLocalization.Text("Paste from clipboard"), async () => {
            await model.Run(async () => { var text = await Clipboard.Default.GetTextAsync(); if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Copy an ingredient list first."); model.PasteIngredients(text); });
        }, true)); ingredients.Add(new DisclosureSection("Paste an ingredient list","Type ingredients above, or paste several at once",paste)); body.Add(Card(ingredients));
        var method = new VerticalStackLayout { Spacing = 10 }; method.Add(Text(AppLocalization.Text("03  THE METHOD"), 12, true));
        method.Add(Text("One action per step. Use the arrows to put them in order.", 14)); method.Add(steps);
        method.Add(Button(AppLocalization.Text("+ Add method step"), () => { if (model.Draft.Steps.Count >= 100) throw new InvalidOperationException("Use no more than 100 method steps."); model.Draft.Steps.Add(new()); model.Refresh(); return Task.CompletedTask; }, true));
        method.Add(Button(AppLocalization.Text("Paste method from clipboard"), async () => {
            var text = await Clipboard.Default.GetTextAsync(); var parsed = RecipeDraft.ParseSteps(text);
            if (parsed.Count == 0) return;
            if (parsed.Count + model.Draft.Steps.Count(s => !string.IsNullOrWhiteSpace(s.Text)) > 100) throw new InvalidOperationException("Paste fewer steps (100 maximum).");
            foreach (var empty in model.Draft.Steps.Where(s => string.IsNullOrWhiteSpace(s.Text)).ToList()) model.Draft.Steps.Remove(empty);
            foreach (var item in parsed) model.Draft.Steps.Add(new() { Text = item }); model.Refresh();
        }, true)); body.Add(Card(method));
        var extra = new VerticalStackLayout { Spacing = 9 }; extra.Add(Text(AppLocalization.Text("04  MAKE IT EASY TO FIND"), 12, true));
        extra.Add(Field("Draft.Recipe.Tags", "Tags · comma separated", "Vegetarian, quick, dinner", 500));
        var chips = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var tag in new[] { "Vegetarian", "Vegan", "Quick", "Dinner", "Dessert", "Batch cooking" }) chips.Add(Button(tag, () => {
            model.Draft.Recipe.Tags = string.Join(", ", (model.Draft.Recipe.Tags ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Append(tag).Distinct(StringComparer.OrdinalIgnoreCase));
            model.Refresh(); return Task.CompletedTask;
        }, true)); extra.Add(chips);
        extra.Add(Field("Draft.Rating", "Your rating · optional, 0–5", "0–5", 1, numeric: true));
        extra.Add(Field("Draft.Recipe.SourceUrl", "Source link · private, optional", "https://…", 2000));
        extra.Add(Field("Draft.Recipe.Notes", "Cooking notes · private, optional", "Substitutions, storage tips, or what you would change next time…", 5000, true));
        extra.Add(Text("Your source link and private cooking notes stay in your recipe book.", 12)); body.Add(new DisclosureSection("Tags, rating & private notes","Optional · Organise your recipe and keep cooking notes",extra));
        var errors = Text("", 14); errors.TextColor = Color.FromArgb("#BA3C32"); errors.SetBinding(Label.TextProperty, nameof(model.Error)); body.Add(errors);
        var actions=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};
        actions.Add(Button(AppLocalization.Text("Preview recipe"),model.Preview,true),0);actions.Add(Button(AppLocalization.Text("Save recipe"),model.Save),1);
        var saved=Text("",12);saved.SetBinding(Label.TextProperty,nameof(model.Status));
        var footer=new VerticalStackLayout{Padding=new Thickness(16,8),Spacing=4,Children={actions,saved}};
        footer.SetDynamicResource(VisualElement.BackgroundColorProperty,"ThemeSurface");
        body.Add(Button(AppLocalization.Text("Discard draft / changes"), model.Discard, true));
        var layout=new Grid{RowDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)}};
        layout.Add(new ScrollView{Content=body},0,0);layout.Add(footer,0,1);Content=layout;
        model.StructureChanged += Rebuild;
        model.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(model.Busy)) {body.IsEnabled = !model.Busy;footer.IsEnabled=!model.Busy;busyIndicator.IsBusy=model.Busy;} };
        Rebuild();
    }
    internal static Label Text(string value, int size = 15, bool bold = false)
    {
        var label = new Label { Text = value, FontSize = size, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None };
        label.SetDynamicResource(Label.FontSizeProperty, "ThemeFont" + size);
        label.SetDynamicResource(Label.TextColorProperty, "ThemeText"); return label;
    }
    internal static Border Card(View content) {
        var card = new Border { Padding = 16, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Content = content };
        card.SetDynamicResource(Border.BackgroundColorProperty, "ThemeSurface"); return card;
    }
    private Button Button(string name, Func<Task> action, bool quiet = false)
    {
        var button = new Button { Text = name, HeightRequest = -1, MinimumHeightRequest = 46, LineBreakMode = LineBreakMode.WordWrap, MinimumWidthRequest = 44, CornerRadius = 12, Padding = new Thickness(12, 4), FontSize = 14 };
        button.SetDynamicResource(Microsoft.Maui.Controls.Button.BackgroundColorProperty, quiet ? "ThemeSubtle" : "ThemeAccent");
        button.SetDynamicResource(Microsoft.Maui.Controls.Button.TextColorProperty, quiet ? "ThemeText" : "ThemeOnAccent");
        button.Clicked += async (_, _) => {
            button.IsEnabled = false;
            try { await ActionProgress.Run(button,action); model.Changed(); }
            catch (Exception e) { await Shell.Current.DisplayAlert("Recipe", e is InvalidOperationException ? e.Message : "That action couldn't finish. Your draft has been kept.", AppLocalization.Text("OK")); }
            finally { button.IsEnabled = true; }
        }; return button;
    }
    private View Field(string path, string title, string hint, int max, bool multiline = false, bool numeric = false, object context = null, Action afterChange = null)
    {
        InputView input = multiline ? new Editor { AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 96 } : new Entry { HeightRequest = 48, ClearButtonVisibility = ClearButtonVisibility.WhileEditing };
        input.Placeholder = AppLocalization.Text(hint); input.MaxLength = max; input.FontSize = 16;
        input.SetDynamicResource(InputView.TextColorProperty, "ThemeText");
        input.SetDynamicResource(InputView.PlaceholderColorProperty, "ThemeMuted");
        input.SetDynamicResource(BackgroundColorProperty, "ThemeBackground");
        if (numeric) input.Keyboard = Keyboard.Numeric;
        if (context != null) input.BindingContext = context;
        input.SetBinding(InputView.TextProperty, new Binding(path, BindingMode.TwoWay)); input.TextChanged += (_, _) => { model.Changed(); afterChange?.Invoke(); };
        return new VerticalStackLayout { Spacing = 2, Children = { Text(AppLocalization.Text(title), 12, true), input } };
    }
    private void Rebuild()
    {
        sections.Clear(); steps.Clear(); UpdatePhoto();
        List<string> cupboardRules;
        try { cupboardRules=CupboardStore.Load(model.EditorAccount); }
        catch(InvalidOperationException e) { cupboardRules=CupboardRules.Defaults.ToList();model.Status=e.Message; }
        var essentials=RecipeService.Staples.Where(s=>model.EditorAccount==null?string.IsNullOrEmpty(s.FirebaseUID):s.FirebaseUID==model.EditorAccount).Select(s=>s.Name).ToList();
        foreach (var section in model.Draft.Sections)
        {
            var block = new VerticalStackLayout { Spacing = 10 };
            block.Add(Field(nameof(section.Name), "Section name", "Ingredients", 100, context: section));
            block.Add(Button("Section options…",async()=>{
                var choice=await Shell.Current.DisplayActionSheet(section.Name??"Ingredient section",AppLocalization.Text("Cancel"),"Remove section","Move section up","Move section down");
                if(choice=="Move section up")Move(model.Draft.Sections,section,-1);
                else if(choice=="Move section down")Move(model.Draft.Sections,section,1);
                else if(choice=="Remove section"){
                    if(section.Items.Count>0&&!await Shell.Current.DisplayAlert("Remove this section?","Its ingredients will be removed from this draft. Your saved recipe is kept until Save.",AppLocalization.Text("Remove"),AppLocalization.Text("Keep")))return;
                    model.Draft.Sections.Remove(section);if(model.Draft.Sections.Count==0)model.Draft.Sections.Add(new());model.Refresh();
                }
            },true));
            foreach (var item in section.Items)
            {
                var row = new VerticalStackLayout { Spacing = 7 };
                var shoppingHint=Text("",12);
                void UpdateShoppingHint()=>shoppingHint.Text=CupboardRules.Check(item,cupboardRules,essentials)
                    ? (item.ShoppingPreference==0?"Recognised · Check cupboard before buying":"Check cupboard before buying")
                    : "Added to the shop by default";
                var first = new Grid { ColumnDefinitions = new() { new(new GridLength(90)), new(GridLength.Star) }, ColumnSpacing = 8 };
                first.Add(Field(nameof(item.Quantity), "Quantity", "e.g. ½ tsp", 200, context: item), 0);
                first.Add(Field(nameof(item.Name), "Ingredient", "e.g. smoked paprika", 200, context: item, afterChange:UpdateShoppingHint), 1); row.Add(first);
                var shopping=new Picker{Title="When shopping",ItemsSource=new[]{"Automatic","Buy each time","Check cupboard"},SelectedIndex=item.ShoppingPreference is >=0 and <=2?item.ShoppingPreference:0,HeightRequest=48};
                shopping.SetDynamicResource(Picker.TextColorProperty,"ThemeText");
                shopping.SelectedIndexChanged+=(_,_)=>{item.ShoppingPreference=shopping.SelectedIndex;UpdateShoppingHint();model.Changed();};
                row.Add(shoppingHint);UpdateShoppingHint();
                var advanced = new VerticalStackLayout { Spacing = 7, IsVisible = false };advanced.Add(shopping);
                var category = new Picker { Title = "Shopping category", ItemsSource = new[] { "Produce", "Dairy", "Meat", "Pantry", "Bakery", "Frozen", "Drinks", "Other" }, HeightRequest = 48 };
                category.SetDynamicResource(Picker.TextColorProperty, "ThemeText");
                category.SelectedItem = ShoppingListDocument.CategoryName(item.Tag);
                if (category.SelectedIndex < 0) category.SelectedItem = "Other";
                category.SelectedIndexChanged += (_, _) => { item.Tag = category.SelectedItem?.ToString() ?? "Other"; model.Changed(); }; advanced.Add(category);
                advanced.Add(Field(nameof(item.Note), "Ingredient note · optional", "finely chopped, room temperature…", 500, context: item));
                row.Add(advanced);
                row.Add(Button("Ingredient options…",async()=>{
                    var choice=await Shell.Current.DisplayActionSheet(item.Name??"Ingredient",AppLocalization.Text("Cancel"),AppLocalization.Text("Remove"),advanced.IsVisible?"Hide details":"Category, note & shopping checks","Move up","Move down");
                    if(choice=="Move up")Move(section.Items,item,-1);
                    else if(choice=="Move down")Move(section.Items,item,1);
                    else if(choice==AppLocalization.Text("Remove")){section.Items.Remove(item);model.Refresh();}
                    else if(choice is "Hide details" or "Category, note & shopping checks")advanced.IsVisible=!advanced.IsVisible;
                },true));block.Add(Card(row));
            }
            block.Add(Button("+ Add ingredient", () => { model.AddIngredient(section); return Task.CompletedTask; }, true)); sections.Add(block);
        }
        int number = 0;
        foreach (var step in model.Draft.Steps)
        {
            var block = new VerticalStackLayout { Spacing = 6 };
            block.Add(Field(nameof(step.Text), $"Step {++number}", "Describe one stage of the recipe…", 15000, true, context: step));
            block.Add(Button("Step options…",async()=>{
                var choice=await Shell.Current.DisplayActionSheet("Method step",AppLocalization.Text("Cancel"),"Remove step","Move up","Move down");
                if(choice=="Move up")Move(model.Draft.Steps,step,-1);
                else if(choice=="Move down")Move(model.Draft.Steps,step,1);
                else if(choice=="Remove step"){model.Draft.Steps.Remove(step);if(model.Draft.Steps.Count==0)model.Draft.Steps.Add(new());model.Refresh();}
            },true));steps.Add(block);
        }
    }
    private void Move<T>(System.Collections.ObjectModel.ObservableCollection<T> list, T item, int direction)
    {
        var old = list.IndexOf(item); var next = old + direction;
        if (next >= 0 && next < list.Count) { list.Move(old, next); model.Refresh(); }
    }
    private void UpdatePhoto()
    {
        photo.IsVisible = model.HasPhoto;
        try { var bytes = model.HasPhoto ? Convert.FromBase64String(model.Draft.Recipe.RecipeImage) : null; photo.Source = bytes == null ? null : ImageSource.FromStream(() => new MemoryStream(bytes)); }
        catch (FormatException) { photo.Source = null; photo.IsVisible = false; }
    }
}
