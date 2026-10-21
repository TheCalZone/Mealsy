namespace RecipeApp.Views;

// A normal tappable stack; Android additionally recognises a held press.
public class RecipePressLayout : VerticalStackLayout
{
    public event Action Held;
    public void Hold() => Held?.Invoke();
}
