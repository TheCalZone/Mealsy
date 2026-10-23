namespace RecipeApp.Services;

public static class DiagnosticPolicy
{
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal) { "UNEXPECTED","INTERNAL","UNAVAILABLE","DEADLINE_EXCEEDED","UNKNOWN","NETWORK","TIMEOUT","INVALID_RESPONSE","AUTH_SERVICE_FAILURE","SYNC_FAILURE","TEST","SDK_STARTUP" };
    private static readonly HashSet<string> Reasons = new(StringComparer.Ordinal) { "SAFETY_REJECTED","SAFETY_BUSY","SAFETY_TIMEOUT","SAFETY_UNAVAILABLE","SAFETY_RESPONSE_INVALID","SERVICE_FAILURE","REQUEST_REJECTED","NONE" };
    private static readonly HashSet<string> Actions = new(StringComparer.Ordinal) { "prepare","request","setRole","requests","resolveRequest","cancelRequest","requestMeal","capabilities","readWeek","upsertMeal","deleteMeal","getPost","profile","setUsername","publish","comment","list","preferences","savePreferences","follow","unfollow","comments","delete","moderate","resolveReport","like","deleteComment","report","status","lookupUser","posts","setPostHidden","pausePromotion","setFamilyGrant","verifyEmail","audit","requestPromotion","activatePromotion","create","join","invite","remove","leave","read","patch","revoke","verify","restore","mine","updateProfile","workspace","removeMember","revokeInvite","publishRecipe","hideRecipe","saveCollection","collection","directory","publicProfile","track","analytics","saveCampaign","pauseCampaign","adminBusinesses","verifyBusiness","wallet","verifyCredit","start","prepareImpression","impression","registerDevice","unregisterDevice" };
    public static string Code(string value) => Codes.Contains(value??"") ? value : "UNEXPECTED";
    public static string Reason(string value) => Reasons.Contains(value??"") ? value : "NONE";
    public static bool Operational(string code) => Codes.Contains(code??"");
    public static string Operation(string service,string action) => new[]{"accountDeletion","householdMeals","household","foodFeed","billing","administration","business","notifications","promotions"}.Contains(service) ? service+"."+(Actions.Contains(action??"")?action:"unknown") : "service.unknown";
    public static string LocalOperation(string value) => new[]{"account.sign-in","account.register","account.profile","account.sync","account.reset","sdk.startup","ui.action","feed.load","shopping.sync","recipe.save","diagnostics.test"}.Contains(value) ? value : "app.unknown";
    public static string Reference(string value) => Guid.TryParseExact(value,"N",out var id) ? id.ToString("N") : Guid.NewGuid().ToString("N");
    public static string Message(string reason,string code,string fallback) => Reason(reason) switch {
        "SAFETY_REJECTED" => "This submission did not pass the safety check. Review the text and photo, then edit and retry. Your draft is kept.",
        "SAFETY_BUSY" => "Safety checks are busy. Your draft is kept. Wait a moment, then retry.",
        "SAFETY_TIMEOUT" => "The safety check took too long. Your draft is kept. Please retry.",
        "SAFETY_UNAVAILABLE" or "SAFETY_RESPONSE_INVALID" => "Safety checks are temporarily unavailable. Your draft is kept. Please retry later.",
        _ => code switch {
            "NETWORK" => "Couldn't connect to the service. Check your connection and retry.",
            "TIMEOUT" or "DEADLINE_EXCEEDED" => "The request took too long. Please retry; pending changes are kept.",
            "INVALID_RESPONSE" => "The service returned an unreadable response. Please retry; pending changes are kept.",
            "INTERNAL" or "UNKNOWN" or "UNAVAILABLE" => "The service is temporarily unavailable. Please retry; pending changes are kept.",
            _ => fallback
        }
    };
}
