using RecipeApp.Services;
using Xunit;
public class DiagnosticPolicyTests
{
    [Fact] public void UntrustedMetadataNeverBecomesReportFields() {
        const string sensitive="email@example.com token=secret caption=private";
        Assert.Equal("UNEXPECTED",DiagnosticPolicy.Code(sensitive));Assert.Equal("NONE",DiagnosticPolicy.Reason(sensitive));
        Assert.Equal("foodFeed.unknown",DiagnosticPolicy.Operation("foodFeed",sensitive));Assert.Equal("app.unknown",DiagnosticPolicy.LocalOperation(sensitive));
        Assert.True(Guid.TryParseExact(DiagnosticPolicy.Reference(sensitive),"N",out _));
    }
    [Fact] public void ReferencesCorrelateWithServerAndExpectedRejectionsAreNotServiceErrors() {
        var reference=Guid.NewGuid().ToString("N");Assert.Equal(reference,DiagnosticPolicy.Reference(reference));
        Assert.False(DiagnosticPolicy.Operational("PERMISSION_DENIED"));Assert.False(DiagnosticPolicy.Operational("INVALID_ARGUMENT"));
        Assert.True(DiagnosticPolicy.Operational("UNAVAILABLE"));Assert.Equal("foodFeed.publish",DiagnosticPolicy.Operation("foodFeed","publish"));
        Assert.Contains("edit",DiagnosticPolicy.Message("SAFETY_REJECTED","PERMISSION_DENIED","fallback"));
        Assert.Contains("retry",DiagnosticPolicy.Message("SAFETY_TIMEOUT","UNAVAILABLE","fallback"));
        Assert.Equal("Helpful validation",DiagnosticPolicy.Message("REQUEST_REJECTED","INVALID_ARGUMENT","Helpful validation"));
    }
}
