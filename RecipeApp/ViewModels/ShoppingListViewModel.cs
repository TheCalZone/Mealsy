using MvvmHelpers;
using MvvmHelpers.Commands;
using RecipeApp.Actions;
using RecipeApp.Models;
using RecipeApp.Services;
using Newtonsoft.Json;
using System.ComponentModel;

namespace RecipeApp.ViewModels
{
    internal class ShoppingListViewModel : CustomViewModelBase
    {
        public List<Ingredient> PlainIngredients { get; set; }
        private ObservableRangeCollection<IngredientGroup> originalIngredients;
        public ObservableRangeCollection<IngredientGroup> OriginalIngredients
        { get => originalIngredients; set => SetProperty(ref originalIngredients, value); }
        public AsyncCommand CopyPlainTextCommand { get; set; }
        public MvvmHelpers.Commands.Command<Ingredient> ShowNoteCommand { get; set; }
        public MvvmHelpers.Commands.Command ClearListCommand { get; set; }
        public MvvmHelpers.Commands.Command ClearSelectedCommand { get; set; }

        private Color iconColor;
        public Color IconColor
        {
            get
            {
                if (AppInfo.RequestedTheme == AppTheme.Dark)
                {
                    // Dark theme is active, set icon color to white
                    return Color.FromArgb("#FFFFFF");
                }
                else
                {
                    // Light theme is active, set icon color to black
                    return Color.FromArgb("#000000");
                }
            }
            set
            {
                SetProperty(ref iconColor, value);
            }
        }


        private ObservableRangeCollection<IngredientGroup> listOfIngredients;
        public ObservableRangeCollection<IngredientGroup> ListOfIngredients
        {
            get
            {
                return listOfIngredients;
            }
            set
            {
                SetProperty(ref listOfIngredients, value);
                ObserveList();

                if (ListOfIngredients.Count > 0)
                {
                    var shoppingListText = string.Join("\n", ListOfIngredients.Select(g =>
                    {
                        var sectionTitle = $"[{g.Key}]";
                        var sectionItems = string.Join("\n", g.Select(i => string.IsNullOrWhiteSpace(i.Note) ? i.Name : $"{i.Name} ({i.Note})"));
                        return $"{sectionTitle}\n{sectionItems}\n";
                    }));

                    ShoppingListText = shoppingListText;
                }
                else
                {
                    ShoppingListText = string.Empty;
                }
                
            }
        }
        private Stack<ListViewAction> _undoStack = new Stack<ListViewAction>();
        private Stack<ListViewAction> _redoStack = new Stack<ListViewAction>();
        private ObservableRangeCollection<Recipe> recipes;
        public ObservableRangeCollection<Recipe>Recipes
        {
            get => recipes;
            set { SetProperty(ref recipes,value); }
        }
        private ObservableRangeCollection<Snack> snacks;
        public ObservableRangeCollection<Snack> Snacks
        {
            get => snacks;
            set { SetProperty(ref snacks, value); }
        }
        private ObservableRangeCollection<Staple> staples;
        public ObservableRangeCollection<Staple> Staples
        {
            get => staples;
            set { SetProperty(ref staples, value); }
        }
        public string ShoppingListText
        {
            get
            {
                return shoppingListText;
            }
            set
            {
                SetProperty(ref shoppingListText, value);
                //SaveShoppingListToSecureStorage();
            }
        }
        public MvvmHelpers.Commands.Command ShowPlainTextCommand { get; }
        public MvvmHelpers.Commands.Command ShowCheckedCommand { get; }
        public MvvmHelpers.Commands.Command<string> AddIngredientCommand { get; }
        public MvvmHelpers.Commands.Command<Ingredient> RemoveIngredientCommand { get; }
        public MvvmHelpers.Commands.Command AddGroupCommand { get; }
        public MvvmHelpers.Commands.Command RemoveGroupCommand { get; }
        public MvvmHelpers.Commands.Command CheckBoxChangedCommand { get; }
        public MvvmHelpers.Commands.Command UndoCommand => new MvvmHelpers.Commands.Command(Undo);
        public MvvmHelpers.Commands.Command RedoCommand => new MvvmHelpers.Commands.Command(Redo);


