using MvvmHelpers;
using RecipeApp.Models;
using System.Text.Json;

namespace RecipeApp.Services;

public class RecipeShoppingChoice : ObservableObject
{
    public Recipe Recipe { get; set; }
    private bool selected;
    private string servings, batches = "1";
    public bool Selected { get => selected; set => SetProperty(ref selected, value); }
    public string Servings { get => servings; set => SetProperty(ref servings, value); }
    public string Batches { get => batches; set => SetProperty(ref batches, value); }
    public RecipeShoppingChoice(Recipe recipe) { Recipe = recipe; servings = Math.Max(1, recipe.Serving).ToString(); }
    public decimal Factor()
    {
        if (!int.TryParse(Servings, out var portions) || portions < 1 || portions > 1000) throw new InvalidOperationException($"Choose 1–1,000 servings for {Recipe.RecipeName}.");
        if (!int.TryParse(Batches, out var count) || count < 1 || count > 100) throw new InvalidOperationException($"Choose 1–100 batches for {Recipe.RecipeName}.");
        return (decimal)portions / Math.Max(1, Recipe.Serving) * count;
    }
}
public class ShoppingReviewItem : ObservableObject
{
    public ShoppingListItem Item { get; set; }
    public string Sources { get; set; }
    public bool CheckCupboard { get; set; }
    public bool AmountNeedsReview { get; set; }
    private bool included;
    public bool Included { get => included; set => SetProperty(ref included, value); }
    public int DuplicateIndex { get; set; }
    private string key;
    public string Key => key ??= JsonSerializer.Serialize(new[] { ShoppingQuantity.Identity(Item.Name), ShoppingQuantity.Identity(Item.Category), ShoppingQuantity.Identity(Item.Note), Item.Quantity, DuplicateIndex.ToString() });
    public Ingredient ToIngredient() => new() { Name = Item.Name, Quantity = Item.Quantity, Tag = Item.Category, Note = Item.Note, MainIngredient = true };
}
public static class ShoppingPlan
{
    public static bool SuggestCupboardCheck(string name) => CupboardRules.Matches(name,CupboardRules.Defaults);

    public static List<ShoppingReviewItem> Build(IEnumerable<RecipeShoppingChoice> choices, IEnumerable<Snack> snacks, IEnumerable<Staple> essentials, IEnumerable<string> cupboardNames, IEnumerable<string> recognitionRules = null)
    {
        var sources = new List<(Ingredient Item, string Source, bool Check)>();
        var cupboard = cupboardNames.ToList(); var rules = (recognitionRules ?? CupboardRules.Defaults).ToList();
        foreach (var choice in choices.Where(c => c.Selected))
        {
            var factor = choice.Factor();
            foreach (var original in choice.Recipe.Ingredients ?? new())
            {
                var item = RecipeDraft.CopyIngredient(original);var input=ShoppingItemInput.Parse(item.Name,item.Quantity);item.Name=input.Name;item.Quantity=input.Quantity; item.InBasket = false; item.Quantity = ShoppingQuantity.Scale(item.Quantity, factor);
                sources.Add((item, choice.Recipe.RecipeName, CupboardRules.Check(item,rules,cupboard)));
            }
        }
        foreach (var s in snacks){var input=ShoppingItemInput.Parse(s.Name,s.Quantity);sources.Add((new() { Name=input.Name, Quantity=input.Quantity, Tag=s.Tag, Note=s.Note }, "Snacks", false));}
        foreach (var original in essentials)
        {
            var input=ShoppingItemInput.Parse(original.Name,original.Quantity);var s=new Staple{Name=input.Name,Quantity=input.Quantity,Tag=original.Tag,Note=original.Note};
            var matches=sources.Where(x => x.Source!="Snacks" && ShoppingQuantity.Identity(x.Item.Name)==ShoppingQuantity.Identity(s.Name)).ToList();
            var source="Essentials";
            if (matches.Count>0)
            {
                source += " · Recipe needs: " + string.Join("; ",matches.Select(x => $"{x.Source}: {x.Item.Quantity}" + (string.IsNullOrWhiteSpace(x.Item.Note) ? "" : $" ({x.Item.Note})")));
                // An explicitly selected usual purchase replaces that essential's recipe amounts.
                // Without a usual quantity, keep the recipe quantities and simply mark them needed.
                if (string.IsNullOrWhiteSpace(s.Quantity))
                {
                    for (var index=0; index<sources.Count; index++) if (matches.Contains(sources[index])) sources[index]=(sources[index].Item,sources[index].Source,false);
                    continue;
                }
                sources.RemoveAll(matches.Contains);
            }
            sources.Add((new() { Name=s.Name, Quantity=s.Quantity, Tag=s.Tag, Note=s.Note }, source, false));
        }
        var result = ShoppingListDocument.FromIngredients(sources.Select(s => s.Item)).Select(item => {
            var matching = sources.Where(s => ShoppingQuantity.Identity(s.Item.Name) == ShoppingQuantity.Identity(item.Name)
                && ShoppingQuantity.Identity(ShoppingListDocument.CategoryName(s.Item.Tag)) == ShoppingQuantity.Identity(item.Category)
                && ShoppingQuantity.Identity(s.Item.Note) == ShoppingQuantity.Identity(item.Note)
                && (ShoppingQuantity.Identity(s.Item.Quantity)==ShoppingQuantity.Identity(item.Quantity) ||
                    (ShoppingQuantity.TryParse(s.Item.Quantity,out var a) && ShoppingQuantity.TryParse(item.Quantity,out var b) && a.Unit==b.Unit))).ToList();
            // A mix of optional and required uses remains required unless the ingredient is a saved essential.
            var check = matching.All(s => s.Check);
            return new ShoppingReviewItem { Item=item, Sources=string.Join(" · ", matching.Select(s => s.Source).Distinct()), CheckCupboard=check,
                Included=!check, AmountNeedsReview=string.IsNullOrWhiteSpace(item.Quantity) || !ShoppingQuantity.TryParse(item.Quantity, out _) };
        }).ToList();
        var duplicates=new Dictionary<string,int>();
        foreach (var row in result)
        {
            var identity=JsonSerializer.Serialize(new[] {ShoppingQuantity.Identity(row.Item.Name),ShoppingQuantity.Identity(row.Item.Category),ShoppingQuantity.Identity(row.Item.Note),row.Item.Quantity});
            row.DuplicateIndex=duplicates.GetValueOrDefault(identity); duplicates[identity]=row.DuplicateIndex+1;
        }
        return result;
    }
    /// <summary>Each meal slot is one full preparation. Servings never divide away repeated meals.</summary>
    public static Dictionary<long, int> MealOccurrences(IEnumerable<MealPlanDay> days)
    {
        var result = new Dictionary<long, int>();
        foreach (var day in days)
        {
            foreach (var slot in new[] { (day.BreakfastRecipesIDs, day.BreakfastRecipeId), (day.LunchRecipesIDs, day.LunchRecipeId), (day.DinnerRecipesIDs, day.DinnerRecipeId) })
                foreach (var id in (slot.Item1 ?? new()).Concat(new[] { slot.Item2 }).Where(id => id > 0).Distinct())
                    result[id] = result.GetValueOrDefault(id) + 1;
        }
        return result;
    }
}
