using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
namespace RecipeApp.Views;

public abstract class BusinessPage:ContentPage
{
    protected readonly string Account=AccountSession.UserId;
    protected VerticalStackLayout Body=new(){Padding=20,Spacing=14};protected Label Status=ShoppingUi.Text("");
    protected BusinessPage(string title){Title=title;ShoppingUi.Theme(this);Content=new ScrollView{Content=Body};}
    protected void Check()=>AccountSession.Check(Account);
    protected Button Button(string text,Func<Task> action)=>FoodFeedPage.Button(text,async()=>{try{Check();await action();Check();}catch(InvalidOperationException e){await DisplayAlert("Business tools",e.Message,"OK");}});
    protected void Reset(string heading){Body.Clear();Body.Add(ShoppingUi.Text(heading,26,true));Body.Add(Status);}
    protected async Task Guard(Func<Task> action){try{Check();await action();Check();}catch(HouseholdException e){Status.Text=e.Message;}catch(InvalidOperationException e){Status.Text=e.Message;}catch(Exception e){Status.Text="Business tools unavailable. Retry shortly.\nReference: "+AppDiagnostics.Report(e,"ui.action");}}
    protected View ProfileHeader(BusinessProfile b){var box=new VerticalStackLayout{Spacing=8};if(!string.IsNullOrEmpty(b.Logo)){var logo=RecipeCardView.Photo(b.Logo,100);if(logo!=null)box.Add(logo);}box.Add(ShoppingUi.Text(b.Name,23,true));box.Add(ShoppingUi.Text((b.Kind=="creator"?"Independent creator":"Organisation")+(b.Verified?" · Verified business":" · Awaiting business verification"),13));box.Add(ShoppingUi.Text(b.Description??""));return ShoppingUi.Card(box);}
}

public class BusinessHubPage:BusinessPage
{
    public BusinessHubPage():base("Business & creators"){}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(Load);}
    private async Task Load(){
        Reset("Mealsy Business");Body.Add(ShoppingUi.Text("Publish recipe collections, share weekly meal plans and manage your team and campaigns."));
        Body.Add(Button("Explore businesses & creators",()=>Navigation.PushAsync(new BusinessDirectoryPage())));
        Body.Add(Button("Create a creator or organisation profile",()=>Navigation.PushAsync(new BusinessProfileEditorPage())));
        Body.Add(Button("Accept team invitation",async()=>{var code=await DisplayPromptAsync("Team invitation","Paste the invitation code shared with you.",maxLength:100);if(string.IsNullOrWhiteSpace(code))return;await BusinessService.Call<BusinessCreated>(new{action="join",code=code.Trim()});await Load();}));
        var result=await BusinessService.Call<BusinessList>(new{action="mine"});Check();
        foreach(var b in result.Businesses){Body.Add(ProfileHeader(b));Body.Add(Button("Manage · "+b.Role,()=>Navigation.PushAsync(new BusinessWorkspacePage(b.Id))));}
        Status.Text=result.Businesses.Count==0?"No creator or business profiles yet.":"Your creator and business accounts";
    }
}

public class BusinessProfileEditorPage:BusinessPage
{
    private readonly BusinessProfile existing;private string logo;
    public BusinessProfileEditorPage(BusinessProfile existing=null):base(existing==null?"Create business profile":"Edit business profile"){
        this.existing=existing;logo=existing?.Logo??"";Reset("Your public profile");
        var name=ShoppingUi.Entry(existing?.Name??"","Business or creator name",_=>{},80);var description=new Editor{Text=existing?.Description??"",Placeholder="Tell people about your recipes",MaxLength=1000,AutoSize=EditorAutoSizeOption.TextChanges};var website=ShoppingUi.Entry(existing?.Website??"","https://your-website",_=>{},1000);
        var kind=new Picker{Title="Account type",ItemsSource=new[]{"Independent creator","Organisation"},SelectedIndex=existing?.Kind=="organisation"?1:0,IsEnabled=existing==null};
        Body.Add(name);Body.Add(description);Body.Add(website);Body.Add(kind);
        Body.Add(Button("Choose logo",async()=>{var file=await MediaPicker.Default.PickPhotoAsync();if(file==null)return;using var stream=await file.OpenReadAsync();logo=await FeedPhoto.PrepareAsync(stream);Status.Text="Logo ready";}));
        Body.Add(Button("Remove logo",()=>{logo="";Status.Text="Logo removed";return Task.CompletedTask;}));
        Body.Add(ShoppingUi.Text("Public profile text and logo are safety checked. Administrators verify business identity separately from email confirmation. Changing your name or website requires verification again.",13));
        Body.Add(Button("Save profile",async()=>{await BusinessService.Call<BusinessCreated>(new{action=existing==null?"create":"updateProfile",businessId=existing?.Id,name=name.Text??"",description=description.Text??"",website=website.Text??"",logo,kind=kind.SelectedIndex==0?"creator":"organisation"});await Navigation.PopAsync();}));
    }
}

