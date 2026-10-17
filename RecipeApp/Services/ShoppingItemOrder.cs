using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;

public static class ShoppingItemOrder
{
    public static List<Guid> Read(string json){try{return (JsonSerializer.Deserialize<List<Guid>>(json??"null")??new()).Where(id=>id!=Guid.Empty).Distinct().ToList();}catch(JsonException){return new();}}
    public static List<ShoppingListItem> Sort(IEnumerable<ShoppingListItem> items,IEnumerable<Guid> saved)
    {
        var ranks=(saved??Array.Empty<Guid>()).Distinct().Select((id,index)=>(id,index)).ToDictionary(x=>x.id,x=>x.index);
        return items.OrderBy(item=>ranks.GetValueOrDefault(item.Id,int.MaxValue)).ToList();
    }
    public static List<Guid> Move(IEnumerable<ShoppingListItem> items,IEnumerable<Guid> saved,Guid id,int direction,IEnumerable<Guid> visible)
    {
        var ordered=Sort(items,saved);var source=ordered.FindIndex(item=>item.Id==id);
        if(source<0||direction is not (-1 or 1))return ordered.Select(item=>item.Id).ToList();
        var shown=visible.ToHashSet();var category=ordered[source].Category;
        var neighbors=ordered.Where(item=>shown.Contains(item.Id)&&item.Category.Equals(category,StringComparison.OrdinalIgnoreCase)).ToList();
        var index=neighbors.FindIndex(item=>item.Id==id);
        if(index<0||index+direction<0||index+direction>=neighbors.Count)return ordered.Select(item=>item.Id).ToList();
        var target=ordered.FindIndex(item=>item.Id==neighbors[index+direction].Id);
        (ordered[source],ordered[target])=(ordered[target],ordered[source]);return ordered.Select(item=>item.Id).ToList();
    }
}
