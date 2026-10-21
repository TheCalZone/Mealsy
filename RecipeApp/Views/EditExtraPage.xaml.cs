using RecipeApp.Services;
using RecipeApp.ViewModels;
namespace RecipeApp.Views;
[QueryProperty(nameof(SnackName),nameof(SnackName))]
[QueryProperty(nameof(StapleName),nameof(StapleName))]
public partial class EditExtraPage : ContentPage
{
    public string SnackName{get;set;}public string StapleName{get;set;}private bool opened;private readonly string owner=AccountSession.UserId;
    public EditExtraPage(){InitializeComponent();ShoppingUi.Theme(this);}
    protected override async void OnAppearing()
    {
        base.OnAppearing();if(owner!=AccountSession.UserId){Content=ShoppingUi.Text("Reopen your current account's checklist.");return;}if(opened)return;opened=true;
        try
        {
            var model=new ShoppingListMakerViewModel();await model.Load();AccountSession.Check(owner);var item=model.Library.FirstOrDefault(i=>i.IsSnack==(SnackName!=null)&&i.Name==(SnackName??StapleName));if(item==null)throw new InvalidOperationException("Item unavailable.");
            Content=new VerticalStackLayout{Padding=20,Spacing=12,Children={ShoppingUi.Text(item.Name,24,true),ShoppingUi.Text(item.Quantity??""),ShoppingUi.Button("Edit saved checklist item",async()=>{AccountSession.Check(owner);await AppNavigation.Back();await AppNavigation.Open(new ShoppingExtraEditorPage(model,item));}),ShoppingUi.Button(AppLocalization.Text("Back"),AppNavigation.Back,true)}};
        }
        catch(Exception){Content=ShoppingUi.Button("Item unavailable · Go back",AppNavigation.Back);}
    }
}
