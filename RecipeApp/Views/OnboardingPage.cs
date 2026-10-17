using RecipeApp.Services;
namespace RecipeApp.Views;

public class OnboardingPage:ContentPage
{
    private int step;private bool busy;private Entry code;private Label status;
    public OnboardingPage(string invitation=null,int startAt=0){Title="Get started";step=Math.Clamp(startAt,0,3);ShoppingUi.Theme(this);if(!string.IsNullOrWhiteSpace(invitation))OnboardingState.Remember(invitation);Render();}
    protected override void OnAppearing(){base.OnAppearing();Render();}
    private void Render()
    {
        var body=new VerticalStackLayout{Padding=20,Spacing=16};body.Add(ShoppingUi.Text($"GET STARTED · {step+1} OF 4",12,true));
        if(step==0){body.Add(ShoppingUi.Text("A little less planning. More good food.",29,true));body.Add(ShoppingUi.Text("Cook for yourself or share a kitchen. Start with a recipe, plan a few meals and turn them into a shopping list."));body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text("Your everyday tools are free",21,true),ShoppingUi.Text("Manual recipes, ingredient paste, personal shopping and basic meal planning. Premium adds recipe-link imports, saved weeks and reusable shopping trips, plus Family sharing and ad removal. No subscription starts during setup.")}}));}
        if(step==1){body.Add(ShoppingUi.Text("Your own place at the table",28,true));if(AccountSession.UserId==null){body.Add(ShoppingUi.Text("Use device mode without signing in, or create your own account for private recipe sync and household shopping. Every household member uses a separate account."));body.Add(ShoppingUi.Button("Create a free account",()=>AppNavigation.Open(new SignupPage())));body.Add(ShoppingUi.Button("Sign in instead",()=>AppNavigation.Open(new LoginPage()),true));}else{body.Add(ShoppingUi.Text("Signed in as "+AccountSession.Email));body.Add(ShoppingUi.Text("Keep your recipes and personal shopping list private, and use a household list when you want to shop together. Confirm your email from Account for household invitations."));}}
        if(step==2){body.Add(ShoppingUi.Text("Joining someone's household?",28,true));body.Add(ShoppingUi.Text("Optional: paste their invitation code or link. Joining is covered by the owner's active Premium plan; you don't need your own subscription. You can also keep using Mealsy on your own."));code=ShoppingUi.Entry(OnboardingState.PendingCode,"Household code or invitation link",_=>{},2000);body.Add(code);status=ShoppingUi.Text("",13);body.Add(status);body.Add(ShoppingUi.Button("Join with my code",Join));body.Add(ShoppingUi.Text("Codes are single-use and expire in 7 days. If one no longer works, ask the household owner for a new invitation.",13));}
        if(step==3){body.Add(ShoppingUi.Text("Make your first week easier",28,true));body.Add(ShoppingUi.Text("You're ready. Try one small step now, and find this guide again from Home, Account or Settings."));body.Add(ShoppingUi.Button("Open my recipe book",()=>Finish("RecipePage")));body.Add(ShoppingUi.Button("Plan a few meals",()=>Finish("MealPlanOverviewPage"),true));body.Add(ShoppingUi.Button("Start my shopping list",()=>Finish("ShoppingListPage"),true));body.Add(ShoppingUi.Button("Invite a friend to try Mealsy",OnboardingState.ShareApp,true));}
        if(step<3)body.Add(ShoppingUi.Button(step==2?"Continue without joining":"Continue",()=>{if(step==2&&!string.IsNullOrWhiteSpace(code?.Text)){try{OnboardingState.Remember(code.Text);}catch(InvalidOperationException){}}step++;Render();return Task.CompletedTask;}));
        if(step>0)body.Add(ShoppingUi.Button("Back",()=>{step--;Render();return Task.CompletedTask;},true));
        body.Add(ShoppingUi.Button(step==3?"Finish setup":"Skip setup for now",()=>Finish("Homepage"),true));Content=new ScrollView{Content=body};
    }
    private async Task Join()
    {
        if(busy)return;busy=true;var account=AccountSession.UserId;
        try{var invitation=InvitationLinks.Code(code.Text);OnboardingState.Remember(invitation);if(account==null){status.Text="Your code is kept. Sign in or create an account, then return to Get started to join.";return;}
            if(!await DisplayAlert("Join this household?","The household owner and members will see your account email. Your personal list and private recipes stay separate. This uses your invitation once and does not start a subscription.","Join household","Cancel"))return;
            AccountSession.Check(account);await HouseholdService.CallAsync<object>(new{action="join",code=invitation});AccountSession.Check(account);if(OnboardingState.PendingCode==invitation)OnboardingState.ClearCode();status.Text="You're in! Open My household from Account or choose Household in Shopping.";code.Text="";
        }catch(Exception e){status.Text=e is InvalidOperationException or HouseholdException?e.Message:"Couldn't join right now. Your code is kept; check your connection and retry.";}finally{busy=false;}
    }
    private async Task Finish(string route){OnboardingState.Complete();await AppNavigation.Root(route);}
}
