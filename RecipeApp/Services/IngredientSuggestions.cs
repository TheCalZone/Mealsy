using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace RecipeApp.Services;

public static class IngredientSuggestions
{
    public record Source(string Name,string Owner);
    public static List<string> ForAccount(IEnumerable<Source> sources,string account)=>sources.Where(source=>account==null?string.IsNullOrEmpty(source.Owner):source.Owner==account).Select(source=>Name(source.Name)).Where(name=>name.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).Take(1000).ToList();
    private static readonly string[] Common=("Minced beef|Minced lamb|Minced pork|Minced garlic|Minced ginger|Chicken breast|Chicken thighs|Chicken drumsticks|Beef steak|Pork chops|Bacon|Ham|Sausages|Turkey mince|Lamb chops|Salmon fillets|Cod fillets|Tuna|Prawns|Tofu|Tempeh|Vegan mince|Eggs|Milk|Oat milk|Almond milk|Butter|Cheddar cheese|Mozzarella|Parmesan|Feta cheese|Greek yoghurt|Double cream|Crème fraîche|Apples|Bananas|Oranges|Lemons|Limes|Pears|Grapes|Strawberries|Blueberries|Raspberries|Mango|Pineapple|Avocado|Tomatoes|Cherry tomatoes|Chopped tomatoes|Tomato puree|Potatoes|Sweet potatoes|Onions|Red onions|Spring onions|Garlic|Carrots|Broccoli|Cauliflower|Spinach|Lettuce|Cabbage|Kale|Cucumber|Courgette|Aubergine|Mushrooms|Red peppers|Green peppers|Chillies|Celery|Leeks|Fresh ginger|Butternut squash|Green beans|Peas|Sweetcorn|Fresh basil|Fresh parsley|Fresh coriander|Fresh mint|Dried basil|Oregano|Thyme|Rosemary|Mixed herbs|Bay leaves|Cumin|Paprika|Smoked paprika|Cinnamon|Turmeric|Chilli powder|Chilli flakes|Garlic granules|Salt|Black pepper|Sugar|Plain flour|Self raising flour|Baking powder|Olive oil|Vegetable oil|Sesame oil|Soy sauce|Fish sauce|Worcestershire sauce|Balsamic vinegar|White wine vinegar|Chicken stock|Beef stock|Vegetable stock|Coconut milk|Coconut cream|Chickpeas|Kidney beans|Black beans|Baked beans|Red lentils|Green lentils|Rice|Basmati rice|Brown rice|Spaghetti|Penne pasta|Lasagne sheets|Noodles|Couscous|Quinoa|Rolled oats|Bread|Bread rolls|Bagels|Tortillas|Naan bread|Pitta bread|Peanut butter|Honey|Jam|Coffee|Tea|Orange juice|Apple juice|Sparkling water|Frozen peas|Frozen spinach|Frozen berries|Ice cream").Split('|');
    private static string Key(string value)
    {
        var decomposed=(value??"").Normalize(NormalizationForm.FormD);
        var text=new string(decomposed.Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(text.ToLowerInvariant(),@"[^\p{L}\p{N}]+"," ").Trim();
    }
    public static string Name(string value)
    {
        var name=ShoppingItemInput.Parse(value,"").Name.Trim();return name.Length is >0 and <=200&&!name.Any(char.IsControl)?name:"";
    }
    public static List<string> Suggest(string input,IEnumerable<string> personal,IReadOnlyDictionary<string,int> history,int limit=5)
    {
        var query=Key(ShoppingItemInput.Parse(input,"").Name);if(query.Length<2||limit<=0)return new();
        var local=(personal??Array.Empty<string>()).Select(Name).Where(n=>n.Length>0).DistinctBy(Key).Take(1000).ToList();
        var uses=(history??new Dictionary<string,int>()).GroupBy(pair=>Key(pair.Key)).ToDictionary(g=>g.Key,g=>g.Max(pair=>pair.Value));
        var personalKeys=local.Select(Key).ToHashSet();var tokens=query.Split(' ');
        return local.Concat(history?.Keys??Array.Empty<string>()).Concat(Common).Select(Name).Where(n=>n.Length>0).DistinctBy(Key)
            .Select((name,index)=>new{name,index,key=Key(name)})
            .Where(item=>item.key!=query&&tokens.All(token=>item.key.Split(' ').Any(word=>word.StartsWith(token,StringComparison.Ordinal))))
            .OrderBy(item=>item.key.StartsWith(query,StringComparison.Ordinal)?0:1).ThenByDescending(item=>uses.GetValueOrDefault(item.key))
            .ThenByDescending(item=>personalKeys.Contains(item.key)).ThenBy(item=>item.index).Take(Math.Min(limit,5)).Select(item=>item.name).ToList();
    }
    public static Dictionary<string,int> ReadHistory(string json)
    {
        try{return (JsonSerializer.Deserialize<Dictionary<string,int>>(json??"null")??new()).Where(pair=>Name(pair.Key).Length>0&&pair.Value>0).OrderByDescending(pair=>pair.Value).Take(200).ToDictionary(pair=>pair.Key,pair=>Math.Min(pair.Value,10000));}
        catch(JsonException){return new();}
    }
    public static Dictionary<string,int> Remember(IReadOnlyDictionary<string,int> previous,string value)
    {
        var name=Name(value);var copy=previous.ToDictionary(pair=>pair.Key,pair=>pair.Value);if(name.Length==0)return copy;
        var existing=copy.Keys.FirstOrDefault(n=>Key(n)==Key(name));var count=existing==null?0:copy[existing];if(existing!=null)copy.Remove(existing);
        return new[]{new KeyValuePair<string,int>(name,Math.Min(count+1,10000))}.Concat(copy).OrderByDescending(pair=>pair.Value).Take(200).ToDictionary(pair=>pair.Key,pair=>pair.Value);
    }
}
