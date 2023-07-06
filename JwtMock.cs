using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler : JwtBearerHandler
{
    public TestAuthHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim("sub", "testuser") }; // Customize the claims as needed
        var identity = new ClaimsIdentity(claims, "TestAuthScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuthScheme");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}


using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler : JwtBearerHandler
{
    public TestAuthHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim("sub", "testuser") }; // Customize the claims as needed
        var identity = new ClaimsIdentity(claims, "TestAuthScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuthScheme");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}



public class YourIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public YourIntegrationTests()
    {
        var webHostBuilder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddScheme<JwtBearerOptions, TestAuthHandler>("TestAuthScheme", options => { });
            });

        _server = new TestServer(webHostBuilder);
        _client = _server.CreateClient();
    }

    // Implement your tests here

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }
}




