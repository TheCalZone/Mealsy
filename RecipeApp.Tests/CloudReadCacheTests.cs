using System.Text.Json;
using RecipeApp.Services;
using Xunit;
namespace RecipeApp.Tests;
public class CloudReadCacheTests
{
    [Fact]
    public async Task ConcurrentScreensShareOneReadAndFreshResultsExpire()
    {
        var now=DateTimeOffset.UtcNow;var cache=new CloudReadCache(()=>now);var pending=new TaskCompletionSource<string>();var calls=0;
        Task<string> Fetch(){calls++;return pending.Task;}
        var first=cache.Read("alice",Fetch,TimeSpan.FromMinutes(1));var second=cache.Read("alice",Fetch,TimeSpan.FromMinutes(1));
        Assert.Equal(1,calls);pending.SetResult("fresh");Assert.Equal("fresh",await first);Assert.Equal("fresh",await second);
        Assert.Equal("fresh",await cache.Read("alice",Fetch,TimeSpan.FromMinutes(1)));Assert.Equal(1,calls);
        Assert.Equal(now,cache.CheckedAt("alice"));
        now=now.AddMinutes(1);Assert.Null(cache.CheckedAt("alice"));await cache.Read("alice",Fetch,TimeSpan.FromMinutes(1));Assert.Equal(2,calls);
        await cache.Read("alice",Fetch,TimeSpan.FromMinutes(1),true);Assert.Equal(3,calls);
    }
    [Fact]
    public async Task MutationPreventsAnOlderInflightReadFromOverwritingFreshData()
    {
        var cache=new CloudReadCache();var pending=new TaskCompletionSource<string>();
        var old=cache.Read("list",()=>pending.Task,TimeSpan.FromMinutes(1));cache.Invalidate("list");
        Assert.Equal("new",await cache.Read("list",()=>Task.FromResult("new"),TimeSpan.FromMinutes(1)));
        pending.SetResult("old");await Assert.ThrowsAsync<CloudReadInvalidatedException>(()=>old);
        Assert.True(cache.TryGet<string>("list",out var saved));Assert.Equal("new",saved);
    }
    [Fact]
    public async Task FailedReadsAreNotCachedAndCallersCannotMutateCachedObjects()
    {
        var cache=new CloudReadCache();await Assert.ThrowsAsync<InvalidOperationException>(()=>cache.Read<string>("key",()=>throw new InvalidOperationException(),TimeSpan.FromMinutes(1)));
        var result=await cache.Read("key",()=>Task.FromResult(new List<string>{"kept"}),TimeSpan.FromMinutes(1));result.Clear();
        Assert.True(cache.TryGet<List<string>>("key",out var next));Assert.Equal("kept",Assert.Single(next));
        cache.Clear();Assert.False(cache.TryGet<List<string>>("key",out _));
    }
    private record Result(List<string> Items);
    [Fact]
    public async Task CallableMutationJsonCanPrimeATypedReadWithoutAnotherRequest()
    {
        var cache=new CloudReadCache();cache.Store("list",JsonSerializer.SerializeToElement(new{items=new[]{"milk"}}),TimeSpan.FromMinutes(1));
        var result=await cache.Read<Result>("list",()=>throw new InvalidOperationException("Must reuse acknowledged mutation"),TimeSpan.FromMinutes(1));
        Assert.Equal("milk",Assert.Single(result.Items));
    }
    [Fact]
    public void KeysSeparateAccountsHouseholdsAndWeeksAndNeverCacheWrites()
    {
        const string endpoint="https://example.com/household";
        string Key(string account,object request)=>HouseholdReadPolicy.Key(account,endpoint,JsonSerializer.SerializeToElement(request));
        Assert.NotEqual(Key("alice",new{action="status"}),Key("bob",new{action="status"}));
        Assert.NotEqual(Key("alice",new{action="read",scope="personal"}),Key("alice",new{action="read",scope="household",householdId="family"}));
        Assert.NotEqual(HouseholdReadPolicy.Week("alice","family","2026-10-19"),HouseholdReadPolicy.Week("alice","family","2026-10-26"));
        Assert.Equal(Key("alice",new{action="read",scope="household",householdId="family"}),Key("alice",new{action="read",scope="household",householdId="family",changes=Array.Empty<string>()}));
        Assert.NotEqual(HouseholdReadPolicy.List("alice",null),HouseholdReadPolicy.List("alice","personal"));
        Assert.Null(Key("alice",new{action="patch",scope="household",householdId="family"}));Assert.Null(Key(null,new{action="status"}));
        Assert.Null(HouseholdReadPolicy.Key("alice","https://example.com/billing",JsonSerializer.SerializeToElement(new{action="status"})));
    }
}
