using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;

public class BusinessDirectoryPage:BusinessPage
{
    private FeedCursor cursor;private readonly Button more;
    public BusinessDirectoryPage():base("Businesses & creators"){more=Button("Load more",()=>Load(false));}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(()=>Load(true));}
    private async Task Load(bool reset){if(reset){Reset("Find your next favourite creator");cursor=null;}else Body.Remove(more);var result=await BusinessService.Call<BusinessList>(new{action="directory",cursor});Check();foreach(var b in result.Businesses){Body.Add(ProfileHeader(b));Body.Add(Button("View recipes & collections",()=>Navigation.PushAsync(new BusinessPublicPage(b.Id))));}cursor=result.Cursor;more.IsVisible=cursor!=null;Body.Add(more);Status.Text=result.Businesses.Count==0?"No profiles on this page yet. Load more if available.":"Verified creators and organisations";}
}

public class BusinessPublicPage:BusinessPage
{
    private readonly string businessId;
    public BusinessPublicPage(string id):base("Creator profile"){businessId=id;}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(Load);}
    private async Task Load(){var p=await BusinessService.Call<PublicBusiness>(new{action="publicProfile",businessId});Check();Reset(p.Business.Name);Body.Add(ProfileHeader(p.Business));_=BusinessService.Track(businessId,"profileViews");
        Body.Add(Button(p.Following?"Unfollow this creator":"Follow this creator",async()=>{await BusinessService.Call<object>(new{action="follow",businessId,following=!p.Following});await Load();}));
        if(!string.IsNullOrEmpty(p.Business.Website))Body.Add(Button("Visit website",async()=>{if(await DisplayAlert("Open website?",p.Business.Website,"Open","Cancel"))await Launcher.Default.OpenAsync(p.Business.Website);}));
        foreach(var c in p.Collections){Body.Add(ShoppingUi.Text(c.Title,21,true));Body.Add(ShoppingUi.Text(c.Description??""));Body.Add(Button("Share collection / meal plan…",()=>ContentSharing.SharePlan(c.Id)));Body.Add(Button(c.Meals.Count>0?"View collection & weekly plan":"View recipe collection",()=>Navigation.PushAsync(new BusinessCollectionPage(c.Id))));}
        if(p.Collections.Count==0)Body.Add(ShoppingUi.Text("No public collections yet."));
    }
}

