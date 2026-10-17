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
    public static List<Guid> Drop(IEnumerable<ShoppingListItem> items,IEnumerable<Guid> saved,Guid id,Guid target,bool after,IEnumerable<Guid> visible)
    {
        var ordered=Sort(items,saved);var source=ordered.FirstOrDefault(item=>item.Id==id);var destination=ordered.FirstOrDefault(item=>item.Id==target);var shown=visible.ToHashSet();
        if(source==null||destination==null||id==target||!shown.Contains(id)||!shown.Contains(target)||!source.Category.Equals(destination.Category,StringComparison.OrdinalIgnoreCase))return ordered.Select(item=>item.Id).ToList();
        var slots=Enumerable.Range(0,ordered.Count).Where(index=>shown.Contains(ordered[index].Id)&&ordered[index].Category.Equals(source.Category,StringComparison.OrdinalIgnoreCase)).ToList();
        var group=slots.Select(index=>ordered[index]).ToList();group.RemoveAll(item=>item.Id==id);var position=group.FindIndex(item=>item.Id==target);group.Insert(position+(after?1:0),source);
        for(var index=0;index<slots.Count;index++)ordered[slots[index]]=group[index];
        return ordered.Select(item=>item.Id).ToList();
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