public class BusinessWorkspacePage:BusinessPage
{
    private readonly string businessId;
    public BusinessWorkspacePage(string id):base("Business workspace"){businessId=id;}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(Load);}
    private async Task Load(){
        var w=await BusinessService.Call<BusinessWorkspace>(new{action="workspace",businessId});Check();Reset(w.Business.Name);Body.Add(ProfileHeader(w.Business));
        Body.Add(ShoppingUi.Text("Your role: "+w.Role,13));
        if(w.Role=="owner"){Body.Add(Button("Edit public profile",()=>Navigation.PushAsync(new BusinessProfileEditorPage(w.Business))));Body.Add(Button("Manage team",()=>Navigation.PushAsync(new BusinessTeamPage(businessId))));}
        if(w.Role is "owner" or "campaignManager"){Body.Add(Button("Performance dashboard",()=>Navigation.PushAsync(new BusinessAnalyticsPage(businessId))));Body.Add(Button("Promotion credits & pricing",()=>Navigation.PushAsync(new CampaignWalletPage(businessId))));Body.Add(Button("Create promotion campaign",()=>Navigation.PushAsync(new BusinessCampaignPage(w))));}
        if(w.Role is "owner" or "editor"){
            Body.Add(Button("Publish or import recipes",()=>Navigation.PushAsync(new BusinessRecipeImportPage(w))));
            Body.Add(Button("Create collection / weekly plan",()=>Navigation.PushAsync(new BusinessCollectionEditorPage(w))));
        }
        Body.Add(ShoppingUi.Text("Collections & weekly plans",22,true));
        foreach(var c in w.Collections){Body.Add(ShoppingUi.Text(c.Title+(c.Published?" · Published":" · Draft"),17,true));if(w.Role is "owner" or "editor")Body.Add(Button("Edit collection",()=>Navigation.PushAsync(new BusinessCollectionEditorPage(w,c))));if(c.Published)Body.Add(Button("Preview public collection",()=>Navigation.PushAsync(new BusinessCollectionPage(c.Id))));}
        Body.Add(ShoppingUi.Text("Campaigns",22,true));Body.Add(ShoppingUi.Text("Paid checkout is not enabled. Campaigns can be prepared and paused; setting a budget does not charge money or activate paid placement.",13));
        foreach(var c in w.Campaigns){Body.Add(ShoppingUi.Text($"{c.Name} · {c.Status}\n£{c.BudgetMinor/100m:0.00} total · £{c.DailyBudgetMinor/100m:0.00}/day · Spent £{c.SpentMinor/100m:0.00} · {c.Impressions} billable impressions",14));if(w.Role is "owner" or "campaignManager"){
            Body.Add(Button("Edit campaign",()=>Navigation.PushAsync(new BusinessCampaignPage(w,c))));
            Body.Add(Button("Start / resume campaign",async()=>{await HouseholdService.CallEndpointAsync<object>(CampaignWalletPage.Endpoint,new{action="start",campaignId=c.Id});await Load();}));
            Body.Add(Button("Pause campaign",async()=>{await BusinessService.Call<object>(new{action="pauseCampaign",businessId,campaignId=c.Id});await Load();}));}}
        Body.Add(ShoppingUi.Text("Published recipe copies",22,true));
        foreach(var p in w.Posts){Body.Add(ShoppingUi.Text(p.Recipe?.Title??p.Caption,17,true));if(w.Role is "owner" or "editor")Body.Add(Button(p.Hidden?"Restore recipe":"Hide recipe",async()=>{await BusinessService.Call<object>(new{action="hideRecipe",businessId,postId=p.Id,hidden=!p.Hidden});await Load();}));}
    }
}

