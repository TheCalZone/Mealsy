using RecipeApp.Services;
namespace RecipeApp.Views;

public class PrivacyAndDataPage:ContentPage
{
    public PrivacyAndDataPage()
    {
        Title="Privacy & your data";ShoppingUi.Theme(this);
        var body=new VerticalStackLayout{Padding=20,Spacing=14};
        body.Add(ShoppingUi.Text("Your food. Your account. Your choices.",25,true));
        body.Add(ShoppingUi.Text("Device mode keeps recipes, lists, meal plans and drafts on this device. Signing in uses Firebase Authentication and syncs your profile, recipes, personal shopping list and checklists. Personal meal plans, reusable weeks and saved shopping trips stay on this device. Household lists and meal plans are shared with household members."));
        body.Add(ShoppingUi.Text("Photos and dietary information",18,true));
        body.Add(ShoppingUi.Text("Recipe photos can be stored with your recipes. Dietary profile details are optional; avoid adding sensitive information you do not want stored. Recipe-link importing contacts the selected website only when you ask it to import."));
        body.Add(ShoppingUi.Text("Diagnostics and advertisements",18,true));
        body.Add(ShoppingUi.Text("Optional crash reporting is off by default for users without a saved choice. You can change it in Settings → Diagnostics & error reports. Reports may include app/device details; Mealsy's custom error reports remove submitted text and account identifiers. Live advertising and public community features are disabled in this test build."));
        if(!string.IsNullOrWhiteSpace(MonetizationConfig.PrivacyPolicyUrl))body.Add(ShoppingUi.Button("Read public privacy policy",()=>Launcher.Default.OpenAsync(MonetizationConfig.PrivacyPolicyUrl),true));
        body.Add(ShoppingUi.Text("Contact and account deletion",18,true));
        body.Add(ShoppingUi.Button("Delete my account automatically",()=>AppNavigation.Open(new DeleteAccountPage())));
        body.Add(ShoppingUi.Text("Use automated deletion below, or contact "+MonetizationConfig.SupportEmail+" for privacy questions and deletion requests. Account ownership must be verified before deletion. Signing out or uninstalling does not delete cloud data. A deletion request does not cancel a Google Play subscription; manage or cancel it separately in Google Play."));
        body.Add(ShoppingUi.Button("Request account and data deletion",async()=>{
            var account=AccountSession.UserId;
            if(!await DisplayAlert("Request account deletion?","This opens an email draft for you to review and send. Support will verify ownership and process the request. This does not immediately delete data or cancel your subscription. Deleting an owned household can affect its members; support will explain before proceeding.","Prepare request","Keep account"))return;
            AccountSession.Check(account);
            var email=AccountSession.Email??"";
            var text="Please delete my Mealsy account and associated personal data.\n\nRegistered email: "+email+"\nAccount reference: "+(account??"Not signed in — supply your registered email")+"\n\nPlease explain any data that must be retained and confirm completion. I understand Google Play subscriptions must be cancelled separately.\n\nDo not include passwords or payment-card details.";
            var uri="mailto:"+MonetizationConfig.SupportEmail+"?subject="+Uri.EscapeDataString("Mealsy account and data deletion request")+"&body="+Uri.EscapeDataString(text);
            if(!await Launcher.Default.TryOpenAsync(uri))await DisplayAlert("Email support","Email "+MonetizationConfig.SupportEmail+" from your registered email address with the subject 'Mealsy account and data deletion request'. Never send your password or payment-card details.","OK");
        }));
        if(ClientPlatform.StorePurchasesAvailable)body.Add(ShoppingUi.Button("Manage Google Play subscriptions",()=>Launcher.Default.OpenAsync("https://play.google.com/store/account/subscriptions"),true));
        body.Add(ShoppingUi.Text("Test-build notice: the public privacy and deletion pages must be published and verified before store distribution. The full release-readiness audit is included with the source handoff.",12));
        Content=new ScrollView{Content=body};
    }
}
