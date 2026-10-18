using Plugin.Firebase.Crashlytics;
using System.Diagnostics;
using System.Text.Json;

namespace RecipeApp.Services;

public record DiagnosticEntry(string Reference,string Operation,string Code,string Reason,DateTimeOffset CreatedAt,string Status="Recorded locally");
public static class AppDiagnostics
{
    private static readonly object gate = new();
    private static readonly Dictionary<string,DateTimeOffset> recent = new();
    private const string HistoryKey="Diagnostics.history.v1", EnabledKey="Diagnostics.enabled";
    private static int sent;
    internal static Action<Exception,Dictionary<string,string>> Send = (error,keys) => {
        var sdk=CrossFirebaseCrashlytics.Current;
        foreach(var pair in keys)sdk.SetCustomKey(pair.Key,pair.Value);
        sdk.Log($"Failure {keys["operation"]} {keys["error_code"]} {keys["failure_reason"]} reference={keys["report_reference"]}");
        sdk.RecordException(error);
    };
    private static int registered;
    public static void RegisterManagedHandlers() {
        if(Interlocked.Exchange(ref registered,1)!=0)return;
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=> { if(e.ExceptionObject is Exception error)Report(error,"ui.action"); };
        TaskScheduler.UnobservedTaskException+=(_,e)=>Report(e.Exception,"ui.action");
    }
    public static bool Enabled => Preferences.Get(EnabledKey,"true")=="true";
    public static void SetEnabled(bool value) { Preferences.Set(EnabledKey,value?"true":"false"); ApplyCollection(); }
    public static void ApplyCollection() { try { CrossFirebaseCrashlytics.Current.SetCrashlyticsCollectionEnabled(Enabled); if(!Enabled)CrossFirebaseCrashlytics.Current.DeleteUnsentReports(); } catch { } }
    public static List<DiagnosticEntry> History() { try{return JsonSerializer.Deserialize<List<DiagnosticEntry>>(Preferences.Get(HistoryKey,"[]"))??new();}catch{return new();} }
    public static string AccountFailure(Exception error,string operation,string reason=null) {
        var message=AccountInput.Error(reason??"");
        if(new[]{"WrongPassword","UserNotFound","InvalidCredential","InvalidEmail","EmailAlreadyInUse","WeakPassword","UserDisabled","TooManyRequests","UserTokenExpired","InvalidUserToken"}.Contains(reason))return message;
        return message+"\nReference: "+Report(error,operation,"AUTH_SERVICE_FAILURE");
    }
    public static string Report(Exception error,string operation,string code="UNEXPECTED",string reason="NONE",string reference=null)
    {
        try {
            lock(gate) {
                if(error.Data["Mealsy.DiagnosticReference"] is string known)return known;
                reference=DiagnosticPolicy.Reference(reference);error.Data["Mealsy.DiagnosticReference"]=reference;
                // Only fixed operation names, codes and reason categories leave the device.
                var parts=operation.Split('.',2);operation=parts.Length==2 && new[]{"householdMeals","household","foodFeed","billing","administration","business","notifications","promotions"}.Contains(parts[0])?DiagnosticPolicy.Operation(parts[0],parts[1]):DiagnosticPolicy.LocalOperation(operation);
                code=DiagnosticPolicy.Code(code);reason=DiagnosticPolicy.Reason(reason);
                var entry=new DiagnosticEntry(reference,operation,code,reason,DateTimeOffset.UtcNow);
                var history=History();history.Insert(0,entry);Preferences.Set(HistoryKey,JsonSerializer.Serialize(history.Take(20)));
                if(!Enabled)return reference;
                void Mark(string status) { var saved=History();var index=saved.FindIndex(e=>e.Reference==reference);if(index>=0)saved[index]=saved[index] with{Status=status};Preferences.Set(HistoryKey,JsonSerializer.Serialize(saved)); }
                var signature=operation+"|"+code+"|"+reason;
                if(sent>=20)return reference;
                if(recent.TryGetValue(signature,out var last) && entry.CreatedAt-last<TimeSpan.FromMinutes(1)){Mark("Grouped with recent report");return reference;}
                recent[signature]=entry.CreatedAt;sent++;
                // Do not attach the original exception: provider messages and inner
                // exceptions can contain URLs, tokens, submitted text or email addresses.
                var frames=new StackTrace(error,false).GetFrames()??Array.Empty<StackFrame>();
                var stack=string.Join("\n",frames.Select(f=>f.GetMethod()).Where(m=>m?.DeclaringType?.Namespace?.StartsWith("RecipeApp",StringComparison.Ordinal)==true)
                    .Select(m=>"   at "+m.DeclaringType.FullName+"."+m.Name+"()").Take(20));
                Send(new SanitizedDiagnosticException($"{operation}: {code} ({reason})",stack),new(){["operation"]=operation,["error_code"]=code,["failure_reason"]=reason,["report_reference"]=reference});
                Mark("Queued for reporting");
                return reference;
            }
        } catch { return reference??""; } // Diagnostics never interrupt saving or error recovery.
    }
    private sealed class SanitizedDiagnosticException : Exception
    {
        private readonly string stack;
        public SanitizedDiagnosticException(string message,string stack):base(message){this.stack=stack;}
        public override string StackTrace=>stack;
    }
}
