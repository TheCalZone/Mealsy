using RecipeApp.Services;
namespace RecipeApp.Views;

[ContentProperty(nameof(Text))]
public class TranslateExtension:IMarkupExtension<string>
{
    public string Text { get; set; }
    public string ProvideValue(IServiceProvider serviceProvider)=>AppLocalization.Text(Text);
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)=>ProvideValue(serviceProvider);
}
