using System.Text.Json;
using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;

public class PremiumLibraryTests
{
    private static List<MealPlanDay> Week() => Enumerable.Range(0,7).Select(n=>new MealPlanDay {Id=100+n,MealPlanID=20,MealDayIndex=n}).ToList();
    [Fact]
    public void WeekTemplateRoundTripsPortionsTypedMealsAndNotesWithoutMutatingCurrentWeek()
    {
        var source=Week();source[0].DinnerRecipesIDs.Add(42);MealPlanning.SetServings(source[0],2,42,3);source[0].DinnerNotes.Add("Make extra sauce");MealPlanning.SetSimpleMeal(source[1],1,"Leftovers");
        var template=SavedWeekTemplate.Create(" Work nights ",source);
        var saved=JsonSerializer.Deserialize<SavedWeekTemplate>(JsonSerializer.Serialize(template));
        source[0].DinnerRecipesIDs.Clear();
        var current=Week();current[0].DinnerRecipesIDs.Add(99);var applied=saved.Apply(current,new long[]{42});
        Assert.Equal("Work nights",saved.Name);Assert.Equal(new long[]{42},applied[0].DinnerRecipesIDs);Assert.Equal(3,MealPlanning.Servings(applied[0],2,new Recipe {Id=42,Serving=1}));
        Assert.Equal("Make extra sauce",Assert.Single(applied[0].DinnerNotes));Assert.Equal("Leftovers",MealPlanning.SimpleMeal(applied[1],1));
        Assert.Equal(new long[]{99},current[0].DinnerRecipesIDs);Assert.Equal(current[0].Id,applied[0].Id);Assert.Equal(current[0].MealPlanID,applied[0].MealPlanID);
    }
    [Fact]
    public void MissingRecipesPreventTemplateReplacement()
    {
        var week=Week();week[0].BreakfastRecipesIDs.Add(42);var template=SavedWeekTemplate.Create("Breakfast",week);
        var current=Week();MealPlanning.SetSimpleMeal(current[0],0,"My original meal");
        Assert.Throws<InvalidOperationException>(()=>template.Apply(current,Array.Empty<long>()));
        Assert.Equal("My original meal",MealPlanning.SimpleMeal(current[0],0));
    }
    [Fact]
    public void ShoppingTripsKeepQuantitiesAndNotesButUseNewUncollectedIds()
    {
        var list=new ShoppingListDocument();list.Add(new(){Name="Milk",Quantity="1 l",Note="(whole)",Category="Dairy",InBasket=true});
        var trip=SavedShoppingTrip.Create("Basics",list);var reused=trip.ReusableItems();
        Assert.False(reused[0].InBasket);Assert.NotEqual(list.Items[0].Id,reused[0].Id);Assert.Equal("(whole)",reused[0].Note);
        reused[0].Name="Changed";Assert.Equal("Milk",trip.ReusableItems()[0].Name);Assert.True(list.Items[0].InBasket);
        var target=new ShoppingListDocument();target.Add(new(){Name="Milk",Quantity="500 ml",Note="(whole)",Category="Dairy"});target.AppendReusableItems(trip.ReusableItems());
        Assert.Equal("1.5 L",Assert.Single(target.Items).Quantity);target.Undo();Assert.Equal("500 ml",Assert.Single(target.Items).Quantity);
    }
    [Fact]
    public async Task SavedLibrariesAreAccountIsolatedAndConcurrentSavesAreRetained()
    {
        var owner="owner-"+Guid.NewGuid();var other="other-"+Guid.NewGuid();var list=new ShoppingListDocument();list.Add(new(){Name="Bread"});
        await Task.WhenAll(Enumerable.Range(0,4).Select(n=>PremiumLibrary.Add(owner,"shopping",SavedShoppingTrip.Create("Trip "+n,list))));
        Assert.Equal(4,(await PremiumLibrary.Load<SavedShoppingTrip>(owner,"shopping")).Count);Assert.Empty(await PremiumLibrary.Load<SavedShoppingTrip>(other,"shopping"));
        Assert.NotEqual(PremiumLibrary.PathFor(owner,"weeks"),PremiumLibrary.PathFor(owner,"shopping"));
        Assert.Throws<InvalidOperationException>(()=>PremiumLibrary.PathFor(null,"shopping"));
    }
    [Fact]
    public async Task CorruptSavedLibrariesAreKeptAndNeverOverwrittenByNewEntries()
    {
        var owner="broken-"+Guid.NewGuid();var path=PremiumLibrary.PathFor(owner,"weeks");Directory.CreateDirectory(Path.GetDirectoryName(path));await File.WriteAllTextAsync(path,"broken json");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PremiumLibrary.Add(owner,"weeks",SavedWeekTemplate.Create("New week",Week())));
        Assert.Equal("broken json",await File.ReadAllTextAsync(path));Assert.True(File.Exists(path+".recovery"));
    }
}
