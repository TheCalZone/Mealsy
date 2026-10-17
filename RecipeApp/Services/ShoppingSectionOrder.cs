using System.Text.Json;
namespace RecipeApp.Services;

public static class ShoppingSectionOrder
{
    private static List<string> Names(IEnumerable<string> values)=>(values??Array.Empty<string>())
        .Where(value=>!string.IsNullOrWhiteSpace(value)).Select(ShoppingListDocument.CategoryName)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    public static List<string> Read(string json)
    {
        try{return Names(JsonSerializer.Deserialize<List<string>>(json??"null"));}
        catch(JsonException){return new();}
    }
    public static List<string> Sort(IEnumerable<string> available,IEnumerable<string> saved)
    {
        var names=Names(available);var order=Names(saved);
        return names.OrderBy(name=>{var index=order.FindIndex(n=>n.Equals(name,StringComparison.OrdinalIgnoreCase));return index<0?int.MaxValue:index;})
            .ThenBy(name=>name.Equals("Other",StringComparison.OrdinalIgnoreCase)).ThenBy(name=>name,StringComparer.OrdinalIgnoreCase).ToList();
    }
    public static List<string> Drop(IEnumerable<string> order,string section,string target,bool after)
    {
        var names=Names(order);var source=names.FindIndex(n=>n.Equals(section,StringComparison.OrdinalIgnoreCase));var destination=names.FindIndex(n=>n.Equals(target,StringComparison.OrdinalIgnoreCase));
        if(source<0||destination<0||source==destination)return names;
        var moved=names[source];names.RemoveAt(source);destination=names.FindIndex(n=>n.Equals(target,StringComparison.OrdinalIgnoreCase));names.Insert(destination+(after?1:0),moved);return names;
    }
    public static List<string> Move(IEnumerable<string> order,string section,int direction)
    {
        var names=Names(order);var index=names.FindIndex(name=>name.Equals(section,StringComparison.OrdinalIgnoreCase));
        if(direction is not (-1 or 1)||index<0||index+direction<0||index+direction>=names.Count)return names;
        (names[index],names[index+direction])=(names[index+direction],names[index]);return names;
    }
}
