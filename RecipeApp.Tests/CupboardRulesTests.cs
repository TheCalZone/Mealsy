using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;
using Xunit;

public class CupboardRulesTests
{
    [Theory]
    [InlineData("smoked paprika")][InlineData("parsley")][InlineData("herbs de Provence")]
    [InlineData("oregano")][InlineData("rosemary")][InlineData("ancho chilli flakes (1/4 tsp for mild)")]
    [InlineData("thyme")][InlineData("freshly ground black pepper")][InlineData("fennel seeds")]
    [InlineData("fresh chopped basil (half pack)")][InlineData("Dried, chopped basil",false)]
    public void MatchesWholeIngredientNamesAndPreparationDescriptions(string name,bool expected=true) => Assert.Equal(expected,ShoppingPlan.SuggestCupboardCheck(name));

    [Theory]
    [InlineData("sausage meat (pork)")][InlineData("onion")][InlineData("garlic")][InlineData("bulb fennel")]
    [InlineData("plum tomatoes (800g)")][InlineData("chopped kale/cavolo Nero/spinach")][InlineData("tomato puree")]
    [InlineData("double cream")][InlineData("rigatoni")][InlineData("grated mozzarella")][InlineData("grated Parmesan")]
    [InlineData("Salted butter")][InlineData("Pepperoni")][InlineData("Basil pesto")][InlineData("Sugar snap peas")]
    public void DoesNotExcludeFreshMainIngredientsOrSubstringMatches(string name) => Assert.False(ShoppingPlan.SuggestCupboardCheck(name));

    [Fact]
    public void ExamplePasteKeepsAmountsAndRoutesHerbsToVisibleUncheckedCupboardRows()
    {
        const string pasted="""
400g sausage meat (pork)
1 onion, diced
4 cloves garlic, minced
1 bulb fennel, diced
2 tins plum tomatoes (800g)
100g chopped kale/cavolo Nero/spinach
2 tbsp tomato puree
2 tbsp double cream
1/4 tsp sugar
500g rigatoni
200g grated mozzarella
80g grated Parmesan
1/2 tsp smoked paprika
1 tsp parsley
1 tsp herbs de Provence
1/2 tsp oregano
1/2 tsp rosemary
1/2 tsp ancho chilli flakes (1/4 tsp for mild)
1/2 thyme
1/2 tsp freshly ground black pepper
1/4 tsp fennel seeds
15g fresh chopped basil (half pack)
""";
        var ingredients=RecipeDraft.ParseIngredients(pasted).SelectMany(s=>s.Items).ToList();
        Assert.Equal(22,ingredients.Count);
        var recipe=new Recipe{RecipeName="Rigatoni",Serving=2,Ingredients=ingredients};
        var rows=ShoppingPlan.Build(new[]{new RecipeShoppingChoice(recipe){Selected=true}},Array.Empty<Snack>(),Array.Empty<Staple>(),Array.Empty<string>());
        Assert.Equal(22,rows.Count);Assert.Equal(11,rows.Count(r=>r.CheckCupboard));Assert.Equal(11,rows.Count(r=>r.Included));
        var basil=Assert.Single(rows,r=>r.Item.Name.StartsWith("fresh chopped basil"));
        Assert.False(basil.Included);Assert.Equal("15 g",basil.Item.Quantity);basil.Included=true;Assert.True(basil.Included);
        Assert.All(ingredients,i=>Assert.True(i.MainIngredient));
    }
    [Fact]
    public void ExplicitRecipeChoicesOverrideAutomaticRulesAndSavedEssentials()
    {
        var item=new Ingredient{Name="Fresh basil",Quantity="15 g",MainIngredient=true};
        Assert.True(CupboardRules.Check(item,CupboardRules.Defaults));
        item.ShoppingPreference=1;Assert.False(CupboardRules.Check(item,CupboardRules.Defaults,new[]{"Fresh basil"}));
        item.Name="Onion";item.ShoppingPreference=2;Assert.True(CupboardRules.Check(item,Array.Empty<string>()));
        var draft=RecipeDraft.FromRecipe(new Recipe{RecipeName="Test dish",RecipeMethod="Cook and serve.",Serving=2,Ingredients=new(){item}});
        Assert.Equal(2,Assert.Single(draft.Sections.Single().Items).ShoppingPreference);
        var recovered=JsonSerializer.Deserialize<RecipeDraft>(JsonSerializer.Serialize(draft));
        Assert.Equal(2,Assert.Single(recovered.Build().Ingredients).ShoppingPreference);
    }
    [Fact]
    public void CustomRulesCanBeAddedRemovedAndSavedSeparatelyForEachAccount()
    {
        var a="cupboard-"+Guid.NewGuid();var b="cupboard-"+Guid.NewGuid();
        var rules=CupboardStore.Load(a).Where(s=>s!="Basil").Append("Sesame oil").ToList();CupboardStore.Save(a,rules);
        Assert.True(CupboardRules.Matches("Sesame oil",CupboardStore.Load(a)));Assert.False(CupboardRules.Matches("Basil",CupboardStore.Load(a)));
        Assert.False(CupboardRules.Matches("Sesame oil",CupboardStore.Load(b)));Assert.True(CupboardRules.Matches("Basil",CupboardStore.Load(b)));
        CupboardStore.Save(a,Array.Empty<string>());Assert.Empty(CupboardStore.Load(a));CupboardStore.Reset(a);Assert.NotEmpty(CupboardStore.Load(a));
        Assert.Throws<InvalidOperationException>(()=>CupboardStore.Save(a,new[]{""}));
    }
    [Fact]
    public void ExistingIngredientTablesMigrateAndPersistExplicitShoppingChoices()
    {
        var path=Path.Combine(FileSystem.AppDataDirectory,"cupboard-"+Guid.NewGuid()+".db");
        using var db=new SQLite.SQLiteConnection(path);
        db.Execute("CREATE TABLE Ingredient (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, MainIngredient INTEGER)");
        db.Execute("INSERT INTO Ingredient (Name,MainIngredient) VALUES ('Basil',1)");db.CreateTable<Ingredient>();
        var old=Assert.Single(db.Table<Ingredient>());Assert.Equal(0,old.ShoppingPreference);old.ShoppingPreference=1;db.Update(old);
        Assert.Equal(1,db.Get<Ingredient>(old.Id).ShoppingPreference);
    }
}
