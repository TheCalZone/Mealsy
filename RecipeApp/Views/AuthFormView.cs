using RecipeApp.Services;
namespace RecipeApp.Views;
public class AuthFormView : ContentView
{
    private readonly VerticalStackLayout body=new(){Padding=20,Spacing=14};
    private readonly Label status=ShoppingUi.Text("",14);
    private readonly Entry email,password,confirm,first,last,diet,cuisines;
    private bool busy,authenticated;
    private readonly LoadingIndicator progress=new("",false);
    public AuthFormView(bool signup,Func<Task> signedIn=null)
    {
        body.Add(ShoppingUi.Text(signup?AppLocalization.Text("A little less planning. A lot more good food."):AppLocalization.Text("Welcome back to your kitchen"),29,true));
        body.Add(ShoppingUi.Text(signup?AppLocalization.Text("Create your own Mealsy account. Keep your recipes and personal shopping separate, and join a family when you're ready."):AppLocalization.Text("Sign in to sync your personal recipes and checklists, and share shopping with your household."),15));
        Entry Field(string title,string hint,int max=200,bool secret=false,Keyboard keyboard=null){body.Add(ShoppingUi.Text(title,12,true));var input=ShoppingUi.Entry("",hint,_=>{},max);input.IsPassword=secret;input.Keyboard=keyboard??Keyboard.Default;input.IsTextPredictionEnabled=!secret;body.Add(input);return input;}
        body.Add(ShoppingUi.Button("Privacy & your data",()=>AppNavigation.Open(new PrivacyAndDataPage()),true));
        if(signup){first=Field(AppLocalization.Text("First name"),AppLocalization.Text("What should we call you?"),80);last=Field(AppLocalization.Text("Last name · optional"),AppLocalization.Text("Last name"),80);}
        email=Field(AppLocalization.Text("Email address"),"you@example.com",254,keyboard:Keyboard.Email);password=Field(signup?AppLocalization.Text("Password · 8+ characters"):AppLocalization.Text("Password"),AppLocalization.Text("Your password"),128,true);
        if(signup)confirm=Field(AppLocalization.Text("Confirm password"),AppLocalization.Text("Enter it again"),128,true);
        var reveal=ShoppingUi.Check(false,v=>{password.IsPassword=!v;if(confirm!=null)confirm.IsPassword=!v;});body.Add(new HorizontalStackLayout{Children={reveal,ShoppingUi.Text(AppLocalization.Text("Show password"),13)}});
        if(signup){body.Add(ShoppingUi.Text("Optional dietary and profile details are stored with your Firebase account. Do not include sensitive health information you do not want stored.",13));diet=Field("Dietary needs · optional","Allergies, dietary preferences…",500);cuisines=Field("Favourite cuisines · optional","Italian, Thai, comfort food…",500);body.Add(ShoppingUi.Text(AppLocalization.Text("Personal tools are free. Family sharing is optional and paid; creating an account does not start a subscription."),13));}
        body.Add(status);body.Add(ShoppingUi.Button(signup?AppLocalization.Text("Create my account"):AppLocalization.Text("Sign in"),async()=>{
            if(busy||authenticated)return;busy=true;body.IsEnabled=false;status.Text="";progress.Message=signup?AppLocalization.Text("Creating your account…"):AppLocalization.Text("Signing in…");progress.IsBusy=true;AppNavigation.DismissKeyboard();
            try
            {
                if(signup)await AccountSession.Register(email.Text,password.Text,confirm.Text,first.Text,last.Text,diet.Text,cuisines.Text);else await AccountSession.SignIn(email.Text,password.Text);
                authenticated=true;password.Text="";if(confirm!=null)confirm.Text="";progress.Message=AppLocalization.Text("Opening your account…");if(signedIn!=null)await signedIn();else{if(Shell.Current.Navigation.NavigationStack.Count>1)await Shell.Current.Navigation.PopToRootAsync(false);await AppNavigation.Root("AccountPage");}if(signup||OnboardingState.PendingCode.Length>0)await AppNavigation.Open(new OnboardingPage(startAt:OnboardingState.PendingCode.Length>0?2:0));await ContentSharing.Resume();
            }
            catch(Exception e){status.Text=e is InvalidOperationException?AppLocalization.Text(e.Message):"Couldn't reach the account service. Check your connection and try again.";}
            finally{busy=false;body.IsEnabled=true;progress.IsBusy=false;}
        }));
        if(!signup)body.Add(ShoppingUi.Button(AppLocalization.Text("Forgot your password?"),async()=>{
            if(busy)return;busy=true;body.IsEnabled=false;progress.Message=AppLocalization.Text("Sending reset instructions…");progress.IsBusy=true;try{await AccountSession.ResetPassword(email.Text);status.Text="If an account exists for this address, check its inbox and spam folder for reset instructions.";}catch(Exception e){status.Text=e is InvalidOperationException?AppLocalization.Text(e.Message):"Couldn't request a reset. Check your connection and try again.";}finally{busy=false;body.IsEnabled=true;progress.IsBusy=false;}
        },true));
        body.Add(ShoppingUi.Button(signup?AppLocalization.Text("Already have an account? Sign in"):AppLocalization.Text("New here? Create an account"),async()=>{
            if(Shell.Current.CurrentPage is LoginPage or SignupPage)await AppNavigation.Back();
            await AppNavigation.Open(signup?new LoginPage():new SignupPage());
        },true));
        body.Add(ShoppingUi.Button(AppLocalization.Text("Continue with my device"),()=>AppNavigation.Root("Homepage"),true));
        Content=new Grid{Children={new ScrollView{Content=body},new Border{Padding=12,StrokeThickness=0,VerticalOptions=LayoutOptions.Center,HorizontalOptions=LayoutOptions.Fill,Margin=20,Content=progress}}};
        var overlay=(Border)((Grid)Content).Children[1];overlay.SetDynamicResource(VisualElement.BackgroundColorProperty,"ThemeSurface");overlay.IsVisible=false;progress.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(IsVisible))overlay.IsVisible=progress.IsVisible;};
    }
}
