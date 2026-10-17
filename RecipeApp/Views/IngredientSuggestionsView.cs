using RecipeApp.Services;
using Microsoft.Maui.Layouts;
namespace RecipeApp.Views;

public class IngredientSuggestionsView:ContentView
{
    private readonly FlexLayout chips=new(){Direction=FlexDirection.Row,Wrap=FlexWrap.Wrap};
    private Entry name,quantity;private Func<string> category;private Action<string> setCategory;private Func<IEnumerable<string>> currentNames;
    private List<string> personal=new();private string account,automaticCategory;private int generation;private bool choosing;
    public IngredientSuggestionsView(){Content=chips;IsVisible=false;Unloaded+=(_,_)=>{generation++;personal.Clear();chips.Children.Clear();IsVisible=false;};}
    public void Configure(Entry name,Entry quantity,Func<IEnumerable<string>> currentNames,Func<string> category=null,Action<string> setCategory=null)
    {
        this.name=name;this.quantity=quantity;this.currentNames=currentNames;this.category=category;this.setCategory=setCategory;
        name.TextChanged+=(_,_)=>{
            if(choosing)return;
            if(string.IsNullOrWhiteSpace(name.Text)&&automaticCategory!=null){if(category?.Invoke()==automaticCategory)setCategory?.Invoke("Other");automaticCategory=null;}
            Render();
        };
    }
    public async Task Refresh()
    {
        var version=++generation;account=AccountSession.UserId;var owner=account;personal.Clear();Render();
        try{var saved=await IngredientSuggestionStore.LoadNames(owner);if(version!=generation||AccountSession.UserId!=owner)return;personal=saved;Render();}
        catch{/* Built-in names remain available offline, even if local recipe loading fails. */}
    }
    private void Render()
    {
        chips.Children.Clear();IsVisible=false;if(name==null||account!=AccountSession.UserId)return;
        var suggestions=IngredientSuggestions.Suggest(name.Text,personal.Concat(currentNames?.Invoke()??Array.Empty<string>()),IngredientSuggestionStore.History(account));
        foreach(var suggestion in suggestions){var button=ShoppingUi.Button(suggestion,()=>{Choose(suggestion);return Task.CompletedTask;},true);button.Padding=new Thickness(10,4);button.Margin=new Thickness(0,2,6,2);button.HeightRequest=44;button.FontSize=13;
            SemanticProperties.SetDescription(button,"Use ingredient "+suggestion+", "+ShoppingCategories.Resolve(suggestion,"Other"));chips.Children.Add(button);}
        IsVisible=chips.Children.Count>0;
    }
    private void Choose(string suggestion)
    {
        AccountSession.Check(account);var input=ShoppingItemInput.Parse(name.Text,quantity.Text);choosing=true;
        try{
            name.Text=suggestion;quantity.Text=input.Quantity;name.CursorPosition=name.Text.Length;
            var selected=category?.Invoke();if(setCategory!=null&&(selected=="Other"||selected==automaticCategory)){automaticCategory=ShoppingCategories.Resolve(suggestion,"Other");setCategory(automaticCategory);}
        }finally{choosing=false;chips.Children.Clear();IsVisible=false;}
    }
}
