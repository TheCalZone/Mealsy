using RecipeApp.Services;
using RecipeApp.ViewModels;
namespace RecipeApp.Views;

public class ShoppingSectionOrderPage:ContentPage
{
    private readonly ShoppingListViewModel model;
    private readonly string account=AccountSession.UserId,context;
    private ShoppingSectionDrag drag;private bool showButtons;
    private readonly VerticalStackLayout body=new(){Padding=16,Spacing=12};
    public ShoppingSectionOrderPage(ShoppingListViewModel model)
    {
        this.model=model;context=model.SectionOrderContext;
        drag=new ShoppingSectionDrag(model.SectionNames,order=>{Check();model.SaveSectionOrder(order);Render();},()=>AccountSession.UserId==account&&model.SectionOrderContext==context);
        ToolbarItems.Add(new ToolbarItem("Move with buttons",null,()=>{showButtons=!showButtons;Render();}){Order=ToolbarItemOrder.Secondary});Title=AppLocalization.Text("Reorder sections");ShoppingUi.Theme(this);Content=new ScrollView{Content=body};
    }
    private void Check(){AccountSession.Check(account);if(model.SectionOrderContext!=context)throw new InvalidOperationException("Your shopping list changed. Reopen section order from the list you want to arrange.");}
    protected override void OnAppearing(){base.OnAppearing();try{Check();Render();}catch(InvalidOperationException e){drag.Cancel();body.Clear();body.Add(ShoppingUi.Text(e.Message));}}
    private void Render()
    {
        drag.Cancel();body.Clear();body.Add(ShoppingUi.Text("Your route around the shop",27,true));
        body.Add(ShoppingUi.Text("Touch the move icon and drag the section above or below another section. A floating card and a highlighted gap show where it will land. Changes save immediately for this list on this device; other household members keep their own layout.",14));
        body.Add(ShoppingUi.Text("Empty sections are included here so they're ready when you next add an item. Search and Hide collected don't change this order.",13));
        var order=model.SectionNames();
        for(var index=0;index<order.Count;index++)
        {
            var section=order[index];
            var row=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(new GridLength(48)),new(GridLength.Auto),new(GridLength.Auto)},ColumnSpacing=8};
            row.Add(ShoppingUi.Text(section,18,true),0);
            var handle=ShoppingUi.Text("☰",20);handle.WidthRequest=48;handle.HeightRequest=48;handle.VerticalTextAlignment=TextAlignment.Center;handle.HorizontalTextAlignment=TextAlignment.Center;
            SemanticProperties.SetDescription(handle,"Move section: "+section);SemanticProperties.SetHint(handle,"Touch and move this icon to reorder. The rest of the card scrolls normally.");row.Add(handle,1);
            SemanticProperties.SetDescription(row,section+". Use its move icon to reorder. Use Move with buttons in the menu for an alternative.");
            var up=ShoppingUi.Button("↑",()=>Move(section,-1),true);up.WidthRequest=52;up.IsEnabled=index>0;SemanticProperties.SetDescription(up,"Move "+section+" up");
            var down=ShoppingUi.Button("↓",()=>Move(section,1),true);down.WidthRequest=52;down.IsEnabled=index<order.Count-1;SemanticProperties.SetDescription(down,"Move "+section+" down");
            if(showButtons){row.Add(up,2);row.Add(down,3);}var card=ShoppingUi.Card(row);drag.Attach(card,handle,()=>section);body.Add(card);
        }
        body.Add(ShoppingUi.Button("Restore default order…",async()=>{Check();if(!await DisplayAlert("Restore section order?","Put sections back in alphabetical order, with Other last? Your items and checkmarks are kept.","Restore","Keep my order"))return;Check();model.ResetSectionOrder();Render();},true));
        body.Add(ShoppingUi.Button("Done",AppNavigation.Back));
    }
    protected override void OnDisappearing(){drag.Cancel();base.OnDisappearing();}
    private Task Move(string section,int direction){Check();model.SaveSectionOrder(ShoppingSectionOrder.Move(model.SectionNames(),section,direction));Render();return Task.CompletedTask;}
}