public class BusinessCollectionEditorPage:BusinessPage
{
    private readonly BusinessWorkspace workspace;private readonly BusinessCollection original;private readonly Entry title;private readonly Editor description;private readonly Entry offerLabel,offerUrl,offerCode;
    private readonly HashSet<string> selected;private readonly List<BusinessMeal> meals;private readonly VerticalStackLayout weekly=new(){Spacing=8};private readonly string collectionId;
    public BusinessCollectionEditorPage(BusinessWorkspace w,BusinessCollection existing=null):base("Collection & weekly plan"){
        workspace=w;original=existing;collectionId=existing?.Id??Guid.NewGuid().ToString("N");selected=(existing?.PostIds??new()).ToHashSet();meals=existing?.Meals.ToList()??new();Reset("Recipes worth collecting");
        title=ShoppingUi.Entry(existing?.Title??"","Collection title",_=>{},120);description=new Editor{Text=existing?.Description??"",Placeholder="Collection introduction",MaxLength=1000,AutoSize=EditorAutoSizeOption.TextChanges};Body.Add(title);Body.Add(description);
        var available=w.Posts.Where(p=>!p.Hidden&&p.Recipe!=null).ToList();
        Body.Add(ShoppingUi.Text("Choose recipes",20,true));foreach(var p in available){var row=new HorizontalStackLayout{Spacing=8};row.Add(ShoppingUi.Check(selected.Contains(p.Id),value=>{if(value)selected.Add(p.Id);else{selected.Remove(p.Id);meals.RemoveAll(m=>m.PostId==p.Id);RenderMeals();}}));row.Add(ShoppingUi.Text(p.Recipe.Title));Body.Add(row);}
        Body.Add(ShoppingUi.Text("Optional weekly meal plan",20,true));Body.Add(ShoppingUi.Text("Add collection recipes to up to 21 day/meal slots. Users can preview and change days and servings before adding them to their own plan.",13));Body.Add(weekly);
        Body.Add(Button("Add a meal slot",async()=>{
            var recipes=available.Where(p=>selected.Contains(p.Id)).ToList();if(recipes.Count==0)throw new InvalidOperationException("Select collection recipes first.");
            var days=Enumerable.Range(0,7).Select(i=>"Day "+(i+1)).ToArray();var day=Array.IndexOf(days,await DisplayActionSheet("Which day?","Cancel",null,days));if(day<0)return;
            var slot=Array.IndexOf(MealPlanning.Slots,await DisplayActionSheet("Which meal?","Cancel",null,MealPlanning.Slots));if(slot<0)return;
            var labels=recipes.Select((p,i)=>$"{i+1}. {p.Recipe.Title}").ToArray();var index=Array.IndexOf(labels,await DisplayActionSheet("Choose recipe","Cancel",null,labels));if(index<0)return;
            meals.RemoveAll(m=>m.Day==day&&m.Slot==slot);meals.Add(new(day,slot,recipes[index].Id));RenderMeals();
        }));
        Body.Add(ShoppingUi.Text("Optional commercial offer",20,true));offerLabel=ShoppingUi.Entry(existing?.Offer?.Label??"","e.g. View this week's recipe box",_=>{},100);offerUrl=ShoppingUi.Entry(existing?.Offer?.Url??"","https://offer-link",_=>{},1000);offerCode=ShoppingUi.Entry(existing?.Offer?.Code??"","Offer code · optional",_=>{},80);Body.Add(offerLabel);Body.Add(offerUrl);Body.Add(offerCode);
        Body.Add(Button("Save draft",()=>Save(false)));Body.Add(Button("Publish collection",async()=>{if(await DisplayAlert("Publish collection?","The selected recipes, meal plan and offer will be visible to signed-in users.","Publish","Cancel"))await Save(true);}));RenderMeals();
    }
    private void RenderMeals(){weekly.Clear();foreach(var m in meals.OrderBy(m=>m.Day).ThenBy(m=>m.Slot)){var p=workspace.Posts.FirstOrDefault(p=>p.Id==m.PostId);weekly.Add(ShoppingUi.Text($"Day {m.Day+1} · {MealPlanning.Slots[m.Slot]} · {p?.Recipe?.Title}",14));weekly.Add(Button("Remove meal slot",()=>{meals.Remove(m);RenderMeals();return Task.CompletedTask;}));}}
    private async Task Save(bool published){await BusinessService.Call<BusinessCreated>(new{action="saveCollection",businessId=workspace.Business.Id,id=collectionId,title=title.Text??"",description=description.Text??"",postIds=selected.ToArray(),meals,offer=new{label=offerLabel.Text??"",url=offerUrl.Text??"",code=offerCode.Text??""},published});await Navigation.PopAsync();}
}