        private bool showPlainText;
        private bool isEditing;
        public bool IsEditing
        {
            get { return isEditing; }
            set { SetProperty(ref isEditing, value); }
        }
        public bool ListIsFiltered { get; set; }
        private string checkedText;
        public string CheckedText
        {
            get { return checkedText; }
            set { SetProperty(ref checkedText, value); }
        }
        public Color CheckedColor
        {
            get => checkedColor;
            set { SetProperty(ref checkedColor, value); }
        }
        private Color checkedColor = Color.FromArgb("#FF6347");

        public int InBasket { get; set; }
        public int OutBasket { get; set; }
        public MvvmHelpers.Commands.Command EditShoppingListCommand { get; }

        public MvvmHelpers.Commands.Command ProcessPastedListCommand { get; }
        public MvvmHelpers.Commands.Command ProcessListFromEnterCommand { get; }

        public bool ShowPlainText
        {
            get { return showPlainText; }
            set { SetProperty(ref showPlainText, value); }
        }

        public bool IsManualList { get; set; }

        public AsyncCommand ShowHelpCommand { get; }
        public IngredientGroup CurrentGroup { get; private set; }

        string helpMessage = $"Welcome to the Shopping List!\n\n" +
                             $"Here, you can create a list of all the items you need to buy. Once you've added an item, you can tick it off your list when you've added it to your basket.\n\n" +
                             $"You can also add items on the fly by pressing the '+' button next to each section header. This is useful for when you're walking around the store and remember something you need to buy.\n\n" +
                             "Happy shopping!";

        private string shoppingListText;

        public ShoppingListViewModel()
        {
            ShowHelpCommand = new AsyncCommand(ShowHelp);
            ListOfIngredients = new ObservableRangeCollection<IngredientGroup>();
            PlainIngredients = new List<Ingredient>();
            ShowPlainTextCommand = new MvvmHelpers.Commands.Command(() =>
            {
                ShowPlainText = !ShowPlainText;
                UpdateShoppingText();
            });
            EditShoppingListCommand = new MvvmHelpers.Commands.Command(() => { IsEditing = !IsEditing; });
            AddIngredientCommand = new MvvmHelpers.Commands.Command<string>(AddIngredient);
            RemoveIngredientCommand = new MvvmHelpers.Commands.Command<Ingredient>(RemoveIngredient);
            AddGroupCommand = new MvvmHelpers.Commands.Command(AddGroup);
            RemoveGroupCommand = new MvvmHelpers.Commands.Command<string>(RemoveGroup);
            CheckBoxChangedCommand = new MvvmHelpers.Commands.Command(UpdateCheckedText);
            //ShowCheckedCommand = new MvvmHelpers.Commands.Command(ShowSelected);
            OriginalIngredients = ListOfIngredients;
            CopyPlainTextCommand = new AsyncCommand(Copy);
            ShowNoteCommand = new MvvmHelpers.Commands.Command<Ingredient>(ShowNote);
            ClearListCommand = new MvvmHelpers.Commands.Command(ClearShoppingList);
            ClearSelectedCommand = new MvvmHelpers.Commands.Command(ClearSelected);
            ProcessPastedListCommand = new MvvmHelpers.Commands.Command(ProcessPastedList);
            ProcessListFromEnterCommand = new MvvmHelpers.Commands.Command(ProcessPastedListFromEnter);
            Recipes = new ObservableRangeCollection<Recipe>();
            Snacks = new ObservableRangeCollection<Snack>();
            Staples = new ObservableRangeCollection<Staple>();
            LoadDataAsync();
        }

