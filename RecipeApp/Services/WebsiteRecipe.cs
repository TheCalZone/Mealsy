using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;
using HtmlAgilityPack;
using RecipeApp.Models;
namespace RecipeApp.Services;
public static class WebsiteRecipe
{
    private static readonly string[] Hosts={"bbcgoodfood.com","bbc.co.uk","jamieoliver.com","allrecipes.com","realfood.tesco.com","deliciousmagazine.co.uk","recipetineats.com"};
    public static Uri Validate(string value)
    {
        if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!uri.IsDefaultPort||uri.UserInfo.Length>0||!Hosts.Any(h=>uri.IdnHost.Equals(h,StringComparison.OrdinalIgnoreCase)||uri.IdnHost.EndsWith("."+h,StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Use a recipe link from BBC Food, Good Food, Jamie Oliver, Allrecipes, Tesco, Delicious or RecipeTin Eats. Other websites can be copied into the recipe editor manually.");
        return uri;
    }
    public static Recipe Parse(string html,string source)
    {
        if(html.Length>2_000_000)throw new InvalidOperationException("This page is too large to import.");
        var doc=new HtmlDocument();doc.LoadHtml(html);
        foreach(var script in doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']")??new HtmlNodeCollection(null)){
            try{using var json=JsonDocument.Parse(script.InnerHtml,new JsonDocumentOptions{MaxDepth=40});var node=Find(json.RootElement);if(node.HasValue)return Convert(node.Value,source);}catch(JsonException){}
        }
        throw new InvalidOperationException("This page does not provide a readable recipe. Open its recipe page, or paste the ingredients and method into a new recipe.");
    }
    private static JsonElement? Find(JsonElement value){
        if(value.ValueKind==JsonValueKind.Array){foreach(var item in value.EnumerateArray()){var found=Find(item);if(found.HasValue)return found;}}
        if(value.ValueKind==JsonValueKind.Object){if(value.TryGetProperty("@type",out var type)&&(type.ValueKind==JsonValueKind.String?type.GetString()=="Recipe":type.ValueKind==JsonValueKind.Array&&type.EnumerateArray().Any(t=>t.ValueKind==JsonValueKind.String&&t.GetString()=="Recipe")))return value;
            if(value.TryGetProperty("@graph",out var graph))return Find(graph);}
        return null;
    }
    private static string Plain(string text){var doc=new HtmlDocument();doc.LoadHtml(text??"");return WebUtility.HtmlDecode(doc.DocumentNode.InnerText).Trim();}
    private static string Text(JsonElement node,string key)=>node.TryGetProperty(key,out var v)?v.ValueKind==JsonValueKind.String?Plain(v.GetString()):v.ValueKind==JsonValueKind.Number?v.ToString():"":"";
    private static IEnumerable<string> Steps(JsonElement value){
        if(value.ValueKind==JsonValueKind.String){yield return Plain(value.GetString());}
        else if(value.ValueKind==JsonValueKind.Array){foreach(var item in value.EnumerateArray())foreach(var step in Steps(item))yield return step;}
        else if(value.ValueKind==JsonValueKind.Object){var text=Text(value,"text");if(text.Length>0)yield return text;else if(value.TryGetProperty("itemListElement",out var children))foreach(var step in Steps(children))yield return step;}
    }
    private static int Minutes(string duration){try{return (int)Math.Clamp(System.Xml.XmlConvert.ToTimeSpan(duration).TotalMinutes,0,10000);}catch{return 0;}}
    private static Recipe Convert(JsonElement node,string source){
        var ingredients=node.TryGetProperty("recipeIngredient",out var list)&&list.ValueKind==JsonValueKind.Array?list.EnumerateArray().Where(i=>i.ValueKind==JsonValueKind.String).Take(150).Select(i=>Plain(i.GetString())).ToList():new();
        var steps=node.TryGetProperty("recipeInstructions",out var method)?Steps(method).Take(100).ToList():new();
        var yield=Text(node,"recipeYield");if(node.TryGetProperty("recipeYield",out var y)&&y.ValueKind==JsonValueKind.Array)yield=Plain(y.EnumerateArray().FirstOrDefault().ToString());
        var match=Regex.Match(yield,@"\d+");var portions=int.TryParse(match.Value,out var count)?Math.Clamp(count,1,1000):2;
        if(ingredients.Count==0||steps.Count==0)throw new InvalidOperationException("The page's recipe is incomplete. Copy it into the editor manually.");
        return new Recipe{RecipeName=Text(node,"name"),Description=Text(node,"description"),SourceUrl=source,Serving=portions,PrepTime=Minutes(Text(node,"prepTime")),CookTime=Minutes(Text(node,"cookTime")),RecipeMethod=string.Join("\n\n",steps),Ingredients=RecipeDraft.ParseIngredients(string.Join("\n",ingredients)).SelectMany(s=>s.Items).ToList()};
    }
    public static async Task<Recipe> Fetch(string source){
        var uri=Validate(source);using var handler=new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false};using var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)};
        for(var redirects=0;redirects<4;redirects++){
            using var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead);
            if((int)response.StatusCode is >=300 and <400){var location=response.Headers.Location??throw new InvalidOperationException("The recipe link redirected without a destination.");uri=Validate(new Uri(uri,location).AbsoluteUri);continue;}
            response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>2_000_000)throw new InvalidOperationException("This page is too large to import.");
            using var stream=await response.Content.ReadAsStreamAsync();using var reader=new StreamReader(stream);var buffer=new char[8192];var text=new System.Text.StringBuilder();int n;
            while((n=await reader.ReadAsync(buffer,0,buffer.Length))>0){text.Append(buffer,0,n);if(text.Length>2_000_000)throw new InvalidOperationException("This page is too large to import.");}
            return Parse(text.ToString(),uri.AbsoluteUri);
        }throw new InvalidOperationException("The recipe link redirected too many times.");
    }
}
