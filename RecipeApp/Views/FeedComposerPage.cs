using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;

namespace RecipeApp.Views;

public class FeedComposerPage : ContentPage
{
    private readonly Entry name = new() { Placeholder = "Choose your username in Account", MaxLength = 40, IsReadOnly = true };
    private readonly Editor caption = new() { Placeholder = "What have you been cooking?", MaxLength = 2000, HeightRequest = 140 };
    private readonly Label attachment = FoodFeedPage.Text("No recipe attached");
    private readonly Label photoStatus = FoodFeedPage.Text("No photo attached");
    private readonly Picker audience=new(){Title="Who can see this?",ItemsSource=SocialPolicy.AudienceLabels,SelectedIndex=1,HeightRequest=48};
    private readonly string account;
    private FeedRecipe recipe;
    private string photo = "", postId = Guid.NewGuid().ToString("N");
    private bool published, loading, sourceLoaded;
    private int preparingPhotos;
    private string DraftKey => "Feed.draft." + account;
    public FeedComposerPage(Recipe source = null)
    {
        Title = "Share a meal"; ShoppingUi.Theme(this); account = HouseholdService.UserId;
        name.Text = Preferences.Get(FoodFeedService.AuthorKey, "");
        var body = new VerticalStackLayout { Padding = 20, Spacing = 14, Children = {
            FoodFeedPage.Text("Something worth sharing", 27, true),
            FoodFeedPage.Text("Your account username is used for authorship. Automated safety checks review submitted text and photos before publication. Confirm your email in Account to share."), name, audience, FoodFeedPage.Text("Choose an audience before posting. Friends and Only me require the privacy-aware server. They will never silently publish publicly.",13),caption, attachment,
            FoodFeedPage.Button("Attach a recipe from my book", async () => {
                CheckAccount();
                var recipes = RecipeBrowser.Visible(RecipeService.Recipes,account).ToList();
                if (recipes.Count == 0) { await DisplayAlert("No recipes yet", "Create a recipe in your recipe book first.", AppLocalization.Text("OK")); return; }
                var picker = new Picker { Title = "Choose recipe", ItemsSource = recipes, ItemDisplayBinding = new Binding(nameof(Recipe.RecipeName)) };
                var chooser = new ContentPage { Title = "Choose a recipe" };
                chooser.Content = new VerticalStackLayout { Padding = 20, Children = { picker, FoodFeedPage.Button("Attach selected recipe", async () => {
                    if (picker.SelectedItem is not Recipe selected) return;
                    CheckAccount(); await Attach(selected); await Navigation.PopAsync();
                }) } };
                await Navigation.PushAsync(chooser);
            }),
            FoodFeedPage.Button("Remove attached recipe", () => { recipe = null; attachment.Text = "No recipe attached"; SaveDraft(); return Task.CompletedTask; }),
            photoStatus,
            FoodFeedPage.Button("Choose a food photo", async () => {
                CheckAccount(); var file = await MediaPicker.Default.PickPhotoAsync(); if (file == null) return;
                preparingPhotos++;
                try { using var stream = await file.OpenReadAsync(); photo = await FeedPhoto.PrepareAsync(stream); photoStatus.Text = "Photo ready · location metadata removed"; SaveDraft(); }
                finally { preparingPhotos--; }
            }),
            FoodFeedPage.Button("Remove photo", () => { photo = ""; photoStatus.Text = "No photo attached"; SaveDraft(); return Task.CompletedTask; }),
            FoodFeedPage.Button("Publish post", Publish),
            FoodFeedPage.Text("Only share recipes and photos you have permission to publish. Please keep comments kind and avoid sharing personal details.", 12)
        } };
        Content = new ScrollView { Content = body };
        loading = true;
        try
        {
            if (account != null && source == null)
            {
                var draft = JsonSerializer.Deserialize<FeedDraft>(Preferences.Get(DraftKey, "null"));
                if (draft != null) { postId = draft.Id; caption.Text = draft.Caption; recipe = draft.Recipe; photo = draft.Photo ?? "";audience.SelectedIndex=Array.IndexOf(SocialPolicy.Audiences,draft.Audience??"friends");if(audience.SelectedIndex<0)audience.SelectedIndex=1; }
            }
            if (source != null) { recipe = FeedRecipe.FromRecipe(source); caption.Text = "I've been cooking " + source.RecipeName + "."; }
            attachment.Text = recipe == null ? "No recipe attached" : "Attached: " + recipe.Title;
            if (!string.IsNullOrEmpty(photo)) photoStatus.Text = "Draft photo attached";
        }
        catch (JsonException) { attachment.Text = "Previous draft could not be read. A new draft is open."; }
        finally { loading = false; }
        audience.SelectedIndexChanged+=(_,_)=>SaveDraft();
        caption.TextChanged += (_, _) => SaveDraft(); name.TextChanged += (_, _) => SaveDraft();
        ToolbarItems.Add(new ToolbarItem("Discard draft", null, async () => {
            if (!await DisplayAlert("Discard this draft?", "This removes the unpublished draft from this device.", "Discard", "Keep editing")) return;
            if (account != null && account == HouseholdService.UserId) Preferences.Set(DraftKey, "null");
            published = true; await Navigation.PopAsync();
        }));
        if (source != null) Loaded += async (_, _) => { if (sourceLoaded) return; sourceLoaded = true; try { await Attach(source); } catch (Exception) { photoStatus.Text = "Recipe attached without a photo."; } };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { CheckAccount();var profile=await FoodFeedService.Profile();CheckAccount();name.Text=profile.Username??""; }
        catch(Exception){name.Text="";}
    }
    private async Task Attach(Recipe source)
    {
        recipe = FeedRecipe.FromRecipe(source); attachment.Text = "Attached: " + recipe.Title;
        if (!string.IsNullOrEmpty(source.RecipeImage))
        {
            preparingPhotos++;
            try { if (source.RecipeImage.Length > 20 * 1024 * 1024) throw new InvalidOperationException("Recipe photo is too large."); using var stream = new MemoryStream(Convert.FromBase64String(source.RecipeImage)); photo = await FeedPhoto.PrepareAsync(stream); photoStatus.Text = "Recipe photo attached · location metadata removed"; }
            catch (Exception) { photoStatus.Text = "Recipe attached. Choose a new photo if you want to add one."; }
            finally { preparingPhotos--; }
        }
        SaveDraft();
    }
    private void CheckAccount()
    {
        if (account == null || account != HouseholdService.UserId) throw new HouseholdException("UNAUTHENTICATED", "Sign in first, then reopen the composer.");
    }
    private void SaveDraft()
    {
        if (loading || published || account == null || account != HouseholdService.UserId) return;
        Preferences.Set(DraftKey, JsonSerializer.Serialize(new FeedDraft(postId, caption.Text ?? "", recipe, photo,SocialPolicy.Audiences[audience.SelectedIndex < 0 ? 1 : audience.SelectedIndex])));
        Preferences.Set(FoodFeedService.AuthorKey, name.Text ?? "");
    }
    private async Task Publish()
    {
        CheckAccount();
        if (preparingPhotos > 0) { await DisplayAlert("Preparing photo", "Wait for the photo to finish before publishing.", AppLocalization.Text("OK")); return; }
        if (string.IsNullOrWhiteSpace(name.Text) || (string.IsNullOrWhiteSpace(caption.Text) && recipe == null))
        { await DisplayAlert("Add a little more", "Choose a public display name and write a caption or attach a recipe.", AppLocalization.Text("OK")); return; }
        var visibility=SocialPolicy.Audience(SocialPolicy.Audiences[audience.SelectedIndex < 0 ? 1 : audience.SelectedIndex]);
        if (!await DisplayAlert("Confirm post audience", "Audience: "+SocialPolicy.AudienceLabel(visibility)+". This includes any attached photo and recipe snapshot. Previously delivered content cannot be recalled.", "Publish", "Keep editing")) return;
        CheckAccount(); SaveDraft();
        Content.IsEnabled = false;
        try {
        if(SocialPolicy.NeedsPrivacyService(visibility))await SocialService.Publish(postId,caption.Text??"",recipe,photo,visibility);
        else await HouseholdService.CallEndpointAsync<object>(FoodFeedService.Endpoint,new { action = "publish", id = postId, caption = caption.Text ?? "", authorName = name.Text.Trim(),
            recipe = recipe == null ? (JsonElement?)null : JsonSerializer.SerializeToElement(recipe, FoodFeedService.Json), photo,audience=visibility },true);
        published = true; Preferences.Set(DraftKey, "null"); await Navigation.PopAsync();
        } finally { Content.IsEnabled = true; }
    }
    protected override void OnDisappearing() { SaveDraft(); base.OnDisappearing(); }
    private record FeedDraft(string Id, string Caption, FeedRecipe Recipe, string Photo,string Audience="friends");
}
