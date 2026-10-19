using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
using Xunit;

public class WidgetAndPlannerTests
{
    [Fact]
    public void DashboardMealNamesKeepTypedMealsAlongsideRecipesWithoutAddingNotesAsRecipes()
    {
        var day=new MealPlanDay();MealPlanning.SetSimpleMeal(day,2,"Leftovers");
        Assert.Equal(new[]{"Leftovers"},MealPlanning.MealNames(day,2,Array.Empty<Recipe>()));
        MealPlanning.Ids(day,2).AddRange(new long[]{4,99});MealPlanning.Notes(day,2).Add("Home late");
        Assert.Equal(new[]{"Pasta","Recipe unavailable","Leftovers"},MealPlanning.MealNames(day,2,new[]{new Recipe{Id=4,RecipeName="Pasta"}}));
        Assert.Empty(MealPlanning.MealNames(day,0,Array.Empty<Recipe>()));
        Assert.Equal("Home late",Assert.Single(MealPlanning.Notes(day,2)));
    }
    [Fact]
    public void BatchOfEightForTwoCreatesFourMealsAndShopsEightPortions()
    {
        var recipe=new Recipe{Id=40,Serving=8,RecipeName="Chilli"};
        var days=Enumerable.Range(0,7).Select(i=>new MealPlanDay{MealDayIndex=i}).ToList();
        var batch=MealPlanning.PlanBatch(days,recipe,2,0,2);
        Assert.Equal(4,batch.Count(d=>MealPlanning.Ids(d,2).Contains(recipe.Id)));
        Assert.All(batch.Take(4),d=>Assert.Equal(2,MealPlanning.Servings(d,2,recipe)));
        Assert.Equal("8",Assert.Single(MealPlanning.ShoppingChoices(batch,new[]{recipe})).Servings);
        Assert.All(days,d=>Assert.Empty(MealPlanning.Ids(d,2)));
    }
    [Fact]
    public void BatchNeverOverwritesAnExistingMealAndRejectsWeekOverflow()
    {
        var recipe=new Recipe{Id=40,Serving=7};var days=Enumerable.Range(0,7).Select(i=>new MealPlanDay{MealDayIndex=i}).ToList();
        MealPlanning.SetSimpleMeal(days[1],2,"Eggs");
        Assert.Throws<InvalidOperationException>(()=>MealPlanning.PlanBatch(days,recipe,2,0,2));Assert.Equal("Eggs",MealPlanning.SimpleMeal(days[1],2));
        Assert.Throws<InvalidOperationException>(()=>MealPlanning.PlanBatch(days,recipe,2,5,2));
        var batch=MealPlanning.PlanBatch(days,recipe,2,0,1);Assert.Equal(1,MealPlanning.Servings(batch[3],1,recipe));Assert.Equal("7",Assert.Single(MealPlanning.ShoppingChoices(batch,new[]{recipe})).Servings);
    }
    [Fact]
    public void OpeningWidgetListUsesChosenTargetAndRejectsAnotherAccount()
    {
        Assert.Equal("//ShoppingListPage?OpenPersonal=True",new WidgetListTarget(null,null,"Device").OpenRoute(null));
        Assert.Equal("//ShoppingListPage?OpenHouseholdId=family-a",new WidgetListTarget("alice","family-a","Family").OpenRoute("alice"));
        Assert.Throws<InvalidOperationException>(()=>new WidgetListTarget("alice","family-a","Family").OpenRoute("bob"));
        Assert.Throws<InvalidOperationException>(()=>new WidgetListTarget("alice",null,"Personal").OpenRoute(null));
    }
    [Fact]
    public async Task TypedMealsPersistWithoutRecipesAndSurviveDayCopyAndClear()
    {
        var owner="simple-meals-"+Guid.NewGuid();await RecipeService.CreateMealPlan(owner);var plan=await RecipeService.GetMealPlan(owner);
        var day=MealPlanning.Copy(plan.Days[0]);var before=MealPlanning.State(day);
        MealPlanning.SetSimpleMeal(day,0," Eggs ");MealPlanning.SetSimpleMeal(day,1,"Toast");MealPlanning.SetSimpleMeal(day,2,"Eating out");
        Assert.NotEqual(before,MealPlanning.State(day));MealPlanning.Serialize(day);await RecipeService.SaveMealPlanDay(day,plan.Id,0);
        var saved=(await RecipeService.GetMealPlan(owner)).Days[0];Assert.Equal("Eggs",MealPlanning.SimpleMeal(saved,0));Assert.Equal("Toast",MealPlanning.SimpleMeal(saved,1));Assert.Equal("Eating out",MealPlanning.SimpleMeal(saved,2));
        Assert.Empty(MealPlanning.ShoppingChoices(new[]{saved},Array.Empty<Recipe>()));
        var target=MealPlanning.Copy(saved,MealPlanning.Copy(plan.Days[1]));await RecipeService.SaveMealPlanDays(new[]{target},plan.Id);
        Assert.Equal("Eggs",MealPlanning.SimpleMeal((await RecipeService.GetMealPlan(owner)).Days[1],0));
        MealPlanning.Clear(target);Assert.Equal("",MealPlanning.SimpleMeal(target,0));Assert.Equal("Eggs",MealPlanning.SimpleMeal(saved,0));
    }
    [Fact]
    public void WidgetTargetsAreIndependentAndAccountBound()
    {
        WidgetShoppingStore.SetTarget(101,new("alice","family-a","Household"));WidgetShoppingStore.SetTarget(102,new("bob",null,"Personal"));
        Assert.True(WidgetShoppingStore.Target(101).BelongsTo("alice"));Assert.False(WidgetShoppingStore.Target(101).BelongsTo("bob"));
        Assert.Equal("ShoppingList.household.alice.family-a",WidgetShoppingStore.Target(101).StorageKey);
        WidgetShoppingStore.Delete(101);Assert.Null(WidgetShoppingStore.Target(101));Assert.Equal("bob",WidgetShoppingStore.Target(102).Account);
    }
    [Fact]
    public void WidgetRetryCannotDuplicateAnItemOrChangeCollectedPurchases()
    {
        Preferences.Clear();ShoppingListStore.AccountId=()=>null;var list=new ShoppingListDocument();list.Add(new(){Name="Milk",Quantity="2 L",InBasket=true});ShoppingListStore.Save(list);
        var item=new ShoppingListItem {Name="Bread",Quantity="1 loaf",Note="Wholemeal"};var target=new WidgetListTarget(null,null,"Device");
        WidgetShoppingStore.Add(target,null,item);WidgetShoppingStore.Add(target,null,item);
        var restored=ShoppingListDocument.Deserialize(Preferences.Get(target.StorageKey,""));Assert.Equal(2,restored.Items.Count);Assert.True(restored.Items[0].InBasket);Assert.Equal("Wholemeal",restored.Items[1].Note);
    }
    [Fact]
    public void WidgetCannotWriteAnotherAccountsTargetOrOverwriteUnreadableStorage()
    {
        ShoppingListStore.AccountId=()=>null;var other=new WidgetListTarget("other",null,"Other");Assert.Throws<InvalidOperationException>(()=>WidgetShoppingStore.Add(other,null,new(){Name="Bad"}));
        var target=new WidgetListTarget(null,null,"Device");Preferences.Set(target.StorageKey,"broken");Assert.Throws<JsonException>(()=>WidgetShoppingStore.Add(target,null,new(){Name="Kept"}));Assert.Equal("broken",Preferences.Get(target.StorageKey,""));Preferences.Clear();
    }
    [Fact]
    public void SharedWidgetWritesOnlyItsSelectedCacheAndKeepsStableIds()
    {
        Preferences.Clear();var target=new WidgetListTarget("alice","house-a","Household");var item=new ShoppingListItem {Name="Apples"};WidgetShoppingStore.Add(target,"alice",item);WidgetShoppingStore.Add(target,"alice",item);
        Assert.Equal(item.Id,Assert.Single(ShoppingListDocument.Deserialize(Preferences.Get(target.StorageKey,"")).Items).Id);Assert.Equal("",Preferences.Get("ShoppingList.v2",""));Preferences.Clear();
    }
    [Fact]
    public void InFlightShoppingSyncRetainsWidgetAdditionsFromTheLatestCache()
    {
        var sent=new ShoppingListDocument();sent.Add(new(){Name="Milk"});var before=sent.Serialize();sent.Add(new(){Name="Widget bread"});
        var merged=ShoppingListSync.Reapply(before,sent.Serialize(),before);Assert.Equal(2,merged.Items.Count);
    }
    [Fact]
    public async Task PersonalMealPlansAreIsolatedAndGuestPlanIsPreserved()
    {
        var alice="planner-"+Guid.NewGuid();var bob="planner-"+Guid.NewGuid();await RecipeService.CreateMealPlan();await RecipeService.CreateMealPlan(alice);await RecipeService.CreateMealPlan(bob);
        var first=await RecipeService.GetMealPlan(alice);var other=await RecipeService.GetMealPlan(bob);Assert.NotEqual(first.Id,other.Id);Assert.NotEqual(first.Id,(await RecipeService.GetMealPlan()).Id);
        var day=MealPlanning.Copy(first.Days[0]);day.DinnerNotes.Add("Private dinner");MealPlanning.Serialize(day);await RecipeService.SaveMealPlanDay(day,first.Id,day.MealDayIndex);
        Assert.Equal("Private dinner",Assert.Single((await RecipeService.GetMealPlan(alice)).Days[0].DinnerNotes));Assert.Empty((await RecipeService.GetMealPlan(bob)).Days[0].DinnerNotes);
    }
    [Fact]
    public async Task ClearedRecipesAndNotesNeverReappearAfterSaving()
    {
        var owner="clear-"+Guid.NewGuid();await RecipeService.CreateMealPlan(owner);var plan=await RecipeService.GetMealPlan(owner);var day=plan.Days[0];
        day.BreakfastRecipesIDs.Add(123);day.BreakfastRecipeId=123;day.BreakfastNotes.Add("Old note");MealPlanning.Serialize(day);await RecipeService.SaveMealPlanDay(day,plan.Id,0);
        MealPlanning.Clear(day);await RecipeService.SaveMealPlanDay(day,plan.Id,0);var restored=(await RecipeService.GetMealPlan(owner)).Days[0];Assert.Empty(restored.BreakfastRecipesIDs);Assert.Empty(restored.BreakfastNotes);Assert.Equal(0,restored.BreakfastRecipeId);
    }
    [Fact]
    public void DayCopiesAreDetachedAndKeepTargetIdentityAndPortions()
    {
        var source=new MealPlanDay {Id=10,MealDayIndex=0};source.DinnerRecipesIDs.Add(42);source.DinnerNotes.Add("Leftovers");MealPlanning.SetServings(source,2,42,3);
        var target=new MealPlanDay {Id=20,MealDayIndex=4};MealPlanning.Copy(source,target);target.DinnerNotes[0]="Changed";
        Assert.Equal(20,target.Id);Assert.Equal(4,target.MealDayIndex);Assert.Equal("Leftovers",source.DinnerNotes[0]);Assert.Equal(3,MealPlanning.Portions(target,2)[42]);
    }
    [Fact]
    public void ShoppingUsesPlannedPortionsAcrossSlotsAndDays()
    {
        var recipe=new Recipe {Id=42,Serving=4,Ingredients=new(){new(){Name="Rice",Quantity="400 g",MainIngredient=true}}};var first=new MealPlanDay();first.LunchRecipesIDs.Add(42);first.DinnerRecipesIDs.Add(42);MealPlanning.SetServings(first,1,42,2);MealPlanning.SetServings(first,2,42,3);
        var next=new MealPlanDay();next.DinnerRecipesIDs.Add(42);var choice=Assert.Single(MealPlanning.ShoppingChoices(new[]{first,next},new[]{recipe}));Assert.Equal("9",choice.Servings);
        var rows=ShoppingPlan.Build(new[]{choice},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());Assert.Equal("900 g",Assert.Single(rows).Item.Quantity);
    }
    [Fact]
    public void LegacySingleRecipeAndRepeatedIdsNormalizeOnce()
    {
        var day=new MealPlanDay {BreakfastRecipeId=42};day.BreakfastRecipesIDs.AddRange(new long[]{42,42,0});MealPlanning.NormalizeLegacy(day,Array.Empty<Recipe>());Assert.Equal(42,Assert.Single(day.BreakfastRecipesIDs));MealPlanning.Serialize(day);Assert.Equal(0,day.BreakfastRecipeId);
    }
    [Fact]
    public void MissingRecipesAndInvalidPortionsCannotProduceMisleadingShoppingTotals()
    {
        var day=new MealPlanDay();day.DinnerRecipesIDs.Add(42);Assert.Throws<InvalidOperationException>(()=>MealPlanning.ShoppingChoices(new[]{day},Array.Empty<Recipe>()));Assert.Throws<InvalidOperationException>(()=>MealPlanning.SetServings(day,2,42,0));
    }
    [Fact]
    public async Task FailedWeekReplacementRollsBackEarlierDays()
    {
        var owner="atomic-"+Guid.NewGuid();await RecipeService.CreateMealPlan(owner);var plan=await RecipeService.GetMealPlan(owner);var first=MealPlanning.Copy(plan.Days[0]);first.DinnerNotes.Add("Keep me");MealPlanning.Serialize(first);await RecipeService.SaveMealPlanDay(first,plan.Id,0);
        var changed=MealPlanning.Copy(first);changed.DinnerNotes.Clear();var invalid=new MealPlanDay {MealDayIndex=999};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>RecipeService.SaveMealPlanDays(new[]{changed,invalid},plan.Id));
        Assert.Equal("Keep me",Assert.Single((await RecipeService.GetMealPlan(owner)).Days[0].DinnerNotes));
    }
    [Fact]
    public async Task WeekDateAndMealPortionsPersistAcrossReload()
    {
        var owner="portions-"+Guid.NewGuid();await RecipeService.CreateMealPlan(owner);var plan=await RecipeService.GetMealPlan(owner);plan.WeekStart=new DateTime(2026,11,2);await RecipeService.SaveMealPlanHeader(plan);
        var day=MealPlanning.Copy(plan.Days[0]);day.LunchRecipesIDs.Add(42);MealPlanning.SetServings(day,1,42,7);MealPlanning.Serialize(day);await RecipeService.SaveMealPlanDay(day,plan.Id,0);
        var reloaded=await RecipeService.GetMealPlan(owner);Assert.Equal(plan.WeekStart,reloaded.WeekStart);Assert.Equal(7,MealPlanning.Portions(reloaded.Days[0],1)[42]);
    }

}
