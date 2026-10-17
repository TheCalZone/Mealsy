using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
using Xunit;

public class WidgetAndPlannerTests
{
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
