using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RecipeApp.Models;
namespace RecipeApp.Services;

public record SavedShoppingTrip(Guid Id, string Name, DateTimeOffset SavedAt, string ItemsJson)
{
    public static SavedShoppingTrip Create(string name, ShoppingListDocument list) => new(Guid.NewGuid(), SavedWeekTemplate.NameFor(name), DateTimeOffset.UtcNow, list.Serialize());
    public List<ShoppingListItem> ReusableItems() => ShoppingListDocument.Deserialize(ItemsJson).Items.Select(i => new ShoppingListItem { Name=i.Name, Quantity=i.Quantity, Category=i.Category, Note=i.Note, InBasket=false }).ToList();
}
public record SavedMealSlot(List<long> Recipes, List<string> Notes, string Text, string Portions);
public record SavedMealDay(long Index, List<SavedMealSlot> Slots);
public record SavedWeekTemplate(Guid Id, string Name, DateTimeOffset SavedAt, List<SavedMealDay> Days)
{
    public static string NameFor(string name)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>80)throw new InvalidOperationException("Choose a name from 1–80 characters.");
        return name.Trim();
    }
    public static SavedWeekTemplate Create(string name, IEnumerable<MealPlanDay> days) => new(Guid.NewGuid(),NameFor(name),DateTimeOffset.UtcNow,
        days.Select(d=>new SavedMealDay(d.MealDayIndex,Enumerable.Range(0,3).Select(s=>new SavedMealSlot(new(MealPlanning.Ids(d,s)),new(MealPlanning.Notes(d,s)),MealPlanning.SimpleMeal(d,s),s switch {0=>d.BreakfastPortionsJson,1=>d.LunchPortionsJson,_=>d.DinnerPortionsJson})).ToList())).ToList());
    public List<MealPlanDay> Apply(IEnumerable<MealPlanDay> target, IEnumerable<long> availableRecipes)
    {
        var available=availableRecipes.ToHashSet();
        if(Days.SelectMany(d=>d.Slots).SelectMany(s=>s.Recipes).Any(id=>!available.Contains(id)))throw new InvalidOperationException("A recipe in this template is no longer in your recipe book. Your current week has been kept. Save a revised template before reusing it.");
        var copies=target.Select(d=>MealPlanning.Copy(d)).ToList();
        foreach(var copy in copies)
        {
            var day=Days.FirstOrDefault(d=>d.Index==copy.MealDayIndex);if(day==null||day.Slots.Count!=3)throw new InvalidOperationException("This template is incomplete. Your week has been kept.");
            MealPlanning.Clear(copy);
            for(var s=0;s<3;s++){MealPlanning.Ids(copy,s).AddRange(day.Slots[s].Recipes);foreach(var note in day.Slots[s].Notes)MealPlanning.Notes(copy,s).Add(note);MealPlanning.SetSimpleMeal(copy,s,day.Slots[s].Text);}
            copy.BreakfastPortionsJson=day.Slots[0].Portions;copy.LunchPortionsJson=day.Slots[1].Portions;copy.DinnerPortionsJson=day.Slots[2].Portions;MealPlanning.Serialize(copy);
        }
        return copies;
    }
}

/// <summary>Account-isolated device library; atomic saves and bounded history.</summary>
public static class PremiumLibrary
{
    private static readonly SemaphoreSlim gate=new(1,1);
    public static string PathFor(string account,string kind)
    {
        if(string.IsNullOrWhiteSpace(account))throw new InvalidOperationException("Sign in to open your Premium library.");
        if(kind is not ("weeks" or "shopping"))throw new ArgumentException("Unknown library.",nameof(kind));
        return Path.Combine(FileSystem.AppDataDirectory,"premium-library",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(account)))+"."+kind+".json");
    }
    public static async Task<List<T>> Load<T>(string account,string kind)
    {
        var path=PathFor(account,kind);await gate.WaitAsync();
        try{return await Read<T>(path);}finally{gate.Release();}
    }
    private static async Task<List<T>> Read<T>(string path)
    {
        if(!File.Exists(path))return new();
        try{return JsonSerializer.Deserialize<List<T>>(await File.ReadAllTextAsync(path))??throw new JsonException();}
        catch(JsonException){File.Copy(path,path+".recovery",true);throw new InvalidOperationException("Your saved library couldn't be read. A recovery copy was kept; no saved entries were replaced.");}
    }
    public static async Task Add<T>(string account,string kind,T entry)
    {
        var path=PathFor(account,kind);await gate.WaitAsync();
        try{var entries=await Read<T>(path);if(entries.Count>=100)throw new InvalidOperationException("This library holds 100 entries. Delete an old entry before saving another.");entries.Insert(0,entry);await Write(path,entries);}finally{gate.Release();}
    }
    public static async Task Remove<T>(string account,string kind,Func<T,bool> matches)
    {
        var path=PathFor(account,kind);await gate.WaitAsync();
        try{var entries=await Read<T>(path);entries.RemoveAll(e=>matches(e));await Write(path,entries);}finally{gate.Release();}
    }
    private static async Task Write<T>(string path,List<T> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));await File.WriteAllTextAsync(path+".tmp",JsonSerializer.Serialize(entries));File.Move(path+".tmp",path,true);
    }
}
