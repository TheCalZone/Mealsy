using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;

public static class MealPlanning
{
    public static readonly string[] Slots={"Breakfast","Lunch","Dinner"};
    public static List<long> Ids(MealPlanDay day,int slot)=>slot switch {0=>day.BreakfastRecipesIDs,1=>day.LunchRecipesIDs,_=>day.DinnerRecipesIDs};
    public static IList<string> Notes(MealPlanDay day,int slot)=>slot switch {0=>day.BreakfastNotes,1=>day.LunchNotes,_=>day.DinnerNotes};
    public static Dictionary<long,int> Portions(MealPlanDay day,int slot)
    {
        try{return JsonSerializer.Deserialize<Dictionary<long,int>>(slot switch {0=>day.BreakfastPortionsJson,1=>day.LunchPortionsJson,_=>day.DinnerPortionsJson} ?? "{}") ?? new();}catch(JsonException){return new();}
    }
    public static int Servings(MealPlanDay day,int slot,Recipe recipe)=>Math.Clamp(Portions(day,slot).GetValueOrDefault(recipe.Id,(int)Math.Clamp(recipe.Serving,1,1000)),1,1000);
    public static void SetServings(MealPlanDay day,int slot,long id,int servings)
    {
        if(servings<1||servings>1000)throw new InvalidOperationException("Choose 1–1,000 servings.");
        var values=Portions(day,slot);values[id]=servings;var json=JsonSerializer.Serialize(values);
        if(slot==0)day.BreakfastPortionsJson=json;else if(slot==1)day.LunchPortionsJson=json;else day.DinnerPortionsJson=json;
    }
    public static void NormalizeLegacy(MealPlanDay day,IEnumerable<Recipe> recipes)
    {
        var legacy=new[]{day.BreakfastRecipeId,day.LunchRecipeId,day.DinnerRecipeId};
        for(var slot=0;slot<3;slot++)
        {
            var ids=Ids(day,slot);if(legacy[slot]>0&&!ids.Contains(legacy[slot]))ids.Insert(0,legacy[slot]);
            var distinct=ids.Where(id=>id>0).Distinct().ToList();ids.Clear();ids.AddRange(distinct);
        }
        day.BreakfastRecipes.ReplaceRange(recipes.Where(r=>day.BreakfastRecipesIDs.Contains(r.Id)));
        day.LunchRecipes.ReplaceRange(recipes.Where(r=>day.LunchRecipesIDs.Contains(r.Id)));
        day.DinnerRecipes.ReplaceRange(recipes.Where(r=>day.DinnerRecipesIDs.Contains(r.Id)));
    }
    public static void Serialize(MealPlanDay day)
    {
        day.BreakfastRecipeId=day.LunchRecipeId=day.DinnerRecipeId=0;
        day.BreakfastIDJson=JsonSerializer.Serialize(day.BreakfastRecipesIDs);day.LunchIDJson=JsonSerializer.Serialize(day.LunchRecipesIDs);day.DinnerIDJson=JsonSerializer.Serialize(day.DinnerRecipesIDs);
        day.BreakfastNotesJson=JsonSerializer.Serialize(day.BreakfastNotes);day.LunchNotesJson=JsonSerializer.Serialize(day.LunchNotes);day.DinnerNotesJson=JsonSerializer.Serialize(day.DinnerNotes);
        day.Breakfast=day.Lunch=day.Dinner="";
    }
    public static MealPlanDay Copy(MealPlanDay source,MealPlanDay target=null)
    {
        target??=new MealPlanDay {Id=source.Id,MealPlanID=source.MealPlanID,MealDayIndex=source.MealDayIndex,MealDay=source.MealDay};
        for(var slot=0;slot<3;slot++){Ids(target,slot).Clear();Ids(target,slot).AddRange(Ids(source,slot));Notes(target,slot).Clear();foreach(var note in Notes(source,slot))Notes(target,slot).Add(note);}
        target.BreakfastPortionsJson=source.BreakfastPortionsJson;target.LunchPortionsJson=source.LunchPortionsJson;target.DinnerPortionsJson=source.DinnerPortionsJson;
        Serialize(target);return target;
    }
    public static void Clear(MealPlanDay day){for(var slot=0;slot<3;slot++){Ids(day,slot).Clear();Notes(day,slot).Clear();}day.BreakfastPortionsJson=day.LunchPortionsJson=day.DinnerPortionsJson="{}";Serialize(day);}
    public static string State(MealPlanDay day)=>JsonSerializer.Serialize(new {ids=Enumerable.Range(0,3).Select(s=>Ids(day,s).ToArray()).ToArray(),notes=Enumerable.Range(0,3).Select(s=>Notes(day,s).ToArray()).ToArray(),day.BreakfastPortionsJson,day.LunchPortionsJson,day.DinnerPortionsJson});
    public static List<RecipeShoppingChoice> ShoppingChoices(IEnumerable<MealPlanDay> days,IEnumerable<Recipe> recipes)
    {
        var available=recipes.ToDictionary(r=>r.Id);var totals=new Dictionary<long,int>();
        foreach(var day in days)for(var slot=0;slot<3;slot++)foreach(var id in Ids(day,slot).Distinct())
        {
            if(!available.TryGetValue(id,out var recipe))throw new InvalidOperationException("A planned recipe is missing from this account. Remove or replace it before shopping.");
            totals[id]=totals.GetValueOrDefault(id)+Servings(day,slot,recipe);
        }
        return totals.Select(p=>new RecipeShoppingChoice(available[p.Key]){Selected=true,Servings=p.Value.ToString()}).ToList();
    }
}
