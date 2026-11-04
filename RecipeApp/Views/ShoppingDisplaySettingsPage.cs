using RecipeApp.Services;
namespace RecipeApp.Views;

public class ShoppingDisplaySettingsPage:ContentPage
{
    public ShoppingDisplaySettingsPage()
    {
        Title="Shopping display";ShoppingUi.Theme(this);
        var body=new VerticalStackLayout{Padding=20,Spacing=18};
        body.Add(ShoppingUi.Text("Make room for your shop",26,true));
        body.Add(ShoppingUi.Text("Changes apply when you return to the shopping list. These preferences stay on this device.",14));
        body.Add(Choice("Shopping item size",SettingsOptions.ShoppingItemSizes,AppSettings.Current.ShoppingItemSize,value=>AppSettings.Current with{ShoppingItemSize=value}));
        body.Add(ShoppingUi.Text("Compact uses smaller text, spacing and padding. Comfortable gives items more room. Checkboxes and drag handles keep their full touch area in both sizes; larger-text preferences are respected.",13));
        body.Add(Choice("Floating + button position",SettingsOptions.ShoppingButtonSides,AppSettings.Current.ShoppingAddButtonSide,value=>AppSettings.Current with{ShoppingAddButtonSide=value}));
        body.Add(ShoppingUi.Text("Tap + to open item entry near the bottom of the list. Closing it keeps unfinished text until you change lists or leave the shopping screen.",13));
        Content=new ScrollView{Content=body};
    }
    private View Choice(string title,string[] options,string current,Func<string,SettingsOptions> update)
    {
        var picker=new Picker{Title=title,ItemsSource=options,SelectedItem=current,HeightRequest=52};
        picker.SetDynamicResource(Picker.TextColorProperty,"ThemeText");var changing=false;
        picker.SelectedIndexChanged+=async(_,_)=>{
            if(changing||picker.SelectedItem is not string value)return;
            try{AppSettings.Save(update(value));current=value;}
            catch{changing=true;picker.SelectedItem=current;changing=false;await DisplayAlert("Settings weren't saved","Try selecting the preference again.","OK");}
        };
        return new VerticalStackLayout{Spacing=4,Children={ShoppingUi.Text(title,17,true),picker}};
    }
}
