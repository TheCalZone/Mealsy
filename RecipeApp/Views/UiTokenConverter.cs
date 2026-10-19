using System.Globalization;
using RecipeApp.Services;
namespace RecipeApp.Views;

/// <summary>Display a translated option while preserving its stable saved identifier.</summary>
public class UiTokenConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>AppLocalization.Text(value as string??"");
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
