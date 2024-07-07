using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

public class CustomTokenValidator : ISecurityTokenValidator
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public CustomTokenValidator()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public bool CanReadToken(string securityToken)
    {
        return _tokenHandler.CanReadToken(securityToken);
    }

    public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
    {
        // Custom logic to validate the token using introspection endpoint
        var introspectionResult = IntrospectToken(securityToken, validationParameters).Result;
        if (!introspectionResult.IsActive)
        {
            throw new SecurityTokenValidationException("Token is inactive");
        }

        // Create claims principal based on the introspection result
        validatedToken = _tokenHandler.ReadToken(securityToken) as JwtSecurityToken;
        var claims = introspectionResult.Claims.Select(c => new Claim(c.Key, c.Value));
        var identity = new ClaimsIdentity(claims, "custom");
        return new ClaimsPrincipal(identity);
    }

    public bool CanValidateToken => true;

    public int MaximumTokenSizeInBytes
    {
        get => _tokenHandler.MaximumTokenSizeInBytes;
        set => _tokenHandler.MaximumTokenSizeInBytes = value;
    }

    private async Task<IntrospectionResponse> IntrospectToken(string token, TokenValidationParameters validationParameters)
    {
        var authority = validationParameters.ValidIssuer;
        var introspectionEndpoint = $"{authority}/connect/introspect";
        
        using (var httpClient = new HttpClient())
        {
            var clientCredentials = new Dictionary<string, string>
            {
                { "client_id", "your-client-id" },
                { "client_secret", "your-client-secret" },
                { "token", token }
            };

            var requestContent = new FormUrlEncodedContent(clientCredentials);
            var response = await httpClient.PostAsync(introspectionEndpoint, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new SecurityTokenValidationException("Introspection endpoint call failed");
            }

            var content = await response.Content.ReadAsStringAsync();
            var introspectionResponse = JObject.Parse(content);

            return new IntrospectionResponse
            {
                IsActive = introspectionResponse["active"].Value<bool>(),
                Claims = introspectionResponse.ToObject<Dictionary<string, string>>()
            };
        }
    }
}

public class IntrospectionResponse
{
    public bool IsActive { get; set; }
    public Dictionary<string, string> Claims { get; set; }
}