public class BusinessTeamPage:BusinessPage
{
    private readonly string businessId;
    public BusinessTeamPage(string id):base("Business team"){businessId=id;}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(Load);}
    private async Task Load(){
        var w=await BusinessService.Call<BusinessWorkspace>(new{action="workspace",businessId});Check();Reset("Your team");
        var email=ShoppingUi.Entry("","Existing member's account email",_=>{},320);var role=new Picker{ItemsSource=new[]{"Recipe editor","Campaign manager"},SelectedIndex=0};Body.Add(email);Body.Add(role);
        Body.Add(ShoppingUi.Text("Editors publish recipes and collections. Campaign managers see analytics and manage campaign plans. Only owners edit profiles and invite/remove members.",13));
        Body.Add(Button("Create invitation code",async()=>{var invite=await BusinessService.Call<BusinessInviteCode>(new{action="invite",businessId,email=email.Text??"",role=role.SelectedIndex==0?"editor":"campaignManager"});await Clipboard.Default.SetTextAsync(invite.Code);await DisplayAlert("Invitation copied","Share this code with that person. It expires in 48 hours and works only for their confirmed-email account.\n\n"+invite.Code,"OK");await Load();}));
        foreach(var member in w.Members){Body.Add(ShoppingUi.Text((member.Label??"Team member")+" · "+member.Role,13));if(member.Role!="owner")Body.Add(Button("Remove team member",async()=>{if(!await DisplayAlert("Remove member?","This removes their access to this business.","Remove","Cancel"))return;await BusinessService.Call<object>(new{action="removeMember",businessId,uid=member.Uid});await Load();}));}
        foreach(var invite in w.Invitations){Body.Add(ShoppingUi.Text("Pending invitation · "+invite.TargetUid+" · "+invite.Role,12));Body.Add(Button("Revoke invitation",async()=>{await BusinessService.Call<object>(new{action="revokeInvite",businessId,inviteId=invite.Id});await Load();}));}
    }
}

public class BusinessAnalyticsPage:BusinessPage
{
    private readonly string businessId;
    public BusinessAnalyticsPage(string id):base("Business performance"){businessId=id;}
    protected override async void OnAppearing(){base.OnAppearing();await Guard(async()=>{
        var stats=await BusinessService.Call<BusinessAnalytics>(new{action="analytics",businessId});Check();Reset("Your last 14 days");Body.Add(ShoppingUi.Text($"{stats.Followers} followers",22,true));
        Body.Add(ShoppingUi.Text("Estimated engagement: events are deduplicated per account, target and day; your team's own activity is excluded. These are aggregate counts, not unique visitor totals, verified conversions or billable advertising figures.",13));
        foreach(var d in stats.Days)Body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=5,Children={ShoppingUi.Text(d.Day,17,true),ShoppingUi.Text($"Profile views: {d.ProfileViews} · Post views: {d.PostViews}\nRecipe opens: {d.RecipeOpens} · Saves: {d.Saves}\nCollection opens: {d.CollectionOpens} · Plan saves: {d.PlanSaves}\nOffer clicks: {d.Clicks}",14)}}));
    });}
}