        private async void ClearShoppingList()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Clear List", "Are you sure you want to clear your shopping list?", "Yes", "No");
            if (confirm)
            {
                _undoStack.Clear();
                _redoStack.Clear();
                ListOfIngredients.Clear();
                PlainIngredients.Clear();
                UpdateCheckedText();
                UpdateShoppingText();
                SaveShoppingList();
            }
        }

        private async void ClearSelected()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Clear List", "Are you sure you want to clear the selected items from your shopping list?", "Yes", "No");

            if (confirm)
            {
                // Create a copy of the ingredients to remove
                var ingredientsToRemove = new List<Ingredient>();

                foreach (var group in ListOfIngredients)
                {
                    foreach (var ingredient in group)
                    {
                        if (ingredient.InBasket)
                        {
                            ingredientsToRemove.Add(ingredient);
                        }
                    }
                }

                // Remove the ingredients from the original list
                foreach (var ingredientToRemove in ingredientsToRemove)
                {
                    RemoveIngredient(ingredientToRemove);
                }

                UpdateCheckedText();
            }
        }

        public async Task ShowHelp()
        {
            await Application.Current.MainPage.DisplayAlert("Help", helpMessage, "Close");
        }

        private void UpdateCheckedText()
        {
            PlainIngredients = ListOfIngredients.SelectMany(group => group).ToList();
            if (ListOfIngredients.Count == 0)
            {
                CheckedText = "0/0";
            }
            else
            {
                int totalCheckedCount = 0;

                foreach (var group in ListOfIngredients)
                {
                    int groupCheckedCount = 0;

                    foreach (var ingredient in group)
                    {
                        if (ingredient.InBasket)
                        {
                            totalCheckedCount++;
                            groupCheckedCount++;
                        }
                    }

                    // Output the count for the current group (optional)
                    Console.WriteLine($"Group: {group.Key}, Checked: {groupCheckedCount}");
                }

                CheckedText = $"{totalCheckedCount}/{PlainIngredients.Count}";

                CheckedColor = totalCheckedCount == PlainIngredients.Count ? Color.FromArgb("#36C977") : Color.FromArgb("#FF6347");
            }
        }


        private void UpdateShoppingText()
        {
            ShoppingListText = string.Join("\n", ListOfIngredients.Select(g =>
            {
                var sectionTitle = $"[{g.Key}]";
                var sectionItems = string.Join("\n", g.Select(i => string.IsNullOrWhiteSpace(i.Note) ? i.Name : $"{i.Name} ({i.Note})"));
                return $"{sectionTitle}\n{sectionItems}\n";
            }));
        }

        //private void ShowSelected()
        //{
        //    if (ListIsFiltered)
        //    {
        //        foreach (var group in ListOfIngredients)
        //        {
        //            var visibleItems = group.Where(i => !i.InBasket).ToList();
        //            group.ReplaceRange(visibleItems);
        //        }

        //    }
        //    else
        //    {
        //        ListOfIngredients = OriginalIngredients;
        //    }

        //    ListIsFiltered = !ListIsFiltered;
        //}

        private void AddGroup()
        {
            var listOfIngredients = ListOfIngredients;
            var newIngredient = new Ingredient { Name = String.Empty };

            string newKey;
            if (listOfIngredients.Count == 0)
            {
                newKey = "1";
            }
            else
            {
                // Find the highest key in the collection
                var maxKey = listOfIngredients.Max(g => int.TryParse(g.Key, out int result) ? result : 0);

                // Create a new key as an increment of the max key
                newKey = "New Group" + (maxKey + 1).ToString();
            }

            var newGroup = new IngredientGroup(newKey, new List<Ingredient> { newIngredient });
            listOfIngredients.Add(newGroup);

            // Set the current group to the newly created group
            CurrentGroup = newGroup;

            var collectionView = Shell.Current.CurrentPage.FindByName<CollectionView>("ColelctionViewShopping");
            if (collectionView != null)
            {
                collectionView.ScrollTo(CurrentGroup[0], CurrentGroup, ScrollToPosition.MakeVisible, true);
            }
            ListOfIngredients = listOfIngredients;

            _redoStack.Clear();
            _undoStack.Push(new ListViewAction
            {
                Type = ActionType.AddGroup,
                Group = newGroup,
                GroupIndex = listOfIngredients.IndexOf(newGroup)
            });

            PlainIngredients.Add(newIngredient);
            UpdateCheckedText();

            // Persist both generated and manual lists.
            {
                UpdateShoppingText();
                SaveShoppingList();
            }
        }

        private async void RemoveGroup(string key)
        {
            var removeGroup = await Application.Current.MainPage.DisplayAlert("Remove Group", "Are you sure you want to remove this group?", "Yes", "No");
            if (removeGroup)
            {
                var listofIngredients = ListOfIngredients;
                var groupToRemove = listofIngredients.FirstOrDefault(g => g.Key == key);

                if (groupToRemove != null)
                {
                    foreach (var ingredient in groupToRemove)
                    {
                        PlainIngredients.Remove(ingredient);
                    }

                    _redoStack.Clear();
            _undoStack.Push(new ListViewAction
                    {
                        Type = ActionType.RemoveGroup,
                        Group = groupToRemove,
                        GroupIndex = listOfIngredients.IndexOf(groupToRemove)
                    });

                    if (groupToRemove != null)
                    {
                        listofIngredients.Remove(groupToRemove);
                    }

                    ListOfIngredients = listOfIngredients;
                    UpdateCheckedText();

                    // Persist both generated and manual lists.
                    {
                        UpdateShoppingText();
                        SaveShoppingList();
                    }

                }
            }
            else
            {
                return;
            }
        }


        private void AddIngredient(string key)
        {
            var listofIngredients = ListOfIngredients;
            var newIngredient = new Ingredient { Name = String.Empty };
            var group = listofIngredients.FirstOrDefault(g => g.Key == key);

            if (group == null)
            {
                // If the group is null, add the new ingredient to a new group with the specified key
                listofIngredients.Add(new IngredientGroup(key, new List<Ingredient> { newIngredient }));
            }
            else
            {
                // Add the new ingredient to the existing group
                group.Add(newIngredient);
                var listView = Shell.Current.CurrentPage.FindByName<ListView>("ListViewShopping");
                if (listView != null)
                {
                    listView.ScrollTo(newIngredient, group, ScrollToPosition.MakeVisible, true);
                }

                _redoStack.Clear();
            _undoStack.Push(new ListViewAction
                {
                    Type = ActionType.AddItem,
                    Group = group,
                    Item = newIngredient,
                    ItemIndex = group.Count - 1
                });

            }
            ListOfIngredients = listofIngredients;
            PlainIngredients.Add(newIngredient);
            UpdateCheckedText();

            // Persist both generated and manual lists.
            {
                UpdateShoppingText();
                SaveShoppingList();
            }
        }

        private void RemoveIngredient(Ingredient ingredient)
        {
            for (int i = 0; i < ListOfIngredients.Count; i++)
            {
                var group = ListOfIngredients[i];

                if (group.Contains(ingredient))
                {
                    // Add the action to the undo stack
                    _redoStack.Clear();
            _undoStack.Push(new ListViewAction
                    {
                        Type = ActionType.RemoveItem,
                        Group = group,
                        GroupIndex = ListOfIngredients.IndexOf(group),
                        ItemIndex = group.IndexOf(ingredient),
                        Item = ingredient
                    });

                    group.Remove(ingredient);
                    PlainIngredients.Remove(ingredient);

                    if (group.Count == 0)
                    {
                        ListOfIngredients.RemoveAt(i);
                        i--; // Adjust the index as we removed an item
                    }
                }
            }

            UpdateCheckedText();

            // Persist both generated and manual lists.
            {
                UpdateShoppingText();
                SaveShoppingList();
            }
        }

        private void ApplyAction(ListViewAction action, bool undo)
        {
            switch (action.Type)
            {
                case ActionType.AddGroup:
                case ActionType.RemoveGroup:
                    bool insertGroup = (action.Type == ActionType.AddGroup) != undo;
                    if (insertGroup)
                        ListOfIngredients.Insert(Math.Min(action.GroupIndex, ListOfIngredients.Count), action.Group);
                    else
                        ListOfIngredients.Remove(action.Group);
                    break;
                case ActionType.AddItem:
                case ActionType.RemoveItem:
                    bool insertItem = (action.Type == ActionType.AddItem) != undo;
                    if (insertItem)
                    {
                        if (!ListOfIngredients.Contains(action.Group))
                            ListOfIngredients.Insert(Math.Min(action.GroupIndex, ListOfIngredients.Count), action.Group);
                        action.Group.Insert(Math.Min(action.ItemIndex, action.Group.Count), action.Item);
                    }
                    else
                    {
                        action.Group.Remove(action.Item);
                        if (action.Group.Count == 0)
                            ListOfIngredients.Remove(action.Group);
                    }
                    break;
            }
            UpdateCheckedText();
            UpdateShoppingText();
            SaveShoppingList();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0)
                return;
            var action = _undoStack.Pop();
            ApplyAction(action, true);
            _redoStack.Push(action);
        }

        private void Redo()
        {
            if (_redoStack.Count == 0)
                return;
            var action = _redoStack.Pop();
            ApplyAction(action, false);
            _undoStack.Push(action);
        }

        private void ShowNote(Ingredient ing)
        {
            if (ing.Note != null)
            {
                Shell.Current.DisplayAlert("Note", ing.Note, "OK");
            }
        }

        private async Task Copy()
        {
            // Copy the text to the clipboard
            await Clipboard.SetTextAsync(ShoppingListText);
        }
        private void LoadDataAsync()
        {
            if (Recipes.Count <= 0)
            {
                Recipes.AddRange(RecipeService.Recipes);
            }
            if (Snacks.Count <= 0)
            {
                Snacks.Clear();
                Snacks.AddRange(RecipeService.Snacks);
            }
            if (Staples.Count <= 0)
            {
                Staples.Clear();
                Staples.AddRange(RecipeService.Staples);
            }
        }

        private void ProcessPastedList()
        {
            ParseShoppingText();
            ShowPlainText = false;
        }

        private void ProcessPastedListFromEnter()
        {
            ParseShoppingText();
        }

        private void ParseShoppingText()
        {
            var text = ShoppingListText ?? string.Empty;
            var groups = new List<IngredientGroup>();
            string section = null;
            foreach (var rawLine in text.Split(new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }
                string name = line;
                string note = null;
                int noteIndex = line.IndexOf('(');
                if (noteIndex > 0 && line.EndsWith(")"))
                {
                    name = line.Substring(0, noteIndex).Trim();
                    note = line.Substring(noteIndex + 1, line.Length - noteIndex - 2).Trim();
                }
                var tag = section ?? FindMatchedIngredient(name)?.Tag ?? string.Empty;
                var ingredient = new Ingredient { Name = name, Note = note, Tag = tag };
                var group = groups.FirstOrDefault(item => item.Key == tag);
                if (group == null)
                {
                    group = new IngredientGroup(tag, new List<Ingredient>());
                    groups.Add(group);
                }
                group.Add(ingredient);
            }

            _undoStack.Clear();
            _redoStack.Clear();
            ListOfIngredients = new ObservableRangeCollection<IngredientGroup>(groups);
            UpdateCheckedText();
            UpdateShoppingText();
            SaveShoppingList();
        }

        private Ingredient FindMatchedIngredient(string ingredientName)
        {
            Ingredient matchedIngredient = null;
            string matchedTag = null;


            if (Recipes.Count > 0)
            {
                // Search for a match in Recipes
                foreach (var recipe in Recipes)
                {
                    if (recipe.Ingredients != null)
                    {
                        matchedIngredient = recipe.Ingredients.FirstOrDefault(ing => ing?.Name?.IndexOf(ingredientName, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (matchedIngredient != null)
                        {
                            matchedTag = matchedIngredient.Tag;
                            if (matchedTag != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }


            // Search for a match in Snacks
            if (matchedIngredient == null)
            {
                if (Snacks.Count > 0)
                {
                    foreach (var snack in Snacks)
                    {
                        Snack matchedSnack = Snacks.FirstOrDefault(ing => ing.Name.IndexOf(ingredientName, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (matchedSnack != null)
                        {
                            matchedIngredient = new Ingredient { Name = matchedSnack.Name, Tag = matchedSnack.Tag };
                            if (matchedSnack != null)
                            {
                                matchedTag = matchedSnack.Tag;
                                break;
                            }
                        }
                    }
                }

            }

            // Search for a match in Staples
            if (matchedIngredient == null)
            {
                if (Staples.Count > 0)
                {
                    foreach (var staple in Staples)
                    {
                        Staple matchedStaple = Staples.FirstOrDefault(ing => ing.Name.IndexOf(ingredientName, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (matchedStaple != null)
                        {
                            matchedIngredient = new Ingredient { Name = matchedStaple.Name, Tag = matchedStaple.Tag };
                            if (matchedStaple != null)
                            {
                                matchedTag = matchedStaple.Tag;
                                break;
                            }
                        }
                    }
                }
            }

            if (matchedIngredient == null)
            {
                foreach (Ingredient ingredient in RecipeService.MasterIngredientList)
                {
                    matchedIngredient = RecipeService.MasterIngredientList.FirstOrDefault(ing => ing.Name.IndexOf(ingredientName, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matchedIngredient != null)
                    {
                        matchedTag = matchedIngredient.Tag;
                        break;
                    }
                }
            }

            return matchedIngredient;
        }

        private readonly HashSet<IngredientGroup> observedGroups = new();
        private readonly HashSet<Ingredient> observedIngredients = new(ReferenceEqualityComparer.Instance);

        private void ObserveList()
        {
            if (ListOfIngredients == null)
                return;
            // Detach first: loading another list must not retain its old event handlers.
            foreach (var group in observedGroups)
                group.CollectionChanged -= ListChanged;
            foreach (var ingredient in observedIngredients)
                ingredient.PropertyChanged -= IngredientChanged;
            observedGroups.Clear();
            observedIngredients.Clear();

            foreach (var group in ListOfIngredients)
            {
                observedGroups.Add(group);
                group.CollectionChanged += ListChanged;
                foreach (var ingredient in group)
                {
                    if (observedIngredients.Add(ingredient))
                        ingredient.PropertyChanged += IngredientChanged;
                }
            }
            ListOfIngredients.CollectionChanged -= ListChanged;
            ListOfIngredients.CollectionChanged += ListChanged;
        }

        private void ListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            ObserveList();
            UpdateCheckedText();
            UpdateShoppingText();
            SaveShoppingList();
        }

        private void IngredientChanged(object sender, PropertyChangedEventArgs args)
        {
            UpdateCheckedText();
            UpdateShoppingText();
            SaveShoppingList();
        }

        private void SaveShoppingList()
        {
            if (IsManualList)
                Preferences.Set("ShoppingListText", ShoppingListText ?? string.Empty);
            else
                Preferences.Set("IngredientsJson", JsonConvert.SerializeObject(
                    ListOfIngredients.SelectMany(group => group).ToList()));
        }

        private void GetShoppingListFromSecureStorage()
        {
            ShoppingListText = Preferences.Get("ShoppingListText", "");
        }
    }
}
