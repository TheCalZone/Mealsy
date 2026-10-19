using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
public class RecipeReadView : ContentView
{
    private readonly Switch cooking=new();private bool previousKeepAwake,holding;
    public int CurrentServings{get;private set;}
    public RecipeReadView(Recipe recipe,bool privateDetails=true)
    {
        CurrentServings=(int)Math.Clamp(recipe.Serving,1,1000);var original=RecipeDraft.Copy(recipe);var body=new VerticalStackLayout{Padding=16,Spacing=16};var scroll=new ScrollView{Content=body};
        var photo=RecipeCardView.Photo(recipe.RecipeImage,240);if(photo!=null)body.Add(photo);body.Add(ShoppingUi.Text(recipe.RecipeName,30,true));if(!string.IsNullOrWhiteSpace(recipe.Description))body.Add(ShoppingUi.Text(recipe.Description,16));
        var time=ShoppingUi.Text(RecipePresentation.TimeText(recipe),14,true);body.Add(ShoppingUi.Card(time));var tags=RecipeBrowser.Tags(recipe);if(tags.Length>0)body.Add(ShoppingUi.Text(string.Join(" · ",tags),12));if(privateDetails&&recipe.Rating>0)body.Add(ShoppingUi.Text($"Your rating · {recipe.Rating}/5",14,true));
        var portionLabels=new List<(int Index,Label Label)>();var portions=ShoppingUi.Entry(CurrentServings.ToString(),"Servings",_=>{},4,true);var portionCard=new VerticalStackLayout{Spacing=7};portionCard.Add(ShoppingUi.Text("Cook for your table",20,true));portionCard.Add(ShoppingUi.Text("Preview quantities for different servings. Your saved recipe stays unchanged.",13));portionCard.Add(portions);
        portionCard.Add(ShoppingUi.Button("Adjust servings",()=>{if(!int.TryParse(portions.Text,out var wanted))throw new InvalidOperationException("Enter a whole number of servings.");var scaled=RecipeBrowser.ForServings(original,wanted);CurrentServings=wanted;time.Text=RecipePresentation.TimeText(scaled);foreach(var entry in portionLabels)entry.Label.Text=$"{scaled.Ingredients[entry.Index].Quantity} {scaled.Ingredients[entry.Index].Name}".Trim();return Task.CompletedTask;},true));body.Add(ShoppingUi.Card(portionCard));
        cooking.Toggled+=(_,e)=>{if(e.Value){previousKeepAwake=DeviceDisplay.Current.KeepScreenOn;holding=true;DeviceDisplay.Current.KeepScreenOn=true;}else Release();};body.Add(new HorizontalStackLayout{Spacing=8,Children={cooking,ShoppingUi.Text("Keep screen awake",14)}});
        var ingredientAnchor=ShoppingUi.Text("Ingredients",24,true);var methodAnchor=ShoppingUi.Text("Method",24,true);var jump=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Star)},ColumnSpacing=8};jump.Add(ShoppingUi.Button("Ingredients",()=>scroll.ScrollToAsync(ingredientAnchor,ScrollToPosition.Start,true),true),0);jump.Add(ShoppingUi.Button("Method",()=>scroll.ScrollToAsync(methodAnchor,ScrollToPosition.Start,true),true),1);body.Add(jump);
        body.Add(ingredientAnchor);body.Add(ShoppingUi.Text("Tick as you prepare. Cooking checks reset when you close the recipe and don't change your shopping list.",12));
        foreach(var section in recipe.Ingredients.GroupBy(i=>string.IsNullOrWhiteSpace(i.IngredientSection)?"Ingredients":i.IngredientSection))
        {
            var group=new VerticalStackLayout{Spacing=9};group.Add(ShoppingUi.Text(section.Key,18,true));
            foreach(var item in section)
            {
                var label=ShoppingUi.Text($"{item.Quantity} {item.Name}".Trim());portionLabels.Add((recipe.Ingredients.IndexOf(item),label));
                var row=new Grid{ColumnDefinitions=new(){new(new GridLength(44)),new(GridLength.Star)}};row.Add(ShoppingUi.Check(false,v=>{label.TextDecorations=v?TextDecorations.Strikethrough:TextDecorations.None;label.Opacity=v?.55:1;}),0);
                var info=new VerticalStackLayout{Spacing=3,Children={label}};if(!string.IsNullOrWhiteSpace(item.Note))info.Add(ShoppingUi.Text(item.Note,12));row.Add(info,1);group.Add(row);
            }body.Add(ShoppingUi.Card(group));
        }
        body.Add(methodAnchor);body.Add(ShoppingUi.Text("Suggested ingredients are matched from step text. Tick each addition separately. Amounts are recipe totals; follow the step for divided ingredients. Checks and adjustments reset when you close the recipe.",12));var steps=RecipeDraft.ParseSteps(recipe.RecipeMethod);var done=new HashSet<int>();var progress=ShoppingUi.Text($"0 of {steps.Count} steps complete",13,true);var bar=new ProgressBar{};bar.SetDynamicResource(ProgressBar.ProgressColorProperty,"ThemeAccent");body.Add(progress);body.Add(bar);
        for(var i=0;i<steps.Count;i++)
        {
            var index=i;var card=new VerticalStackLayout{Spacing=9};card.Add(ShoppingUi.Text($"STEP {i+1}",12,true));card.Add(ShoppingUi.Text(steps[i],17));
            var selected=StepIngredients.Match(steps[i],recipe.Ingredients);var checks=new Dictionary<int,bool>();var stepLabels=new List<(int Index,Label Label)>();
            var additions=new VerticalStackLayout{Spacing=7};
            void RenderAdditions(){
                foreach(var old in stepLabels)portionLabels.Remove(old);stepLabels.Clear();additions.Clear();
                additions.Add(ShoppingUi.Text("Ingredients for this step · recipe totals",13,true));
                if(selected.Count==0)additions.Add(ShoppingUi.Text("No names matched automatically. Choose ingredients below or refer to the full ingredient list.",12));
                var scaledIngredients=RecipeBrowser.ForServings(original,CurrentServings).Ingredients;
                foreach(var ingredientIndex in selected){
                    var ingredient=scaledIngredients[ingredientIndex];
                    var label=ShoppingUi.Text($"{ingredient.Quantity} {ingredient.Name}".Trim(),14);var pair=(ingredientIndex,label);portionLabels.Add(pair);stepLabels.Add(pair);
                    var check=ShoppingUi.Check(checks.GetValueOrDefault(ingredientIndex),value=>checks[ingredientIndex]=value);SemanticProperties.SetDescription(check,$"Added {ingredient.Name} in step {index+1}");
                    var row=new Grid{ColumnDefinitions=new(){new(new GridLength(44)),new(GridLength.Star)}};row.Add(check,0);
                    var info=new VerticalStackLayout{Spacing=2,Children={label}};if(!string.IsNullOrWhiteSpace(ingredient.IngredientSection)&&ingredient.IngredientSection!="Ingredients")info.Add(ShoppingUi.Text(ingredient.IngredientSection,12));row.Add(info,1);additions.Add(row);
                }
            }
            RenderAdditions();card.Add(additions);
            card.Add(ShoppingUi.Button("Choose ingredients for this step…",async()=>{
                var action=await Shell.Current.DisplayActionSheet("Step "+(index+1)+" ingredients","Cancel",null,"Add an ingredient","Remove an ingredient");if(action is not ("Add an ingredient" or "Remove an ingredient"))return;
                var candidates=Enumerable.Range(0,recipe.Ingredients.Count).Where(n=>action=="Add an ingredient"?!selected.Contains(n):selected.Contains(n)).ToList();
                if(candidates.Count==0){await Shell.Current.DisplayAlert("Step ingredients",action=="Add an ingredient"?"All recipe ingredients are already shown for this step.":"No ingredients to remove.","OK");return;}
                var labels=candidates.Select(n=>$"{n+1}. {recipe.Ingredients[n].Name} · {recipe.Ingredients[n].IngredientSection}").ToArray();
                var choice=await Shell.Current.DisplayActionSheet(action,"Cancel",null,labels);var position=Array.IndexOf(labels,choice);if(position<0)return;var chosen=candidates[position];
                if(action=="Add an ingredient")selected.Add(chosen);else{selected.Remove(chosen);checks.Remove(chosen);}RenderAdditions();
            },true));
            card.Add(new HorizontalStackLayout{Children={ShoppingUi.Check(false,v=>{if(v)done.Add(index);else done.Remove(index);progress.Text=$"{done.Count} of {steps.Count} steps complete";bar.Progress=(double)done.Count/Math.Max(1,steps.Count);card.Opacity=v?.6:1;}),ShoppingUi.Text("Done",13)}});body.Add(ShoppingUi.Card(card));
        }
        if(privateDetails&&!string.IsNullOrWhiteSpace(recipe.Notes))body.Add(ShoppingUi.Card(new VerticalStackLayout{Spacing=8,Children={ShoppingUi.Text("Your private notes",20,true),ShoppingUi.Text(recipe.Notes)}}));
        if(privateDetails&&Uri.TryCreate(recipe.SourceUrl,UriKind.Absolute,out var url)&&url.Scheme is "http" or "https"&&string.IsNullOrEmpty(url.UserInfo))body.Add(ShoppingUi.Button("Open original source",async()=>await Launcher.Default.OpenAsync(url),true));Content=scroll;
    }
    private void Release(){if(holding){DeviceDisplay.Current.KeepScreenOn=previousKeepAwake;holding=false;}}
    public void StartCooking(){cooking.IsToggled=AppSettings.Current.KeepAwake;}
    public void StopCooking(){Release();cooking.IsToggled=false;}
}
public class RecipePreviewPage : ContentPage
{
    private readonly RecipeReadView read;
    public RecipePreviewPage(RecipeDraft draft){Title="Preview · not saved";ShoppingUi.Theme(this);var recipe=RecipePresentation.Preview(draft);Content=read=new(recipe);ToolbarItems.Add(new ToolbarItem("Copy",null,async()=>{try{await Clipboard.Default.SetTextAsync(RecipePresentation.PlainText(RecipeBrowser.ForServings(recipe,read.CurrentServings)));}catch(Exception){await DisplayAlert("Copy unavailable","Try copying again.","OK");}}));}
    protected override void OnAppearing(){base.OnAppearing();read.StartCooking();}
    protected override void OnDisappearing(){read.StopCooking();base.OnDisappearing();}
}
