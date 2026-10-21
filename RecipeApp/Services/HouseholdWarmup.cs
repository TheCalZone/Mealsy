using RecipeApp.Models;
using System.Text.Json;
namespace RecipeApp.Services;

public record HouseholdQuickView(HouseholdInfo Household,string ShoppingJson,HouseholdWeek Week,DateTimeOffset ListChecked,DateTimeOffset MealsChecked);

/// <summary>Read-only startup warming of the shared list and current week, never historical data.</summary>
public static class HouseholdWarmup
{
    private record Saved(string Account,string Household,string Shopping,HouseholdWeek Week,DateTimeOffset ListChecked,DateTimeOffset MealsChecked);
    private static readonly object gate=new();
    private static readonly Dictionary<string,Task> flights=new();
    private static readonly Dictionary<string,DateTimeOffset> attempts=new();
    private static long revision;
    public static event Action Changed;
    public static string CurrentWeek=>SocialPolicy.DateKey(DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek+6)%7)));
    private static string Key(string account)=>"Mealsy.household.quick."+account;
    private static Saved Read(string account)
    {
        try{var saved=JsonSerializer.Deserialize<Saved>(Preferences.Get(Key(account),"null"));return saved?.Account==account?saved:null;}catch(JsonException){return null;}
    }
    private static void Notify(){var handlers=Changed;if(handlers==null)return;foreach(Action handler in handlers.GetInvocationList())try{handler();}catch(Exception){ }}
    public static HouseholdQuickView Cached
    {
        get
        {
            var uid=AccountSession.UserId;if(uid==null||!HouseholdService.Reads.TryGet<HouseholdInfo>(HouseholdReadPolicy.Status(uid),out var info)||info?.HouseholdId==null||!info.FamilyEnabled)return null;
            var saved=Read(uid);if(saved?.Household!=info.HouseholdId)return new(info,null,null,default,default);
            var cutoff=DateTimeOffset.UtcNow.AddDays(-1);
            return new(info,saved.ListChecked>cutoff?saved.Shopping:null,
                saved.MealsChecked>cutoff&&saved.Week?.WeekStart==CurrentWeek&&saved.Week.HouseholdId==info.HouseholdId?saved.Week:null,saved.ListChecked,saved.MealsChecked);
        }
    }
    public static string ShoppingDisplay(HouseholdQuickView view)
    {
        var uid=AccountSession.UserId;if(uid==null||view?.Household?.HouseholdId==null)return "[]";
        var key=$"ShoppingList.household.{uid}.{view.Household.HouseholdId}";
        var local=Preferences.Get(key,"");if(string.IsNullOrWhiteSpace(local))return view.ShoppingJson??"[]";
        if(string.IsNullOrWhiteSpace(view.ShoppingJson))return local;
        try{return ShoppingListSync.Reapply(Preferences.Get(key+".synced","[]"),local,view.ShoppingJson).Serialize();}
        catch(Exception){return local;} // Pending/conflicting device edits are never overwritten by preloading.
    }
    public static Task Start(bool force=false)
    {
        var uid=AccountSession.UserId;if(uid==null||!App.IsForeground)return Task.CompletedTask;
        TaskCompletionSource<bool> owner;
        lock(gate)
        {
            if(flights.TryGetValue(uid,out var running))return running;
            if(!force&&attempts.TryGetValue(uid,out var previous)&&DateTimeOffset.UtcNow-previous<HouseholdReadPolicy.MaxAge)return Task.CompletedTask;
            attempts[uid]=DateTimeOffset.UtcNow;owner=new(TaskCreationOptions.RunContinuationsAsynchronously);flights[uid]=owner.Task;
        }
        _=Run();return owner.Task;
        async Task Run()
        {
            var generation=Interlocked.Read(ref revision);
            try
            {
                var household=await HouseholdService.StatusAsync(force);AccountSession.Check(uid);
                if(household.HouseholdId==null||!household.FamilyEnabled){Preferences.Remove(Key(uid));Notify();return;}
                if(!App.IsForeground){lock(gate)attempts.Remove(uid);return;}
                Notify(); // A verified membership permits showing the previous local snapshot immediately.
                var list=LoadList();var week=LoadWeek();await Task.WhenAll(list,week);
                async Task LoadList()
                {
                    try
                    {
                        var result=await HouseholdService.CallAsync<ShoppingSyncResult>(new{action="read",scope="household",householdId=household.HouseholdId},force);
                        AccountSession.Check(uid);if(generation!=Interlocked.Read(ref revision))return;
                        lock(gate)
                        {
                            AccountSession.Check(uid);if(generation!=Interlocked.Read(ref revision))return;
                            var saved=Read(uid);if(saved?.Household!=household.HouseholdId)saved=new(uid,household.HouseholdId,null,null,default,default);
                            Preferences.Set(Key(uid),JsonSerializer.Serialize(saved with{Shopping=JsonSerializer.Serialize(result.Items),ListChecked=HouseholdService.Reads.CheckedAt(HouseholdReadPolicy.List(uid,household.HouseholdId))??DateTimeOffset.UtcNow}));
                        }
                        Notify();
                    }
                    catch(Exception error){AppDiagnostics.Report(error,"household.preload.shopping","PRELOAD_FAILURE");}
                }
                async Task LoadWeek()
                {
                    try
                    {
                        var result=await HouseholdMealsService.Call<HouseholdWeek>(new{action="readWeek",householdId=household.HouseholdId,weekStart=CurrentWeek},force);
                        AccountSession.Check(uid);if(generation!=Interlocked.Read(ref revision)||result.HouseholdId!=household.HouseholdId||result.WeekStart!=CurrentWeek)return;
                        lock(gate)
                        {
                            AccountSession.Check(uid);if(generation!=Interlocked.Read(ref revision))return;
                            var saved=Read(uid);if(saved?.Household!=household.HouseholdId)saved=new(uid,household.HouseholdId,null,null,default,default);
                            Preferences.Set(Key(uid),JsonSerializer.Serialize(saved with{Week=result,MealsChecked=HouseholdService.Reads.CheckedAt(HouseholdReadPolicy.Week(uid,household.HouseholdId,result.WeekStart))??DateTimeOffset.UtcNow}));
                        }
                        Notify();
                    }
                    catch(Exception error){AppDiagnostics.Report(error,"household.preload.meals","PRELOAD_FAILURE");}
                }
            }
            catch(Exception error){AppDiagnostics.Report(error,"household.preload","PRELOAD_FAILURE");}
            finally{Notify();lock(gate)flights.Remove(uid);owner.TrySetResult(true);}
        }
    }
    public static void RecordList(string uid,string household,string items)
    {
        if(uid==null||uid!=AccountSession.UserId||household==null)return;
        lock(gate)
        {
            var saved=Read(uid);if(saved?.Household!=household)saved=new(uid,household,null,null,default,default);
            Preferences.Set(Key(uid),JsonSerializer.Serialize(saved with{Shopping=items,ListChecked=DateTimeOffset.UtcNow}));
        }
        Notify();
    }
    public static void RecordWeek(string uid,HouseholdWeek week)
    {
        if(uid==null||uid!=AccountSession.UserId||week?.WeekStart!=CurrentWeek)return;
        var checkedAt=HouseholdService.Reads.CheckedAt(HouseholdReadPolicy.Week(uid,week.HouseholdId,week.WeekStart));
        if(checkedAt==null)return;
        lock(gate)
        {
            var saved=Read(uid);if(saved?.Household!=week.HouseholdId)saved=new(uid,week.HouseholdId,null,null,default,default);
            Preferences.Set(Key(uid),JsonSerializer.Serialize(saved with{Week=week,MealsChecked=checkedAt.Value}));
        }
        Notify();
    }
    public static void Invalidate(string uid,bool membership,bool meals)
    {
        if(membership)Interlocked.Increment(ref revision);
        if(uid==null)return;
        if(membership)Preferences.Remove(Key(uid));
        else if(meals){var saved=Read(uid);if(saved!=null)Preferences.Set(Key(uid),JsonSerializer.Serialize(saved with{Week=null,MealsChecked=default}));}
        lock(gate)attempts.Remove(uid);
        Notify();
    }
    public static void ResetSession(){HouseholdService.Reads.Clear();Interlocked.Increment(ref revision);lock(gate)attempts.Clear();Notify();}
}
