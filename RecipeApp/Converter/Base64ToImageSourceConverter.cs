using System.Globalization;
using SysConvert = System.Convert;


namespace RecipeApp.Converter
{
    public class Base64ToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string base64Image || string.IsNullOrWhiteSpace(base64Image)) return null;
            try
            {
                var bytes = SysConvert.FromBase64String(base64Image);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (FormatException) { return null; }
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
