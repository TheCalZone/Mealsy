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
    public string AddedBy { get; set; }
    public string AddedByName { get; set; }
    public string AdditionKind { get; set; }
    [JsonIgnore] public bool HasContributor => !string.IsNullOrWhiteSpace(AddedBy) && !string.IsNullOrWhiteSpace(AddedByName);
    [JsonIgnore] public string ContributorLabel => (AdditionKind=="requested"?"Requested by ":"Added by ")+AddedByName;
    // A white badge keeps these dark member colours readable in either appearance.
    [JsonIgnore] public string ContributorColor {
        get {
            uint hash=2166136261;foreach(var ch in AddedBy??"")hash=unchecked((hash^ch)*16777619);
            string[] colors={"#165D91","#7A325F","#426320","#915015","#51449A","#176865","#8B3544","#4F5965"};
            return colors[hash%(uint)colors.Length];
        }
    }
    [JsonIgnore] public bool HasQuantity => !string.IsNullOrWhiteSpace(Quantity);
    [JsonIgnore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}
