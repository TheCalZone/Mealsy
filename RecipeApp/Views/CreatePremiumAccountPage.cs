using System.Security.Cryptography;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class CreatePremiumAccountPage:ContentPage
{
    private readonly string account=AccountSession.UserId;
    private readonly Action<string> created;
    private readonly Entry email=ShoppingUi.Entry("","Reviewer email address",_=>{},254);
    private readonly Entry password=ShoppingUi.Entry("","New password · 12–128 characters",_=>{},128);
    private readonly Entry reason=ShoppingUi.Entry("Google Play reviewer account","Reason for the administrator log",_=>{},440);
    private readonly Picker duration=new(){Title="Complimentary Premium duration",ItemsSource=new[]{"30 days","90 days","365 days"},SelectedIndex=2};
    private readonly Label status=ShoppingUi.Text("Create a separate account for reviewers or testers. Your administrator account stays signed in.",14);
    private readonly Button retry,manage;
    private CreatedReviewerAccount pending;
    private bool busy;
    private string attemptedEmail;
    public CreatePremiumAccountPage(Action<string> onCreated)
    {
        created=onCreated;Title="Create Premium account";ShoppingUi.Theme(this);
        email.Keyboard=Keyboard.Email;password.IsPassword=true;password.IsTextPredictionEnabled=false;
        var body=new VerticalStackLayout {Padding=20,Spacing=14};
        body.Add(ShoppingUi.Text("A ready-to-test account",26,true));
        body.Add(ShoppingUi.Text("Creates a new email/password account, sends a verification email, then grants complimentary Premium using the existing administrator service. No payment, app-store subscription or administrator rights are added. Existing accounts and passwords are never replaced.",14));
        body.Add(email);body.Add(password);
        var show=new CheckBox();show.CheckedChanged+=(_,e)=>password.IsPassword=!e.Value;
        body.Add(new HorizontalStackLayout {Spacing=6,Children={show,ShoppingUi.Text("Show password",14)}});
        body.Add(ShoppingUi.Button("Generate a strong password",()=>{Check();if(!busy)password.Text="M!7a-"+Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));return Task.CompletedTask;},true));
        body.Add(ShoppingUi.Button("Copy login details",async()=>{
            Check();if(busy)return;
            var address=AccountInput.Email(email.Text);
            if(string.IsNullOrWhiteSpace(password.Text))throw new InvalidOperationException("Enter or generate a password first.");
            if(!await DisplayAlert("Copy login credentials?","This places the email and password on your device clipboard. Paste them only into your secure password manager or Play Console App access. The account is not created until you confirm below.","Copy credentials","Cancel"))return;
            Check();await Clipboard.Default.SetTextAsync("Mealsy reviewer login\nEmail: "+address+"\nPassword: "+password.Text);
            status.Text="Credentials copied. Save them securely before creating the account.";
        },true));
        body.Add(ShoppingUi.Text("Save the password before creating the account. It is cleared after the attempt and when leaving this screen; Mealsy cannot show it again. Use a dedicated review email, never someone else's address.",13));
        duration.SetDynamicResource(Picker.TextColorProperty,"ThemeText");body.Add(duration);body.Add(reason);
        body.Add(ShoppingUi.Button("Create account & grant Premium",()=>Run(Create)));
        retry=ShoppingUi.Button("Retry Premium grant for created account",()=>Run(Grant));retry.IsVisible=false;body.Add(retry);
        manage=ShoppingUi.Button("Find this account / manage Premium",async()=>{Check();if(busy)return;created?.Invoke(pending?.Email??attemptedEmail);await AppNavigation.Back();},true);manage.IsVisible=false;body.Add(manage);
        body.Add(status);Content=new ScrollView {Content=body};
    }
    private void Check()=>AccountSession.Check(account);
    private async Task Authorize()
    {
        Check();if(account==null||!(await AdminService.Status()).Admin)throw new InvalidOperationException("Current administrator access is required.");Check();
    }
    private int Days=>duration.SelectedIndex switch {0=>30,1=>90,2=>365,_=>throw new InvalidOperationException("Choose a Premium duration.")};
    private string Reason=>string.IsNullOrWhiteSpace(reason.Text)?throw new InvalidOperationException("A reason is required for the administrator log."):reason.Text.Trim();
    private async Task Create()
    {
        await Authorize();
        if(pending!=null)throw new InvalidOperationException("This screen already created an account. Retry its grant or manage it instead of creating another.");
        var address=AccountInput.Email(email.Text);var secret=password.Text;
        if(secret==null||secret.Length<12)throw new InvalidOperationException("Enter or generate a password of at least 12 characters.");
        var days=Days;_ = Reason;
        if(!await DisplayAlert("Create this reviewer account?",$"Create {address} with {days} days of complimentary Premium? Confirm you have saved its password. Your administrator login will stay signed in. No charge is made.","Create & grant","Cancel"))return;
        Check();attemptedEmail=address;manage.IsVisible=true;
        try{pending=await ReviewerAccountConnection.Create(address,secret);}
        finally{password.Text="";secret=null;}
        Check();created?.Invoke(pending.Email);status.Text="Account created. Granting complimentary Premium…";retry.IsVisible=true;
        await Grant();
    }
    private async Task Grant()
    {
        await Authorize();if(pending==null)throw new InvalidOperationException("Find the account by email to check whether creation succeeded.");
        var days=Days;
        await AdminService.Call<object>(new {action="setFamilyGrant",uid=pending.Uid,days,reason="Created via administrator account tools: "+Reason});Check();
        retry.IsVisible=false;status.Text=$"{pending.Email} has {days} days of complimentary Premium. "+(pending.VerificationEmailSent?"A verification email was sent: open its link in the controlled inbox before giving this account to reviewers.":"Verification-email delivery was not confirmed. Sign in to this account separately and send verification from Account, or use the admin verification control after its backend update.")+" No administrator role or subscription was created.";
    }
    private async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;IsBusy=true;email.IsEnabled=password.IsEnabled=reason.IsEnabled=duration.IsEnabled=false;
        try{await action();}
        catch(Exception error){status.Text=(pending!=null?"Account created; the Premium grant is not confirmed. Retry the grant or manage the account.\n":"")+(error is InvalidOperationException or HouseholdException?error.Message:"Could not finish. Find the account by email before retrying. Existing accounts were not changed.");}
        finally{busy=false;IsBusy=false;email.IsEnabled=password.IsEnabled=reason.IsEnabled=duration.IsEnabled=true;}
    }
    protected override void OnDisappearing(){password.Text="";base.OnDisappearing();}
}
