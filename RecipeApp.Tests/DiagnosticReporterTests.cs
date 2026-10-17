using RecipeApp.Services;
using System.Text.Json;
using Xunit;
public class DiagnosticReporterTests
{
    [Fact] public void ReportsAreSanitizedDeduplicatedAndDisabledWithoutBlockingRecovery() {
        var original=AppDiagnostics.Send;var previous=AppDiagnostics.Enabled;
        const string secret="password=secret token=private email@example.com recipe-text";
        var delivered=new List<(Exception Error,Dictionary<string,string> Keys)>();
        try {
            AppDiagnostics.Send=(error,keys)=>delivered.Add((error,keys));AppDiagnostics.SetEnabled(true);
            var failure=new InvalidOperationException(secret,new Exception(secret));
            var reference=AppDiagnostics.Report(failure,"account.profile","SYNC_FAILURE");
            Assert.Equal(reference,AppDiagnostics.Report(failure,"account.profile","SYNC_FAILURE"));Assert.Single(delivered);
            Assert.Null(delivered[0].Error.InnerException);Assert.DoesNotContain(secret,delivered[0].Error.ToString());
            Assert.DoesNotContain(secret,JsonSerializer.Serialize(delivered[0].Keys));Assert.Equal(reference,delivered[0].Keys["report_reference"]);
            Assert.Equal("Queued for reporting",AppDiagnostics.History().First(e=>e.Reference==reference).Status);
            AppDiagnostics.Report(new Exception(secret),"account.profile","SYNC_FAILURE");Assert.Single(delivered);
            AppDiagnostics.SetEnabled(false);var off=AppDiagnostics.Report(new Exception(secret),"recipe.save");Assert.Single(delivered);
            Assert.Contains(AppDiagnostics.History(),entry=>entry.Reference==off && entry.Status=="Recorded locally");Assert.DoesNotContain(secret,JsonSerializer.Serialize(AppDiagnostics.History()));
            AppDiagnostics.SetEnabled(true);AppDiagnostics.Send=(_,_)=>throw new Exception("Reporting unavailable");
            Assert.True(Guid.TryParseExact(AppDiagnostics.Report(new Exception(secret),"diagnostics.test","TEST"),"N",out _));
        } finally {AppDiagnostics.Send=original;AppDiagnostics.SetEnabled(previous);}
    }
}
