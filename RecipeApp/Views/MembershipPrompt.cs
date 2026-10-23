using RecipeApp.Services;

namespace RecipeApp.Views;

/// <summary>Contextual account/Premium invitation. Reads local status only.</summary>
public sealed class MembershipPrompt : ContentView
{
    private readonly bool meals;
    private bool attached;
    private string rendered;
    public MembershipPrompt():this(false){}
    public MembershipPrompt(bool meals)
    {
        this.meals=meals;
        Loaded+=(_,_)=>{if(!attached){attached=true;PlanService.Changed+=Changed;}Refresh();};
        Unloaded+=(_,_)=>{if(attached){attached=false;PlanService.Changed-=Changed;}};
        Refresh();
    }
    private void Changed()=>MainThread.BeginInvokeOnMainThread(Refresh);
    public void Refresh()
    {
        var account=AccountSession.UserId;
        var key="Mealsy.context-invitation.hidden-until."+(account??"device");
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        IsVisible=!PremiumEntitlement.IsActive(PlanService.Cached,now)
            &&!(long.TryParse(Preferences.Get(key,"0"),out var until)&&until>now);
        var state=(account??"device")+"|"+meals;
        if(rendered==state)return;
        rendered=state;
        var body=new VerticalStackLayout{Spacing=5};
        body.Add(ShoppingUi.Text(account==null?"Make it yours with a free account":meals?"Plan once. Enjoy it together.":"Your shopping, ready to share",16,true));
        body.Add(ShoppingUi.Text(account==null
            ?meals?"Keep your personal planner free. Sign up to get started; shared household plans are available with Premium.":"Create a free account to sync your personal shopping list. Household cloud sharing and saved shopping trips are Premium extras."
            :meals?"Premium adds reusable weekly templates and a shared household meal plan. Your personal planner stays free.":"Upgrade for more personal shopping lists, reusable saved trips and a shared household list. Your personal lists stay separate.",13));
        if(account!=null&&ClientPlatform.StorePurchasesAvailable)body.Add(ShoppingUi.Text("Check plans for an eligible 7-day free trial. Renewal price and terms are shown before checkout.",12));
        var actions=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)},ColumnSpacing=8};
        actions.Add(ShoppingUi.Button(account==null?"Create free account":ClientPlatform.StorePurchasesAvailable?"Explore Premium":"See Premium benefits",()=>{
            AccountSession.Check(account);
            return account==null?AppNavigation.Open(new SignupPage()):AppNavigation.Open(new UpgradeFeaturesPage());
        }),0);
        actions.Add(ShoppingUi.Button("Not now",()=>{
            AccountSession.Check(account);Preferences.Set(key,DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds().ToString());Refresh();return Task.CompletedTask;
        },true),1);
        body.Add(actions);Content=ShoppingUi.Card(body);
    }
}