public class BusinessRecipeImportPage:BusinessPage
{
    private readonly BusinessWorkspace workspace;private readonly Editor input=new(){Placeholder="Paste an array of recipe snapshots as JSON",HeightRequest=180};private List<RecipeUpload> preview=new();private readonly VerticalStackLayout results=new(){Spacing=10};
    private record RecipeUpload(string Id,FeedRecipe Recipe,string Photo,string Caption);
    private bool publishing,cancelPublishing;
    private RecipeUpload Upload(FeedRecipe recipe,string photo)=>new(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Account+"|"+workspace.Business.Id+"|"+JsonSerializer.Serialize(recipe,BusinessService.Json)+"|"+photo))).ToLowerInvariant(),recipe,photo,recipe.Description??"");
    private static async Task<string> PreparePhoto(string value){if(string.IsNullOrEmpty(value))return"";try{using var stream=new MemoryStream(Convert.FromBase64String(value));return await FeedPhoto.PrepareAsync(stream);}catch(FormatException){throw new InvalidOperationException("Imported photos must be JPEG/PNG base64.");}}
    protected override void OnDisappearing(){if(publishing)cancelPublishing=true;base.OnDisappearing();}
    public BusinessRecipeImportPage(BusinessWorkspace w):base("Publish business recipes"){
        workspace=w;Reset("Preview before publishing");
        Body.Add(ShoppingUi.Text("Only verified businesses can publish. Every recipe and photo uses the same automatic safety checks as community posts. Publishing creates a public copy; private originals are kept.",13));
        Body.Add(Button("Choose one of my saved recipes",async()=>{
            var all=(await RecipeService.GetAllRecipes()).Where(r=>r.FirebaseUID==Account).ToList();Check();
            var options=all.Select((r,i)=>$"{i+1}. {r.RecipeName}").ToArray();if(options.Length==0)throw new InvalidOperationException("Save a recipe in your recipe book first.");
            var chosen=await DisplayActionSheet("Choose a recipe","Cancel",null,options);var index=Array.IndexOf(options,chosen);if(index<0)return;
            var r=all[index];var photo=await PreparePhoto(r.RecipeImage);Check();preview=new(){Upload(FeedRecipe.FromRecipe(r),photo)};RenderPreview();
        }));
        Body.Add(ShoppingUi.Text("Batch import · up to 20 recipes",18,true));Body.Add(ShoppingUi.Text("Each entry needs title, method, servings, prepMinutes and ingredients. Ingredient entries need name, quantity, category and note. Imported photos can be JPEG/PNG base64.",13));
        Body.Add(Button("Copy an example import file",async()=>await Clipboard.Default.SetTextAsync("[{\"title\":\"Vegetable soup\",\"method\":\"Simmer and serve.\",\"servings\":2,\"prepMinutes\":10,\"ingredients\":[{\"name\":\"Carrot\",\"quantity\":\"2\",\"category\":\"Produce\",\"note\":\"\"}]}]")));
        Body.Add(input);Body.Add(Button("Paste import JSON",async()=>input.Text=await Clipboard.Default.GetTextAsync()));
        Body.Add(Button("Preview batch",async()=>{
            if((input.Text??"").Length>500000)throw new InvalidOperationException("Import fewer recipes at a time.");
            try{var items=JsonSerializer.Deserialize<List<FeedRecipe>>(input.Text??"",BusinessService.Json);if(items==null||items.Count<1||items.Count>20||items.Any(r=>r==null||string.IsNullOrWhiteSpace(r.Title)||string.IsNullOrWhiteSpace(r.Method)||r.Servings<1||r.Ingredients==null||r.Ingredients.Any(i=>i==null)))throw new InvalidOperationException("Choose 1–20 complete recipes.");using var raw=JsonDocument.Parse(input.Text);var parsed=new List<RecipeUpload>();for(var i=0;i<items.Count;i++){var r=items[i] with{Ingredients=items[i].Ingredients.Select(a=>a with{Category=a.Category??"Other",Note=a.Note??"",Quantity=a.Quantity??""}).ToList()};var p=raw.RootElement[i].TryGetProperty("photo",out var image)&&image.ValueKind==JsonValueKind.String?image.GetString():"";var photo=await PreparePhoto(p);Check();parsed.Add(Upload(r,photo));}preview=parsed;RenderPreview();}catch(JsonException){throw new InvalidOperationException("The import JSON could not be read. Use the example format.");}
        }));Body.Add(results);
        Body.Add(Button("Publish previewed recipes",async()=>{
            if(preview.Count==0)throw new InvalidOperationException("Preview recipes first.");if(!await DisplayAlert("Publish publicly?",$"Publish {preview.Count} recipe copies as {workspace.Business.Name}? Each recipe is checked independently. Completed recipes remain published if a later one fails.","Publish","Cancel"))return;
            publishing=true;cancelPublishing=false;Body.IsEnabled=false;try{foreach(var upload in preview.ToList()){
                if(cancelPublishing)return;Check();
                await BusinessService.Call<object>(new{action="publishRecipe",businessId=workspace.Business.Id,id=upload.Id,recipe=upload.Recipe,photo=upload.Photo,caption=upload.Caption});
                preview.Remove(upload);RenderPreview();Status.Text="Recipe published. Remaining: "+preview.Count;
                if(preview.Count>0)await Task.Delay(10500);
            }
            if(!cancelPublishing){await DisplayAlert("Published","Your safety-approved recipes are now public.","OK");await Navigation.PopAsync();}}finally{publishing=false;Body.IsEnabled=true;}
        }));
    }
    private void RenderPreview(){results.Clear();foreach(var p in preview){var snapshot=new FoodPost{Recipe=p.Recipe,Photo=p.Photo,AuthorName=workspace.Business.Name};results.Add(ShoppingUi.Text(p.Recipe.Title+" · "+p.Recipe.Ingredients.Count+" ingredients",17,true));results.Add(Button("Review · "+p.Recipe.Title,()=>Navigation.PushAsync(new FeedRecipePage(snapshot))));}}
}
