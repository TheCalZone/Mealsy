using RecipeApp.Services;
namespace RecipeApp.Views;

public class DeleteAccountPage:ContentPage
{
    private readonly string account=AccountSession.UserId;
    private readonly Entry password=ShoppingUi.Entry("","Confirm account password",_=>{},128);
    private readonly Entry confirmation=ShoppingUi.Entry("","Type DELETE",_=>{},6);
    private readonly Label details=ShoppingUi.Text("Confirm your password to review the shared spaces owned by this account. No deletion occurs during review.");
    private readonly Label status=ShoppingUi.Text("",13);
    private readonly CheckBox ownedConsent=new();
    private readonly Button remove;
    private bool busy,reviewed;
    public DeleteAccountPage()
    {
        Title="Delete my account";ShoppingUi.Theme(this);password.IsPassword=true;password.IsTextPredictionEnabled=false;
        var body=new VerticalStackLayout{Padding=20,Spacing=14};
        body.Add(ShoppingUi.Text("Permanently delete your Mealsy account",25,true));
        body.Add(ShoppingUi.Text("This requests automated deletion of your login and associated cloud data. Access is blocked when the request is accepted. A background job completes cleanup, including a later sweep. This cannot be undone."));
        body.Add(ShoppingUi.Text("Any households and creator workspaces you own will be deleted, including their shared lists and plans. Other people's personal accounts and recipes remain intact. If you are only a member, your membership is removed. Copies other people already saved in their own private recipe books are separate."));
        body.Add(ShoppingUi.Text("Deleting an account does not cancel subscriptions or request a refund. Cancel recurring Google Play payments separately before proceeding."));
        if(ClientPlatform.StorePurchasesAvailable)body.Add(ShoppingUi.Button("Manage or cancel subscriptions",()=>Launcher.Default.OpenAsync("https://play.google.com/store/account/subscriptions"),true));
        if(account==null){body.Add(ShoppingUi.Button("Sign in to delete my account",()=>AppNavigation.Root("AccountPage")));Content=new ScrollView{Content=body};return;}
        body.Add(password);body.Add(ShoppingUi.Button("Review my deletion",()=>Run(async()=>{
            AccountSession.Check(account);reviewed=false;remove.IsEnabled=false;ownedConsent.IsChecked=false;confirmation.Text="";
            var preview=await AccountDeletionService.Prepare(password.Text);AccountSession.Check(account);
            details.Text="Owned households: "+(preview.OwnedHouseholds.Count==0?"none":string.Join(", ",preview.OwnedHouseholds.Select(h=>h.Name)))+"\nOwned creator workspaces: "+(preview.OwnedBusinesses.Count==0?"none":string.Join(", ",preview.OwnedBusinesses.Select(h=>h.Name)))+"\nType DELETE and confirm below. Password confirmation expires after five minutes.";
            reviewed=true;remove.IsEnabled=true;
        })));body.Add(details);
        body.Add(new HorizontalStackLayout{Children={ownedConsent,ShoppingUi.Text("I agree to permanently delete any shared households or creator workspaces I own.",13)}});
        body.Add(confirmation);
        remove=ShoppingUi.Button("Request permanent deletion",()=>Run(async()=>{
            AccountSession.Check(account);
            if(!reviewed||confirmation.Text!="DELETE"||!ownedConsent.IsChecked)throw new InvalidOperationException("Review the account, type DELETE and confirm removal of owned shared spaces.");
            if(!await DisplayAlert("Permanently delete this account?","Your account and owned shared spaces will be removed. Access will stop when accepted. Google Play subscriptions must be cancelled separately.","Request deletion","Keep account"))return;
            var receipt=await AccountDeletionService.Request();
            status.Text="Deletion accepted. Reference: "+receipt.Reference;
            await DisplayAlert("Deletion requested","Your request was accepted; cloud cleanup continues in the background. Reference: "+receipt.Reference+". This is not a subscription cancellation. Other devices may still contain offline copies; clear Mealsy's app data on those devices.","OK");
            await AppNavigation.Root("AccountPage");
        }));remove.IsEnabled=false;body.Add(remove);body.Add(status);
        body.Add(ShoppingUi.Button("Email support instead",()=>AppNavigation.Open(new PrivacyAndDataPage()),true));
        Content=new ScrollView{Content=body};
    }
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;
        try{await action();}
        catch(Exception e){status.Text=AccountDeletionService.IsPending(account)?"The server accepted deletion, but local cleanup needs retrying. Restart Mealsy to finish. Reference: "+Preferences.Get("Mealsy.deletion.reference",""):e is HouseholdException or InvalidOperationException?e.Message:"The deletion service could not be reached. No deletion was confirmed. Retry review or use the email support option.";}
        finally{password.Text="";busy=false;}
    }
}
