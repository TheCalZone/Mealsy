using RecipeApp.Services;
namespace RecipeApp.Views;
public class AuthFormView : ContentView
{
    private readonly VerticalStackLayout body=new(){Padding=20,Spacing=14};
    private readonly Label status=ShoppingUi.Text("",14);
    private readonly Entry email,password,confirm,first,last,diet,cuisines;
    private bool busy,authenticated;
    public AuthFormView(bool signup)
    {
        body.Add(ShoppingUi.Text(signup?"A little less planning. A lot more good food.":"Welcome back to your kitchen",29,true));
        body.Add(ShoppingUi.Text(signup?"Create your own Mealsy account. Keep your recipes and personal shopping separate, and join a family when you're ready.":"Sign in to sync your personal recipes and checklists, join your household and share with the food community.",15));
        Entry Field(string title,string hint,int max=200,bool secret=false,Keyboard keyboard=null){body.Add(ShoppingUi.Text(title,12,true));var input=ShoppingUi.Entry("",hint,_=>{},max);input.IsPassword=secret;input.Keyboard=keyboard??Keyboard.Default;input.IsTextPredictionEnabled=!secret;body.Add(input);return input;}
        if(signup){first=Field("First name","What should we call you?",80);last=Field("Last name · optional","Last name",80);}
        email=Field("Email address","you@example.com",254,keyboard:Keyboard.Email);password=Field(signup?"Password · 8+ characters":"Password","Your password",128,true);
        if(signup)confirm=Field("Confirm password","Enter it again",128,true);
        var reveal=ShoppingUi.Check(false,v=>{password.IsPassword=!v;if(confirm!=null)confirm.IsPassword=!v;});body.Add(new HorizontalStackLayout{Children={reveal,ShoppingUi.Text("Show password",13)}});
        if(signup){diet=Field("Dietary needs · optional","Allergies, dietary preferences…",500);cuisines=Field("Favourite cuisines · optional","Italian, Thai, comfort food…",500);body.Add(ShoppingUi.Text("Personal tools are free. Family sharing is optional and paid; creating an account does not start a subscription.",13));}
        body.Add(status);body.Add(ShoppingUi.Button(signup?"Create my account":"Sign in",async()=>{
            if(busy||authenticated)return;busy=true;body.IsEnabled=false;status.Text=signup?"Creating your account…":"Signing in…";
            try
            {
                if(signup)await AccountSession.Register(email.Text,password.Text,confirm.Text,first.Text,last.Text,diet.Text,cuisines.Text);else await AccountSession.SignIn(email.Text,password.Text);
                authenticated=true;password.Text="";if(confirm!=null)confirm.Text="";if(Shell.Current.Navigation.NavigationStack.Count>1)await Shell.Current.Navigation.PopToRootAsync(false);await AppNavigation.Root("AccountPage");
            }
            catch(Exception e){status.Text=e is InvalidOperationException?e.Message:"Couldn't reach Firebase. Check your connection and try again.";}
            finally{busy=false;body.IsEnabled=true;}
        }));
        if(!signup)body.Add(ShoppingUi.Button("Forgot your password?",async()=>{
            if(busy)return;busy=true;try{await AccountSession.ResetPassword(email.Text);status.Text="If an account exists for this address, check its inbox and spam folder for reset instructions.";}catch(Exception e){status.Text=e is InvalidOperationException?e.Message:"Couldn't request a reset. Check your connection and try again.";}finally{busy=false;}
        },true));
        body.Add(ShoppingUi.Button(signup?"Already have an account? Sign in":"New here? Create an account",async()=>{
            if(Shell.Current.CurrentPage is LoginPage or SignupPage)await AppNavigation.Back();
            await AppNavigation.Open(signup?new LoginPage():new SignupPage());
        },true));
        body.Add(ShoppingUi.Button("Continue with my device",()=>AppNavigation.Root("Homepage"),true));
        Content=new ScrollView{Content=body};
    }
}