public class BusinessCollectionPage:BusinessPage
{
    private readonly string collectionId;private bool imported,loaded,refreshing;private List<BusinessPlanChoice> choices=new();
    public BusinessCollectionPage(string id):base("Recipe collection"){collectionId=id;Reset("Opening shared recipes & meal plan…");ToolbarItems.Add(new ToolbarItem("Refresh",null,async()=>await Guard(Refresh)));}
    protected override async void OnAppearing(){base.OnAppearing();if(!loaded)await Guard(Refresh);}
    private async Task Refresh(){if(refreshing)return;refreshing=true;Reset("Opening shared recipes & meal plan…");var progress=new LoadingIndicator("Loading creator recipes…");Body.Add(progress);try{await Load();loaded=true;}finally{Body.Remove(progress);refreshing=false;}}
    private async Task Load(){
        var d=await BusinessService.Call<BusinessCollectionDetail>(new{action="collection",collectionId});Check();Reset(d.Collection.Title);Body.Add(ProfileHeader(d.Business));Body.Add(ShoppingUi.Text(d.Collection.Description??""));Body.Add(Button("Share this collection / meal plan…",()=>ContentSharing.SharePlan(collectionId)));_=BusinessService.Track(d.Business.Id,"collectionOpens",collectionId);
        foreach(var post in d.Posts){Body.Add(ShoppingUi.Text(post.Recipe.Title,20,true));Body.Add(Button("View recipe",async()=>{_=BusinessService.Track(d.Business.Id,"recipeOpens",post.Id);await Navigation.PushAsync(new FeedRecipePage(post));}));Body.Add(Button("Save a private copy",async()=>{var all=await RecipeService.GetAllRecipes();Check();var key="business-copy-"+post.Id;if(!all.Any(r=>r.FirebaseUID==Account&&r.CloudKey==key)){var recipe=post.PrivateCopy(Account);recipe.CloudKey=key;await RecipeService.AddRecipe(recipe);if(Account!=null)_=AccountSession.SyncSafely();}_=BusinessService.Track(d.Business.Id,"saves",post.Id);await DisplayAlert("Saved","A private recipe copy is in your recipe book.","OK");}));}
        if(d.Collection.Meals.Count>0){
            await RecipeService.CreateMealPlan(Account);var plan=await RecipeService.GetMealPlan(Account);Check();Body.Add(ShoppingUi.Text("Add to your weekly plan",22,true));Body.Add(ShoppingUi.Text("Select meals and adjust their days, meal slots and servings. These will be added alongside existing meals and notes.",13));
            choices=d.Collection.Meals.Select(m=>new BusinessPlanChoice{Post=d.Posts.Single(p=>p.Id==m.PostId),Day=m.Day,Slot=m.Slot,Servings=d.Posts.Single(p=>p.Id==m.PostId).Recipe.Servings.ToString()}).ToList();
            var dayNames=Enumerable.Range(0,7).Select(i=>plan.WeekStart.AddDays(i).ToString("ddd d MMM")).ToArray();
            foreach(var choice in choices){var box=new VerticalStackLayout{Spacing=7};box.Add(new HorizontalStackLayout{Children={ShoppingUi.Check(true,v=>choice.Selected=v),ShoppingUi.Text(choice.Post.Recipe.Title,17,true)}});var day=new Picker{ItemsSource=dayNames,SelectedIndex=choice.Day};day.SelectedIndexChanged+=(_,_)=>choice.Day=day.SelectedIndex;var slot=new Picker{ItemsSource=MealPlanning.Slots,SelectedIndex=choice.Slot};slot.SelectedIndexChanged+=(_,_)=>choice.Slot=slot.SelectedIndex;box.Add(day);box.Add(slot);box.Add(ShoppingUi.Entry(choice.Servings,"Servings",s=>choice.Servings=s,4,true));Body.Add(ShoppingUi.Card(box));}
            Body.Add(Button("Save recipes & shop selected meals",async()=>{
                var totals=CreatorPlanSelection.ShoppingServings(choices);
                if(!await DisplayAlert("Save recipes & review shopping?","Save private recipe copies and review ingredients for the selected meals and portions? Check your cupboard before adding items. Your meal plan and current shopping list are kept.","Review shopping","Cancel"))return;
                var copies=await BusinessPlanImport.SaveRecipes(choices,Account);Check();
                var shopping=totals.Select(pair=>new RecipeShoppingChoice(copies[pair.Key]){Selected=true,Servings=pair.Value.ToString()}).ToList();
                await AppNavigation.Open(new ShoppingListMakerPage(shopping));
            }));
            Body.Add(Button("Add selected meals to my plan",async()=>{if(imported)throw new InvalidOperationException("This selection was already added. View your meal planner to make changes.");BusinessPlanImport.Validate(choices);if(!await DisplayAlert("Add meals?","Save private recipe copies and add these meals to your current week? Existing meals are kept.","Add meals","Cancel"))return;await BusinessPlanImport.Apply(choices,Account);imported=true;_=BusinessService.Track(d.Business.Id,"planSaves",collectionId);await DisplayAlert("Meals added","Open your meal planner to review the week and create a shopping list with cupboard checks.","OK");}));
        }
        if(!string.IsNullOrEmpty(d.Collection.Offer?.Url)){
            Body.Add(ShoppingUi.Text("Commercial offer",18,true));Body.Add(ShoppingUi.Text(d.Collection.Offer.Label));if(!string.IsNullOrEmpty(d.Collection.Offer.Code))Body.Add(ShoppingUi.Text("Offer code: "+d.Collection.Offer.Code,16,true));
            Body.Add(Button("Open offer website",async()=>{if(!await DisplayAlert("Open commercial offer?",d.Collection.Offer.Url,"Open","Cancel"))return;await Launcher.Default.OpenAsync(d.Collection.Offer.Url);_=BusinessService.Track(d.Business.Id,"clicks",collectionId);}));
        }
    }
}

