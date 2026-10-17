using RecipeApp.Services;
namespace RecipeApp.Views;

public class DiagnosticsPage : ContentPage
{
    public DiagnosticsPage(){Title="Diagnostics";ShoppingUi.Theme(this);Render();}
    private void Render()
    {
        var body=new VerticalStackLayout{Padding=20,Spacing=14};
        body.Add(ShoppingUi.Text("Help improve Mealsy",26,true));
        body.Add(ShoppingUi.Text("Diagnostic reports help investigate crashes, connection failures and unavailable safety checks. Reports include app/device information, a fixed operation name, error category and reference number. Mealsy does not add recipe text, photos, email addresses, passwords or tokens to these reports.",14));
        var toggle=new Switch{IsToggled=AppDiagnostics.Enabled};
        toggle.Toggled+=(_,e)=>AppDiagnostics.SetEnabled(e.Value);
        body.Add(ShoppingUi.Text("Send diagnostic reports",17,true));body.Add(toggle);
        body.Add(ShoppingUi.Text("Turn this off to stop collection of new diagnostic reports and discard queued SDK reports. Reports already delivered are unaffected. Recent references remain on this device. Delivery can be delayed while offline or until the next launch.",13));
        body.Add(FoodFeedPage.Button("Send a test diagnostic",async()=>{
            if(!AppDiagnostics.Enabled){await DisplayAlert("Reporting is off","Enable diagnostic reporting before sending a test.","OK");return;}
            var reference=AppDiagnostics.Report(new InvalidOperationException("Synthetic diagnostic"),"diagnostics.test","TEST");
            var state=AppDiagnostics.History().FirstOrDefault(e=>e.Reference==reference)?.Status??"Recorded locally";
            await DisplayAlert("Test recorded",state+". Check the diagnostics dashboard after relaunching to verify delivery.\nReference: "+reference,"OK");Render();
        }));
        body.Add(ShoppingUi.Text("Recent diagnostic references",21,true));
        var entries=AppDiagnostics.History();
        if(entries.Count==0)body.Add(ShoppingUi.Text("No handled failures recorded on this device yet."));
        foreach(var entry in entries){
            body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=6,Children={ShoppingUi.Text(entry.Operation,16,true),ShoppingUi.Text(entry.Code+" · "+entry.Reason,13),
                ShoppingUi.Text(entry.CreatedAt.ToLocalTime().ToString("g")+" · "+entry.Status,12),ShoppingUi.Text(entry.Reference,12),
                FoodFeedPage.Button("Copy reference",async()=>await Clipboard.Default.SetTextAsync(entry.Reference))}}));
        }
        Content=new ScrollView{Content=body};
    }
}
