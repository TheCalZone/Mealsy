using RecipeApp.Services;

namespace RecipeApp.Views;

/// <summary>A local, dismissible invitation; never fetches billing or changes access.</summary>
internal sealed class PremiumBenefitsCard : ContentView
{
    private readonly string account=AccountSession.UserId;
    private bool attached;
    private string DismissKey=>"Mealsy.premium-prompt.hidden-until."+(account??"device");

    public PremiumBenefitsCard()
    {
        var body=new VerticalStackLayout{Spacing=10};
        body.Add(ShoppingUi.Text("MAKE MORE OF MEALSY",12,true));
        body.Add(ShoppingUi.Text("Less planning. More enjoying dinner.",23,true));
        body.Add(ShoppingUi.Text("Premium saves time whether you're cooking for yourself or feeding the family.",15));
        body.Add(ShoppingUi.Text("↗  Import recipes from supported websites\n↻  Reuse favourite weekly meal plans\n✓  Bring back saved shopping trips\n⌂  Share shopping lists and meal plans with your household\n♡  Enjoy an ad-free household",15));
        body.Add(ShoppingUi.Text("One membership covers up to four people, including you. Your personal recipes and lists stay private. Basic personal recipes, shopping and meal planning stay free.",13));
        body.Add(ShoppingUi.Button(ClientPlatform.StorePurchasesAvailable?"Explore Premium · Plans & benefits":"Explore Premium benefits",()=>{
            AccountSession.Check(account);
            return AppNavigation.Open(new UpgradeFeaturesPage());
        }));
        body.Add(ShoppingUi.Text(ClientPlatform.StorePurchasesAvailable?"Monthly or annual options · See the current local price before subscribing.":ClientPlatform.IosPurchaseNotice,12));
        body.Add(ShoppingUi.Button("Not now",()=>{
            AccountSession.Check(account);
            Preferences.Set(DismissKey,DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds().ToString());
            Update();return Task.CompletedTask;
        },true));
        Content=ShoppingUi.Card(body);
        Loaded+=(_,_)=>{if(!attached){PlanService.Changed+=PlanChanged;attached=true;}Update();};
        Unloaded+=(_,_)=>{if(attached){PlanService.Changed-=PlanChanged;attached=false;}};
        Update();
    }
    private void PlanChanged()=>MainThread.BeginInvokeOnMainThread(Update);
    private void Update()
    {
        var plan=PlanService.Cached;
        // Hide for owners, covered household members and administrator grants,
        // including when a cached entitlement is still active while offline.
        var active=PremiumEntitlement.IsActive(plan,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var dismissed=long.TryParse(Preferences.Get(DismissKey,"0"),out var until)&&until>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        IsVisible=account==AccountSession.UserId&&!active&&!dismissed;
    }
}
