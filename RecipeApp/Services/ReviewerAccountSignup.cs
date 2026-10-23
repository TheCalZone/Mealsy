using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace RecipeApp.Services;

public record CreatedReviewerAccount(string Uid,string Email,bool VerificationEmailSent=false);

/// <summary>Normal Firebase sign-up over a separate HTTP request. Never installs an Auth session.</summary>
public static class ReviewerAccountSignup
{
    public static async Task<CreatedReviewerAccount> Create(HttpClient client,string apiKey,string email,string password)
    {
        email=AccountInput.Email(email);
        if(string.IsNullOrWhiteSpace(apiKey))throw new InvalidOperationException("Firebase configuration is unavailable. Reopen Mealsy and try again.");
        if(password==null||password.Length<12||password.Length>128)throw new InvalidOperationException("Use a password from 12–128 characters.");
        HttpResponseMessage response;
        try
        {
            response=await client.PostAsJsonAsync("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key="+Uri.EscapeDataString(apiKey),new {email,password,returnSecureToken=true});
        }
        catch(Exception e) when(e is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("Account creation could not be confirmed. It may have succeeded. Use Find account by email before retrying; no Premium grant was confirmed.");
        }
        using(response)
        {
            JsonDocument body;
            try{body=JsonDocument.Parse(await response.Content.ReadAsStringAsync());}
            catch(JsonException){throw new InvalidOperationException("The account service returned an unexpected reply. Check Find account by email before retrying.");}
            using(body)
            {
                if(body.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidOperationException("The account service returned an unexpected reply. Check Find account by email before retrying.");
                if(!response.IsSuccessStatusCode)
                {
                    var code=body.RootElement.TryGetProperty("error",out var error)&&error.ValueKind==JsonValueKind.Object&&error.TryGetProperty("message",out var message)?message.GetString()??"":"";
                    throw new InvalidOperationException(code switch {
                        "EMAIL_EXISTS"=>"An account already uses this email. Nothing was changed on that account. Use Find account / manage Premium instead.",
                        "INVALID_EMAIL"=>"Check the email address and try again.",
                        "OPERATION_NOT_ALLOWED"=>"Email/password sign-up is not enabled for this Firebase project.",
                        "TOO_MANY_ATTEMPTS_TRY_LATER"=>"Too many sign-up attempts. Wait before trying again.",
                        _ when code.StartsWith("WEAK_PASSWORD",StringComparison.Ordinal)=>"Firebase did not accept this password. Choose a stronger password that meets your project's password policy.",
                        _=>"Firebase could not create the account. Project API restrictions or sign-up protection may require configuration. Check Find account by email before retrying. No existing password was changed."
                    });
                }
                if(!body.RootElement.TryGetProperty("localId",out var id)||id.ValueKind!=JsonValueKind.String||!Regex.IsMatch(id.GetString()??"","^[A-Za-z0-9_-]{1,128}$"))
                    throw new InvalidOperationException("Account creation returned no valid account reference. Check Find account by email before retrying.");
                var sent=false;
                if(body.RootElement.TryGetProperty("idToken",out var token)&&token.ValueKind==JsonValueKind.String&&!string.IsNullOrWhiteSpace(token.GetString()))
                {
                    try
                    {
                        using var verification=await client.PostAsJsonAsync("https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key="+Uri.EscapeDataString(apiKey),new {requestType="VERIFY_EMAIL",idToken=token.GetString()});
                        sent=verification.IsSuccessStatusCode;
                    }
                    catch(Exception e) when(e is HttpRequestException or TaskCanceledException) { }
                }
                return new(id.GetString(),email,sent);
            }
        }
    }
}
