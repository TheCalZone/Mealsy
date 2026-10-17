using RecipeApp.Services;
using RecipeApp.ViewModels;
namespace RecipeApp.Views;

public class ShoppingSectionOrderPage:ContentPage
{
    private readonly ShoppingListViewModel model;
    private readonly string account=AccountSession.UserId,context;
    private readonly VerticalStackLayout body=new(){Padding=16,Spacing=12};
    public ShoppingSectionOrderPage(ShoppingListViewModel model)
    {
        this.model=model;context=model.SectionOrderContext;Title="Reorder sections";ShoppingUi.Theme(this);Content=new ScrollView{Content=body};
    }
    private void Check(){AccountSession.Check(account);if(model.SectionOrderContext!=context)throw new InvalidOperationException("Your shopping list changed. Reopen section order from the list you want to arrange.");}
    protected override void OnAppearing(){base.OnAppearing();try{Check();Render();}catch(InvalidOperationException e){body.Clear();body.Add(ShoppingUi.Text(e.Message));}}
    private void Render()
    {
        body.Clear();body.Add(ShoppingUi.Text("Your route around the shop",27,true));
        body.Add(ShoppingUi.Text("Move sections up or down to match the way you shop. Changes save immediately for this list on this device; other household members keep their own layout.",14));
        body.Add(ShoppingUi.Text("Empty sections are included here so they're ready when you next add an item. Search and Hide collected don't change this order.",13));
        var order=model.SectionNames();
        for(var index=0;index<order.Count;index++)
        {
            var section=order[index];
            var row=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8};
            row.Add(ShoppingUi.Text($"{index+1}. {section}",18,true),0);
            var up=ShoppingUi.Button("↑",()=>Move(section,-1),true);up.WidthRequest=52;up.IsEnabled=index>0;SemanticProperties.SetDescription(up,"Move "+section+" up");
            var down=ShoppingUi.Button("↓",()=>Move(section,1),true);down.WidthRequest=52;down.IsEnabled=index<order.Count-1;SemanticProperties.SetDescription(down,"Move "+section+" down");
            row.Add(up,1);row.Add(down,2);body.Add(ShoppingUi.Card(row));
        }
        body.Add(ShoppingUi.Button("Restore default order…",async()=>{Check();if(!await DisplayAlert("Restore section order?","Put sections back in alphabetical order, with Other last? Your items and checkmarks are kept.","Restore","Keep my order"))return;Check();model.ResetSectionOrder();Render();},true));
        body.Add(ShoppingUi.Button("Done",AppNavigation.Back));
    }
    private Task Move(string section,int direction){Check();model.SaveSectionOrder(ShoppingSectionOrder.Move(model.SectionNames(),section,direction));Render();return Task.CompletedTask;}
}
