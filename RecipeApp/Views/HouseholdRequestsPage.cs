using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class HouseholdRequestsPage:SocialPage
{
    private HouseholdInfo household;
    private HouseholdRequestsResult result;
    private bool history;
    public HouseholdRequestsPage():base("Household requests")
    {
        ToolbarItems.Add(new ToolbarItem("Refresh requests",null,async()=>await Run(Load)));
    }
    protected override async void OnAppearing(){base.OnAppearing();await Run(Load);}
    private async Task Load()
    {
        household=await HouseholdService.StatusAsync(true);Check();
        if(household.HouseholdId==null)throw new InvalidOperationException("Join a household first.");
        if(household.PermissionsVersion<1)throw new InvalidOperationException("Household requests need the updated Firebase household service. Ask the owner to update the backend.");
        result=await HouseholdService.CallAsync<HouseholdRequestsResult>(new {action="requests",householdId=household.HouseholdId});Check();Render();
    }
    private void Render()
    {
        Body.Clear();
        Body.Add(ShoppingUi.Text("Ideas for your household",26,true));
        Body.Add(ShoppingUi.Text("Owners and adults approve shopping additions and meals. Requests stay separate until approved; personal lists are unaffected.",14));
        var pending=result.Requests.Count(r=>r.Status=="pending");
        Body.Add(ShoppingUi.Text($"{pending} pending",18,true));
        if(household.FamilyEnabled)Body.Add(ShoppingUi.Button("Suggest a meal",()=>AppNavigation.Open(new HouseholdMealEditorPage(household.HouseholdId,DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek+6)%7)),date:DateTime.Today,requestOnly:true)),true));
        Body.Add(ShoppingUi.Button(history?"Show pending only":"Show recent decisions",()=>{history=!history;Render();return Task.CompletedTask;},true));
        var entries=result.Requests.Where(r=>history?r.Status!="pending":r.Status=="pending").ToList();
        if(entries.Count==0)Body.Add(ShoppingUi.Text(history?"No recent decisions yet.":"No requests waiting. Household members can add a shopping item or suggest a meal.",14));
        foreach(var request in entries)
        {
            var card=new VerticalStackLayout {Spacing=8};
            card.Add(ShoppingUi.Text(request.Kind=="meal"?request.Meal?.Title:request.Item?.Name,20,true));
            card.Add(ShoppingUi.Text(request.AuthorName+" · "+(request.Kind=="meal"?"Meal suggestion":"Shopping request")+" · "+request.Status,13));
            if(request.Kind=="shopping"&&request.Item!=null)
            {
                if(!string.IsNullOrWhiteSpace(request.Item.Quantity))card.Add(ShoppingUi.Text("Quantity: "+request.Item.Quantity,14));
                if(!string.IsNullOrWhiteSpace(request.Item.Note))card.Add(ShoppingUi.Text(request.Item.Note,14));
            }
            if(request.Meal!=null)
            {
                card.Add(ShoppingUi.Text($"{request.Meal.Date} · {request.Meal.Slot} · {request.Meal.Servings} portions",14));
                if(!string.IsNullOrWhiteSpace(request.Meal.Note))card.Add(ShoppingUi.Text(request.Meal.Note,14));
                if(request.Meal.Recipe!=null)card.Add(ShoppingUi.Button("Review attached recipe",()=>AppNavigation.Open(new RecipePreviewPage(RecipeDraft.FromRecipe(request.Meal.Recipe.ToRecipe("")))),true));
            }
            if(request.Status=="pending")
            {
                if(result.CanApprove)
                {
                    card.Add(ShoppingUi.Button(request.Kind=="meal"?"Approve & add to plan":"Approve & add to shopping",()=>Run(()=>Resolve(request,"approve"))));
                    card.Add(ShoppingUi.Button("Decline request",()=>Run(()=>Resolve(request,"decline")),true));
                }
                if(request.AuthorId==Account)card.Add(ShoppingUi.Button("Cancel my request",()=>Run(async()=>{
                    Check();await HouseholdService.CallAsync<object>(new {action="cancelRequest",householdId=household.HouseholdId,requestId=request.Id});Check();await Load();
                }),true));
            }
            Body.Add(ShoppingUi.Card(card));
        }
    }
    private async Task Resolve(HouseholdRequest request,string decision)
    {
        if(!await DisplayAlert(decision=="approve"?"Approve this request?":"Decline this request?",decision=="approve"?"This adds the item or meal to the shared household list or plan. Meal approval does not automatically add ingredients to shopping.":"The shared list and meal plan will stay unchanged.",decision=="approve"?"Approve":"Decline","Keep reviewing"))return;
        Check();await HouseholdService.CallAsync<object>(new {action="resolveRequest",householdId=household.HouseholdId,requestId=request.Id,decision});Check();await Load();
    }
}
