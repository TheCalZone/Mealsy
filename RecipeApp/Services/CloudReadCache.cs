using System.Text.Json;
namespace RecipeApp.Services;

/// <summary>Short-lived, read-only results with coalesced requests and mutation invalidation.</summary>
public sealed class CloudReadCache
{
    private record Entry(string Json,DateTimeOffset Checked,DateTimeOffset Expires);
    private static readonly JsonSerializerOptions json=new(){PropertyNameCaseInsensitive=true};
    private readonly object gate=new();
    private readonly Dictionary<string,Entry> values=new();
    private readonly Dictionary<string,Task<string>> flights=new();
    private readonly Dictionary<string,long> versions=new();
    private readonly Func<DateTimeOffset> clock;
    public CloudReadCache(Func<DateTimeOffset> clock=null)=>this.clock=clock??(()=>DateTimeOffset.UtcNow);
    public bool TryGet<T>(string key,out T value)
    {
        lock(gate){if(values.TryGetValue(key,out var entry)&&entry.Expires>clock()){value=JsonSerializer.Deserialize<T>(entry.Json,json);return true;}}
        value=default;return false;
    }
    public DateTimeOffset? CheckedAt(string key){lock(gate)return values.TryGetValue(key,out var entry)&&entry.Expires>clock()?entry.Checked:null;}
    public void Store<T>(string key,T value,TimeSpan age){lock(gate){var now=clock();values[key]=new(JsonSerializer.Serialize(value),now,now+age);}}
    public void Invalidate(string key)
    {lock(gate){values.Remove(key);versions[key]=versions.GetValueOrDefault(key)+1;flights.Remove(key);}}
    public void Clear()
    {lock(gate){foreach(var key in values.Keys.Concat(flights.Keys).Distinct().ToList())versions[key]=versions.GetValueOrDefault(key)+1;values.Clear();flights.Clear();}}
    public async Task<T> Read<T>(string key,Func<Task<T>> fetch,TimeSpan age,bool force=false)
    {
        Task<string> task;TaskCompletionSource<string> owner=null;long version=0;
        lock(gate)
        {
            if(!force&&values.TryGetValue(key,out var cached)&&cached.Expires>clock())return JsonSerializer.Deserialize<T>(cached.Json,json);
            version=versions.GetValueOrDefault(key);
            if(!flights.TryGetValue(key,out task))
            {
                owner=new(TaskCreationOptions.RunContinuationsAsynchronously);task=owner.Task;flights[key]=task;version=versions.GetValueOrDefault(key);
            }
        }
        if(owner!=null)_=Fetch();
        var result=await task;
        lock(gate)if(versions.GetValueOrDefault(key)!=version)throw new CloudReadInvalidatedException();
        return JsonSerializer.Deserialize<T>(result,json);
        async Task Fetch()
        {
            try
            {
                var result=JsonSerializer.Serialize(await fetch());
                lock(gate)
                {
                    if(versions.GetValueOrDefault(key)!=version)throw new CloudReadInvalidatedException();
                    var now=clock();values[key]=new(result,now,now+age);
                }
                owner.TrySetResult(result);
            }
            catch(Exception error){owner.TrySetException(error);}
            finally{lock(gate)if(flights.TryGetValue(key,out var current)&&ReferenceEquals(current,task))flights.Remove(key);}
        }
    }
}
public class CloudReadInvalidatedException:Exception { }

public static class HouseholdReadPolicy
{
    public static readonly TimeSpan MaxAge=TimeSpan.FromMinutes(1);
    public static string Status(string account)=>JsonSerializer.Serialize(new[]{account,"household","status"});
    public static string List(string account,string household)=>JsonSerializer.Serialize(new[]{account,"shopping",household==null?"personal":"household",household});
    public static string Week(string account,string household,string week)=>JsonSerializer.Serialize(new[]{account,"meals",household,week});
    public static string Key(string account,string endpoint,JsonElement request)
    {
        if(string.IsNullOrEmpty(account))return null;
        string Text(string name)=>request.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
        var action=Text("action");
        if(endpoint.EndsWith("/household",StringComparison.Ordinal))return action switch {
            "status"=>Status(account),"read" when Text("scope")=="personal"=>List(account,null),
            "read" when Text("scope")=="household"&&!string.IsNullOrEmpty(Text("householdId"))=>List(account,Text("householdId")),_=>null};
        if(endpoint.EndsWith("/householdMeals",StringComparison.Ordinal))return action switch {
            "readWeek"=>Week(account,Text("householdId"),Text("weekStart")),
            "capabilities"=>JsonSerializer.Serialize(new[]{account,"householdMeals","capabilities"}),_=>null};
        return null;
    }
}
