using System.Collections;

namespace RecipeApp.CustomViews
{
    public class AutoCompleteView : ContentView
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(AutoCompleteView),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty SelectedValueProperty = BindableProperty.Create(
            nameof(SelectedValue),
            typeof(object),
            typeof(AutoCompleteView),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
    nameof(Placeholder),
    typeof(string),
    typeof(AutoCompleteView),
    defaultValue: string.Empty);

        public static readonly BindableProperty PlaceholderColorProperty = BindableProperty.Create(
            nameof(PlaceholderColor),
            typeof(Color),
            typeof(AutoCompleteView),
            defaultValue: Color.FromArgb("#373737"));

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(AutoCompleteView),
        defaultValue: string.Empty,
        defaultBindingMode: BindingMode.TwoWay);

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public Color PlaceholderColor
        {
            get => (Color)GetValue(PlaceholderColorProperty);
            set => SetValue(PlaceholderColorProperty, value);
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private Entry entry;
        private ListView listView;

        public AutoCompleteView()
        {
            entry = new Entry();
            listView = new ListView { IsVisible = false };


            entry.TextChanged += OnEntryTextChanged;
            listView.ItemTapped += OnListViewItemTapped;


            var stackLayout = new StackLayout();
            stackLayout.Children.Add(entry);
            stackLayout.Children.Add(listView);

            Content = stackLayout;
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewTextValue))
            {
                listView.IsVisible = false;
            }
            else
            {
                if (ItemsSource != null)
                {
                    listView.ItemsSource = ItemsSource.Cast<string>()
                        .Where(x => x.StartsWith(e.NewTextValue, StringComparison.InvariantCultureIgnoreCase));
                    listView.IsVisible = true;
                }
            }
        }

        private void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            SelectedValue = e.Item;
            entry.Text = e.Item.ToString();
            listView.IsVisible = false;
        }
    }

}
