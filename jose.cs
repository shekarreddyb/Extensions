using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Jose;
using System.Linq;

public void Configuration(IAppBuilder app)
{
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AuthenticationType = "Cookies"
    });

    app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
    {
        ClientId = "your-client-id",
        Authority = "https://your-identityserver-url",
        RedirectUri = "https://your-redirect-uri",
        ResponseType = "code id_token",
        Scope = "openid profile",
        SignInAsAuthenticationType = "Cookies",
        
        TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://your-identityserver-url",
            ValidateIssuerSigningKey = true,
        },

        Notifications = new OpenIdConnectAuthenticationNotifications
        {
            SecurityTokenValidated = notification =>
            {
                var jwtToken = notification.ProtocolMessage.IdToken;
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadToken(jwtToken) as JwtSecurityToken;

                if (token != null)
                {
                    ValidateTokenWithJose(jwtToken, token, notification.Options.TokenValidationParameters.IssuerSigningKeys);
                }

                return Task.CompletedTask;
            },
            AuthenticationFailed = notification =>
            {
                // Handle authentication failures here
                return Task.CompletedTask;
            }
        }
    });
}

private void ValidateTokenWithJose(string jwtToken, JwtSecurityToken token, IEnumerable<SecurityKey> issuerSigningKeys)
{
    var key = issuerSigningKeys.OfType<RsaSecurityKey>().FirstOrDefault(k => k.KeyId == token.Header.Kid);
    if (key == null)
    {
        throw new SecurityTokenValidationException("Invalid token key identifier.");
    }

    var rsa = key.Rsa;
    if (rsa == null)
    {
        throw new SecurityTokenValidationException("RSA key is missing.");
    }

    var publicKey = new RsaKey
    {
        Key = rsa,
        Use = JwkUse.Sig,
        Alg = key.Algorithm
    };

    try
    {
        var payload = JWT.Decode(jwtToken, publicKey);
        // Token is valid, and you can access the payload
    }
    catch (IntegrityException)
    {
        throw new SecurityTokenValidationException("Invalid token signature.");
    }
    catch (Exception ex)
    {
        // Handle other exceptions
        throw new SecurityTokenValidationException("Token validation failed.", ex);
    }
}