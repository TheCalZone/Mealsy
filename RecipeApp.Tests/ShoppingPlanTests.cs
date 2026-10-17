using RecipeApp.Models;
using RecipeApp.Services;
using Xunit;

public class ShoppingPlanTests
{
    private static Recipe Recipe() => new() { Id=42, RecipeName="Soup", Serving=4, Ingredients=new() {
        new() { Name="Tomatoes", Quantity="500 g", Tag="Veg", MainIngredient=true },
        new() { Name="Oregano", Quantity="½ tsp", Tag="Pantry", MainIngredient=false, Note="Dried" } } };
    [Theory]
    [InlineData("500 g","0.5 kg","1 kg")]
    [InlineData("750ml","1 litre","1.75 L")]
    [InlineData("1½ tbsp","1/2 tablespoon","2 tbsp")]
    [InlineData("1 1/4 cups","¾ cup","2 cups")]
    [InlineData("2 x 400 g","1 × 400g","3 × 400 g")]
    [InlineData("1 jar","2 jars","3 jars")]
    [InlineData("2","3","5")]
    public void CompatibleQuantitiesAddWithoutInventingWeights(string left,string right,string expected)
    {
        Assert.True(ShoppingQuantity.TryAdd(left,right,out var combined)); Assert.Equal(expected,combined);
    }
    [Theory]
    [InlineData("1 cup","100 g")]
    [InlineData("1 tsp","1 tbsp")]
    [InlineData("2 x 400 g","800 g")]
    [InlineData("1–2 tsp","1 tsp")]
    [InlineData("to taste","to taste")]
    [InlineData("1,000 g","500 g")]
    [InlineData("1/0 tsp","1 tsp")]
    public void AmbiguousOrDifferentUnitsRemainSeparate(string left,string right) => Assert.False(ShoppingQuantity.TryAdd(left,right,out _));
    [Fact]
    public void ServingsAndBatchesScaleCopiesAndLeaveOriginalUntouched()
    {
        var recipe=Recipe(); var choice=new RecipeShoppingChoice(recipe) {Selected=true,Servings="2",Batches="3"};
        var rows=ShoppingPlan.Build(new[] {choice},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.Equal("750 g",rows.Single(r => r.Item.Name=="Tomatoes").Item.Quantity);
        var herb=rows.Single(r => r.Item.Name=="Oregano"); Assert.Equal("0.75 tsp",herb.Item.Quantity); Assert.True(herb.CheckCupboard); Assert.False(herb.Included);
        Assert.Equal("500 g",recipe.Ingredients[0].Quantity); Assert.Equal("½ tsp",recipe.Ingredients[1].Quantity);
    }
    [Fact]
    public void SavedEssentialsBecomeCupboardChecksEvenForRequiredIngredients()
    {
        var recipe=Recipe(); var rows=ShoppingPlan.Build(new[] {new RecipeShoppingChoice(recipe) {Selected=true}},Array.Empty<Snack>(),Array.Empty<Staple>(),new[] {" tomatoes "});
        Assert.False(rows.Single(r => r.Item.Name=="Tomatoes").Included); Assert.Equal(2,rows.Count);
    }
    [Fact]
    public void SelectedEssentialUsesUsualPurchaseOnceAndShowsRecipeRequirement()
    {
        var rows=ShoppingPlan.Build(new[] {new RecipeShoppingChoice(Recipe()) {Selected=true}},Array.Empty<Snack>(),new[] {new Staple {Name="Oregano",Quantity="1 jar",Tag="Pantry"}},new[] {"Oregano"});
        var herb=Assert.Single(rows,r => r.Item.Name=="Oregano"); Assert.True(herb.Included); Assert.Equal("1 jar",herb.Item.Quantity); Assert.Contains("Soup: 0.5 tsp",herb.Sources);
    }
    [Fact]
    public void EssentialWithoutPurchaseAmountKeepsRecipeAmount()
    {
        var rows=ShoppingPlan.Build(new[] {new RecipeShoppingChoice(Recipe()) {Selected=true}},Array.Empty<Snack>(),new[] {new Staple {Name="Oregano",Tag="Pantry"}},new[] {"Oregano"});
        var herb=Assert.Single(rows,r => r.Item.Name=="Oregano"); Assert.True(herb.Included); Assert.Equal("0.5 tsp",herb.Item.Quantity);
    }
    [Fact]
    public void ExtrasRetainAmountsAndNotesAndUnknownAmountsAreFlagged()
    {
        var rows=ShoppingPlan.Build(Array.Empty<RecipeShoppingChoice>(),new[] {new Snack {Name="Popcorn",Quantity="2 bags",Tag="Pantry",Note="Salted"}},new[] {new Staple {Name="Soap",Quantity="A refill",Tag="Other"}},Array.Empty<string>());
        Assert.Equal("Salted",rows.Single(r => r.Item.Name=="Popcorn").Item.Note); Assert.False(rows.Single(r => r.Item.Name=="Popcorn").AmountNeedsReview);
        Assert.True(rows.Single(r => r.Item.Name=="Soap").AmountNeedsReview); Assert.All(rows,r => Assert.True(r.Included));
    }
    [Fact]
    public void MealOccurrencesCountEachSlotButNotDuplicateLegacyId()
    {
        var counts=ShoppingPlan.MealOccurrences(new[] {new MealPlanDay {BreakfastRecipeId=42,BreakfastRecipesIDs=new(){42},LunchRecipesIDs=new(){42}},new MealPlanDay {DinnerRecipeId=42}});
        Assert.Equal(3,counts[42]);
        var choice=new RecipeShoppingChoice(Recipe()) {Selected=true,Batches=counts[42].ToString()};
        var rows=ShoppingPlan.Build(new[]{choice},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.Equal("1.5 kg",rows.Single(r => r.Item.Name=="Tomatoes").Item.Quantity);
    }
    [Theory]
    [InlineData("0","1")][InlineData("two","1")][InlineData("4","0")][InlineData("4","101")]
    public void InvalidScalingCannotDropIngredients(string servings,string batches) => Assert.Throws<InvalidOperationException>(() => new RecipeShoppingChoice(Recipe()) {Servings=servings,Batches=batches}.Factor());
    [Fact]
    public void ConsolidationPreservesDifferentNotesAndDoesNotGuessMissingCounts()
    {
        var rows=ShoppingListDocument.FromIngredients(new[] {new Ingredient {Name="Milk",Quantity="500 ml",Note="Oat"},new Ingredient {Name="Milk",Quantity="500 ml",Note="Whole"},new Ingredient {Name="Salt"},new Ingredient {Name="Salt"}});
        Assert.Equal(3,rows.Count); Assert.Equal("",rows.Single(r => r.Name=="Salt").Quantity);
    }
    [Fact]
    public void AppendingMergesUncollectedAmountsAndCanBeUndoneWithoutChangingCollectedPurchases()
    {
        var list=new ShoppingListDocument(); list.Add(new() {Name="Rice",Category="Pantry",Quantity="250 g"}); list.Add(new() {Name="Rice",Category="Pantry",Quantity="1 kg",InBasket=true});
        var id=list.Items[0].Id; list.AppendIngredients(new[] {new Ingredient {Name="rice",Tag="Pantry",Quantity="750 g"}});
        Assert.Equal(2,list.Items.Count); Assert.Equal(id,list.Items[0].Id); Assert.Equal("1 kg",list.Items[0].Quantity); Assert.True(list.Items[1].InBasket);
        list.Undo(); Assert.Equal("250 g",list.Items[0].Quantity); Assert.True(list.Items[1].InBasket);
    }
    [Fact]
    public void TinyPositiveQuantitiesNeverRoundToZero() => Assert.Equal("0.0001 g",ShoppingQuantity.Normalize("0.0001 g"));
    [Fact]
    public async Task LibrarySyncUsesCloudIdentityAndPreservesMetadataAcrossDevicesAndAccounts()
    {
        var owner="library-"+Guid.NewGuid(); var other="library-"+Guid.NewGuid();
        await RecipeService.InsertSnack(new() {Name="Other account",FirebaseUID=other,CloudKey="same",Quantity="9 bags"});
        await RecipeService.MergeShoppingLibrary(owner,new[] {new Snack {Id=1,Name="Popcorn",CloudKey="same",Quantity="2 bags",Note="Salted",LastUpdated=DateTimeOffset.UtcNow}},new[] {new Staple {Id=1,Name="Herbs",CloudKey="herbs",Quantity="1 jar",Note="Check cupboard",LastUpdated=DateTimeOffset.UtcNow}});
        var local=Assert.Single(RecipeService.Snacks,s => s.FirebaseUID==owner); var id=local.Id;
        await RecipeService.MergeShoppingLibrary(owner,new[] {new Snack {Id=999,Name="Renamed popcorn",CloudKey="same",Quantity="3 bags",Note="Plain",LastUpdated=DateTimeOffset.UtcNow}},Array.Empty<Staple>());
        local=Assert.Single(RecipeService.Snacks,s => s.FirebaseUID==owner); Assert.Equal(id,local.Id); Assert.Equal("Renamed popcorn",local.Name); Assert.Equal("3 bags",local.Quantity); Assert.Equal("Plain",local.Note);
        Assert.Equal("9 bags",Assert.Single(RecipeService.Snacks,s => s.FirebaseUID==other).Quantity); Assert.Equal("Check cupboard",Assert.Single(RecipeService.Staples,s => s.FirebaseUID==owner).Note);
    }
    [Fact]
    public void TextSharingCanBePastedWithAmountsAndNotesIntact()
    {
        var original=new ShoppingListDocument(); original.Add(new() {Name="Rice",Quantity="500 g",Category="Pantry",Note="Brown (long grain)"});
        var parsed=Assert.Single(ShoppingListDocument.ParseText(original.PlainText()));
        Assert.Equal("Rice",parsed.Name); Assert.Equal("500 g",parsed.Quantity); Assert.Equal("Brown (long grain)",parsed.Note);
        var target=new ShoppingListDocument(); target.AppendText(original.PlainText()); target.AppendText("Pantry:\nRice — 250 g (Brown (long grain))");
        Assert.Equal("750 g",Assert.Single(target.Items).Quantity);
    }
    [Fact]
    public void IncompatibleAmountsShowTheirOwnRecipeSourcesAndRetainIndependentChoices()
    {
        var a=new Recipe {RecipeName="Soup",Serving=1,Ingredients=new(){new() {Name="Basil",Quantity="10 g",MainIngredient=true}}};
        var b=new Recipe {RecipeName="Salad",Serving=1,Ingredients=new(){new() {Name="Basil",Quantity="1 bunch",MainIngredient=true}}};
        var rows=ShoppingPlan.Build(new[] {new RecipeShoppingChoice(a) {Selected=true},new RecipeShoppingChoice(b) {Selected=true}},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.Equal("Soup",rows.Single(r => r.Item.Quantity=="10 g").Sources); Assert.Equal("Salad",rows.Single(r => r.Item.Quantity=="1 bunch").Sources);
        a.Ingredients[0].Quantity="to taste"; b.Ingredients[0].Quantity="to taste";
        rows=ShoppingPlan.Build(new[] {new RecipeShoppingChoice(a) {Selected=true},new RecipeShoppingChoice(b) {Selected=true}},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.NotEqual(rows[0].Key,rows[1].Key);
        var key=rows[0].Key; rows[0].Item.Quantity="1 jar"; Assert.Equal(key,rows[0].Key);
    }
    [Fact]
    public async Task LibraryDeletionMarkersRemoveOldDeviceCopiesWithoutTouchingOtherAccounts()
    {
        var owner="deleted-"+Guid.NewGuid(); var other="other-"+Guid.NewGuid();
        await RecipeService.InsertStaple(new() {Name="Herbs",CloudKey="herbs",FirebaseUID=owner});
        await RecipeService.InsertStaple(new() {Name="Herbs",CloudKey="herbs",FirebaseUID=other});
        await RecipeService.MergeShoppingLibrary(owner,Array.Empty<Snack>(),new[] {new Staple {CloudKey="herbs",IsDeleted=true}});
        Assert.DoesNotContain(RecipeService.Staples,s => s.FirebaseUID==owner); Assert.Contains(RecipeService.Staples,s => s.FirebaseUID==other);
        await RecipeService.MergeShoppingLibrary(owner,Array.Empty<Snack>(),new[] {new Staple {CloudKey="herbs",IsDeleted=true}});
        Assert.DoesNotContain(RecipeService.Staples,s => s.FirebaseUID==owner);
    }
    [Fact]
    public void OldLibraryTablesMigrateWithoutLosingChecklistItems()
    {
        var path=Path.Combine(Path.GetTempPath(),"mealsy-library-migration-"+Guid.NewGuid()+".db");
        using (var db=new SQLite.SQLiteConnection(path))
        {
            db.Execute("CREATE TABLE Snack (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Tag TEXT)");
            db.Execute("CREATE TABLE Staple (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Tag TEXT)");
            db.Execute("INSERT INTO Snack (Name,Tag) VALUES ('Popcorn','Pantry')"); db.Execute("INSERT INTO Staple (Name,Tag) VALUES ('Oregano','Pantry')");
            db.CreateTable<Snack>(); db.CreateTable<Staple>();
            var snack=Assert.Single(db.Table<Snack>()); var staple=Assert.Single(db.Table<Staple>());
            Assert.Equal("Popcorn",snack.Name); Assert.Equal("Oregano",staple.Name);
            staple.Quantity="1 jar"; staple.Note="Check cupboard"; staple.CloudKey="stable"; db.Update(staple);
            Assert.Equal("Check cupboard",db.Get<Staple>(staple.Id).Note); Assert.Equal("stable",db.Get<Staple>(staple.Id).CloudKey);
        }
        File.Delete(path);
    }

    [Fact]
    public void CommonHerbsAndOilsAreSuggestedForCheckingWithoutChangingRecipeFlags()
    {
        var recipe=Recipe(); recipe.Ingredients=new() {new() {Name="Dried oregano",Quantity="1 tsp",MainIngredient=true},new() {Name="Salted butter",Quantity="50 g",MainIngredient=true}};
        var rows=ShoppingPlan.Build(new[] {new RecipeShoppingChoice(recipe) {Selected=true}},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.False(rows.Single(r => r.Item.Name=="Dried oregano").Included); Assert.True(rows.Single(r => r.Item.Name=="Salted butter").Included);
        Assert.True(recipe.Ingredients[0].MainIngredient);
    }
    [Fact]
    public async Task OlderCloudLibraryCopiesCannotOverwriteNewerLocalEdits()
    {
        var owner="newer-"+Guid.NewGuid(); var now=DateTimeOffset.UtcNow;
        await RecipeService.InsertSnack(new() {Name="New name",FirebaseUID=owner,CloudKey="stable",Quantity="4 bags",LastUpdated=now});
        await RecipeService.MergeShoppingLibrary(owner,new[] {new Snack {Name="Old name",CloudKey="stable",Quantity="1 bag",LastUpdated=now.AddDays(-1)}},Array.Empty<Staple>());
        var item=Assert.Single(RecipeService.Snacks,s => s.FirebaseUID==owner); Assert.Equal("New name",item.Name); Assert.Equal("4 bags",item.Quantity);
    }

    [Fact]
    public void FractionalTotalsAreRoundedAfterAggregationRatherThanPerIngredient()
    {
        var ingredients=Enumerable.Range(0,6).Select(_ => new Ingredient {Name="Flour",Quantity="1/3 cup"});
        Assert.Equal("2 cups",Assert.Single(ShoppingListDocument.FromIngredients(ingredients)).Quantity);
        var recipe=new Recipe {RecipeName="Bread",Serving=3,Ingredients=new() {new() {Name="Flour",Quantity="1 cup",MainIngredient=true}}};
        var rows=ShoppingPlan.Build(Enumerable.Range(0,3).Select(_ => new RecipeShoppingChoice(recipe) {Selected=true,Servings="1"}),Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.Equal("1 cup",Assert.Single(rows).Item.Quantity);
    }

}
