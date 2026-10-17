using MvvmHelpers;
using System.Text.Json.Serialization;

namespace RecipeApp.Models;

public class ShoppingListItem : ObservableObject
{
    private string name = "";
    private string category = "Other";
    private string quantity = "";
    private string note = "";
    private bool inBasket;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get => name; set => SetProperty(ref name, value); }
    public string Category { get => category; set => SetProperty(ref category, value); }
    public string Quantity { get => quantity; set { if (SetProperty(ref quantity, value)) OnPropertyChanged(nameof(HasQuantity)); } }
    public string Note { get => note; set { if (SetProperty(ref note, value)) OnPropertyChanged(nameof(HasNote)); } }
    public bool InBasket { get => inBasket; set => SetProperty(ref inBasket, value); }
    [JsonIgnore] public bool HasQuantity => !string.IsNullOrWhiteSpace(Quantity);
    [JsonIgnore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}
