using System.Globalization;
using SysConvert = System.Convert;


namespace RecipeApp.Converter
{
    public class Base64ToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            string base64Image = (string)value;
            return ImageSource.FromStream(() => new MemoryStream(SysConvert.FromBase64String(base64Image)));
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
