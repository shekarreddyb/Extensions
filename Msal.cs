using Microsoft.Identity.Client;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class TokenHandler : DelegatingHandler
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;
    
    public TokenHandler(IPublicClientApplication app, string[] scopes)
    {
        _app = app;
        _scopes = scopes;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            // Use MSAL to acquire token
            var accounts = await _app.GetAccountsAsync();
            var result = await _app.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                                    .ExecuteAsync();

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}





using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        string clientId = "<Your Client ID>";
        string redirectUri = "<Your Redirect Uri>";
        string[] scopes = new[] { "<Your Scopes>" };

        var publicClient = PublicClientApplicationBuilder.Create(clientId)
                                .WithRedirectUri(redirectUri)
                                .Build();

        services.AddSingleton<IPublicClientApplication>(publicClient);

        services.AddTransient(x => new TokenHandler(publicClient, scopes));

        services.AddHttpClient("MyClient")
                .AddHttpMessageHandler<TokenHandler>();
    }
}