public class BusinessCampaignPage:BusinessPage
{
    public BusinessCampaignPage(BusinessWorkspace w,BusinessCampaign existing=null):base("Promotion campaign"){
        Reset("Prepare a promotion");var name=ShoppingUi.Entry(existing?.Name??"","Campaign name",_=>{},100);var recipes=w.Posts.Where(p=>!p.Hidden&&p.Recipe!=null).ToList();var picker=new Picker{Title="Recipe to promote",ItemsSource=recipes.Select(p=>p.Recipe.Title).ToList(),SelectedIndex=recipes.Count>0?0:-1};var budget=ShoppingUi.Entry((existing?.BudgetMinor/100m??10m).ToString(),"Total budget (£)",_=>{},8,true);var daily=ShoppingUi.Entry((existing?.DailyBudgetMinor/100m??2m).ToString(),"Daily budget (£)",_=>{},8,true);var start=new DatePicker{Date=DateTime.Today};var end=new DatePicker{Date=DateTime.Today.AddDays(7)};
        var noEnd=new Switch{IsToggled=existing==null||existing.EndsAt==0};end.IsEnabled=!noEnd.IsToggled;noEnd.Toggled+=(_,e)=>end.IsEnabled=!e.Value;if(existing!=null){picker.SelectedIndex=recipes.FindIndex(p=>p.Id==existing.PostId);start.Date=DateTimeOffset.FromUnixTimeMilliseconds(existing.StartsAt).LocalDateTime.Date;if(existing.EndsAt>0)end.Date=DateTimeOffset.FromUnixTimeMilliseconds(existing.EndsAt).LocalDateTime.Date.AddDays(-1);}Body.Add(ShoppingUi.Text("Run without an end date",13));Body.Add(noEnd);Body.Add(ShoppingUi.Text("£1 per 1,000 qualifying impressions. Daily and total spending limits still apply.",13));Body.Add(name);Body.Add(picker);Body.Add(ShoppingUi.Text("Total budget in GBP",13));Body.Add(budget);Body.Add(ShoppingUi.Text("Daily budget in GBP",13));Body.Add(daily);Body.Add(ShoppingUi.Text("Start date",13));Body.Add(start);Body.Add(ShoppingUi.Text("End date",13));Body.Add(end);
        Body.Add(ShoppingUi.Text("Paid checkout is not enabled. This saves a campaign plan awaiting payment integration. No money is charged and no paid placement starts. Live activation will require verified payment and enforce these limits.",13));
        Body.Add(Button("Preview placement",async()=>{if(picker.SelectedIndex<0)throw new InvalidOperationException("Choose a recipe first.");var post=recipes[picker.SelectedIndex];await DisplayAlert("Placement preview · Promoted",w.Business.Name+" · Verified business\n\n"+post.Recipe.Title+"\n"+post.Caption,"OK");}));
        var campaignId=existing?.Id??Guid.NewGuid().ToString("N");Body.Add(Button("Save campaign plan",async()=>{if(picker.SelectedIndex<0)throw new InvalidOperationException("Choose a recipe first.");if(!decimal.TryParse(budget.Text,out var total)||!decimal.TryParse(daily.Text,out var perDay)||total*100!=decimal.Truncate(total*100)||perDay*100!=decimal.Truncate(perDay*100))throw new InvalidOperationException("Enter budgets with up to two decimal places.");await BusinessService.Call<object>(new{action="saveCampaign",businessId=w.Business.Id,id=campaignId,name=name.Text??"",postId=recipes[picker.SelectedIndex].Id,budgetMinor=(long)(total*100),dailyBudgetMinor=(long)(perDay*100),startsAt=new DateTimeOffset(start.Date??throw new InvalidOperationException("Choose a start date.")).ToUnixTimeMilliseconds(),endsAt=noEnd.IsToggled?0:new DateTimeOffset((end.Date??throw new InvalidOperationException("Choose an end date.")).AddDays(1)).ToUnixTimeMilliseconds()});await Navigation.PopAsync();}));
    }
}

public class BusinessVerificationPage:BusinessPage
{
    private FeedCursor cursor;private readonly Button more;
    public BusinessVerificationPage():base("Business verification"){more=Button("Load more",()=>Load(false));}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(()=>Load(true));}
    private async Task Load(bool reset){if(reset){Reset("Verify business identities");cursor=null;Body.Add(ShoppingUi.Text("Check that the applicant controls the organisation or creator identity and website. Email confirmation alone is not business verification.",13));}else Body.Remove(more);var page=await BusinessService.Call<BusinessList>(new{action="adminBusinesses",cursor});Check();foreach(var b in page.Businesses){Body.Add(ProfileHeader(b));Body.Add(ShoppingUi.Text("Owner account: "+b.Owner+"\nWebsite: "+b.Website,12));Body.Add(Button(b.Verified?"Revoke verification":"Approve business verification",async()=>{var reason=await DisplayPromptAsync("Verification decision","Record how identity was checked or why verification is being removed.",maxLength:500);if(string.IsNullOrWhiteSpace(reason))return;await BusinessService.Call<object>(new{action="verifyBusiness",businessId=b.Id,verified=!b.Verified,reason});await Load(true);}));}cursor=page.Cursor;more.IsVisible=cursor!=null;Body.Add(more);}
}
