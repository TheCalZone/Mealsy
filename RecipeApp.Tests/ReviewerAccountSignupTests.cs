using System.Net;
using System.Text;
using System.Text.Json;
using RecipeApp.Services;
using Xunit;

public class ReviewerAccountSignupTests
{
    private class Handler:HttpMessageHandler
    {
        public Func<HttpRequestMessage,Task<HttpResponseMessage>> Reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Reply(request);
    }
    private static HttpResponseMessage Json(string text,HttpStatusCode code=HttpStatusCode.OK)=>new(code){Content=new StringContent(text,Encoding.UTF8,"application/json")};
    [Fact]
    public async Task SignupUsesSeparateRequestAndVerificationTokenNeverEscapesReturnedResult()
    {
        var count=0;var handler=new Handler {Reply=async request=>{
            Assert.Null(request.Headers.Authorization);
            Assert.Equal("identitytoolkit.googleapis.com",request.RequestUri.Host);
            using var payload=JsonDocument.Parse(await request.Content.ReadAsStringAsync());
            if(++count==1){Assert.EndsWith("/accounts:signUp",request.RequestUri.AbsolutePath);Assert.Equal("reviewer@example.com",payload.RootElement.GetProperty("email").GetString());return Json("{\"localId\":\"reviewer123\",\"idToken\":\"test-token\",\"refreshToken\":\"unused-refresh\"}");}
            Assert.EndsWith("/accounts:sendOobCode",request.RequestUri.AbsolutePath);Assert.Equal("VERIFY_EMAIL",payload.RootElement.GetProperty("requestType").GetString());Assert.Equal("test-token",payload.RootElement.GetProperty("idToken").GetString());return Json("{}");
        }};
        using var client=new HttpClient(handler);
        var result=await ReviewerAccountSignup.Create(client,"test-public-key","reviewer@example.com","Test!Password123");
        Assert.Equal("reviewer123",result.Uid);Assert.True(result.VerificationEmailSent);Assert.Equal(2,count);
        var serialized=JsonSerializer.Serialize(result);Assert.DoesNotContain("test-token",serialized);Assert.DoesNotContain("Password",serialized);Assert.DoesNotContain("unused-refresh",serialized);
    }
    [Fact]
    public async Task ExistingAccountIsNotResetAndNoVerificationIsSent()
    {
        var count=0;using var client=new HttpClient(new Handler {Reply=_=>{count++;return Task.FromResult(Json("{\"error\":{\"message\":\"EMAIL_EXISTS\"}}",HttpStatusCode.BadRequest));}});
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>ReviewerAccountSignup.Create(client,"key","reviewer@example.com","Test!Password123"));
        Assert.Contains("Nothing was changed",error.Message);Assert.Equal(1,count);
    }
    [Fact]
    public async Task FailedVerificationDoesNotHideSuccessfulAccountCreation()
    {
        var count=0;using var client=new HttpClient(new Handler {Reply=_=>Task.FromResult(++count==1?Json("{\"localId\":\"reviewer123\",\"idToken\":\"token\"}"):Json("{}",HttpStatusCode.TooManyRequests))});
        var result=await ReviewerAccountSignup.Create(client,"key","reviewer@example.com","Test!Password123");Assert.Equal("reviewer123",result.Uid);Assert.False(result.VerificationEmailSent);
    }
    [Fact]
    public async Task AmbiguousTimeoutExplainsRecoveryWithoutRetryingOrExposingCredentials()
    {
        var count=0;using var client=new HttpClient(new Handler {Reply=_=>{count++;throw new TaskCanceledException();}});
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>ReviewerAccountSignup.Create(client,"key","reviewer@example.com","Test!Password123"));
        Assert.Contains("may have succeeded",error.Message);Assert.DoesNotContain("Password123",error.Message);Assert.Equal(1,count);
    }
    [Fact]
    public async Task InvalidInputsFailBeforeContactingFirebase()
    {
        using var client=new HttpClient(new Handler {Reply=_=>throw new Exception("Unexpected network request")});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>ReviewerAccountSignup.Create(client,"key","not-an-email","Test!Password123"));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>ReviewerAccountSignup.Create(client,"key","reviewer@example.com","short"));
    }
}
