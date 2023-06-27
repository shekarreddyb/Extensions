using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using ReactBFF.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactBFF.Security
{
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private const string FirstTicketIssuedTicks = nameof(FirstTicketIssuedTicks);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenIdConnectOptions _openIdOptions;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;
        private TimeSpan maxTimeToEllapse = TimeSpan.FromMinutes(3);

        public CustomCookieAuthenticationEvents(IHttpClientFactory httpClientFactory,
            IOptionsMonitor<OpenIdConnectOptions> openIdOptions,
            ISystemClock clock,
            ILogger<CustomCookieAuthenticationEvents> logger)
        {
            _httpClientFactory = httpClientFactory;
            // make sure this Get(->name<-) matches the scheme mentioned in AddOpenIdConnect(->name<-)
            _openIdOptions = openIdOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
            _clock = clock;
            _logger = logger;
        }

        /// <summary>
        /// Adds FirstTicketIssuedTicks to Authentication ticket properties when first coookie is generated after SignIn
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task SigningIn(CookieSigningInContext context)
        {
            context.Properties.SetString(
                FirstTicketIssuedTicks,
                _clock.UtcNow.Ticks.ToString());

            await base.SigningIn(context);
        }

        /// <summary>
        /// checks if the token has initial issued time set in SingningIn method above
        /// verifies if the maxTime has reached, if yes, rejects pricipal.
        /// when the request queryparam has renew=true:
        ///   verifies if the access_token inside cookie is about expire, if about to expire,
        /// this function makes use of refresh token to get new token and makes ShouldRenew = true.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ValidatePrincipal(
            CookieValidatePrincipalContext context)
        {
            _logger.LogDebug("Validating Principal");
            var ticketIssuedTicksValue = context
                .Properties.GetString(FirstTicketIssuedTicks);

            if (ticketIssuedTicksValue is null ||
                !long.TryParse(ticketIssuedTicksValue, out var ticketIssuedTicks))
            {
                _logger.LogDebug("Ticket issued time is blank. treating the cookie as invalid");
                await RejectPrincipalAsync(context);
                return;
            }

            var ticketIssuedUtc =
                new DateTimeOffset(ticketIssuedTicks, TimeSpan.FromHours(0));
            // max cookie lifetime reached
            if (_clock.UtcNow.Subtract(ticketIssuedUtc) > maxTimeToEllapse)
            {
                _logger.LogDebug("Ticket issued time has exceeded max lifetime. Rejecting Principal");
                await RejectPrincipalAsync(context);
                return;
            }

            var currentUtc = _clock.UtcNow;
            var issuedUtc = context.Properties.IssuedUtc;
            var expiresUtc = context.Properties.ExpiresUtc;
            var refreshTokenExpiry = context.Properties.GetTokenValue("refresh_token_expires");
            var renewQueryParam = context.Request.Query["renew"].ToString();
            bool.TryParse(renewQueryParam, out bool renew);

            bool shouldAttemptRenew = renew
                                        && expiresUtc is not null
                                        && issuedUtc is not null
                                        && refreshTokenExpiry is not null;

            if (shouldAttemptRenew)
            {
                var timeLeftBeforeExpiry = expiresUtc.Value.Subtract(currentUtc);
                var refreshTokenExpiresUtc = DateTimeOffset.Parse(refreshTokenExpiry);

                shouldAttemptRenew = timeLeftBeforeExpiry >= TimeSpan.FromMinutes(5)
                    && currentUtc <= refreshTokenExpiresUtc;
            }

            if (shouldAttemptRenew)
            {
                //attempt renewal only if renew=true param  exists and  time limit reached
                await RenewIfAccessTokenIsAboutToExpire(context);
            }

            await base.ValidatePrincipal(context);
        }

        /// <summary>
        /// This event gets fired after above "ValidatePrincipal" called. we can have this code in above method technically,
        /// But I decided to keep it here as this feels like the right place to decide context.ShouldRenew value based on QueryParam "noslide" value.
        /// because it got introduced for this purpose
        /// https://github.com/dotnet/aspnetcore/pull/33016
        /// https://github.com/dotnet/aspnetcore/blob/v7.0.5/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs#L96
        /// Warning: if the both renew query param and noslide are set to true, the new cookie issues with updated token won't be created.
        /// Avoid adding noslide to api call that attempts renewal
        /// </summary>
        public new Func<CookieSlidingExpirationContext, Task> OnCheckSlidingExpiration { get; set; } = context =>
        {
            var noSlideParam = context.Request.Query["noslide"].ToString();
            bool.TryParse(noSlideParam, out bool noSlide);

            if (noSlide)
            {
                context.ShouldRenew = false;
            }
            return Task.CompletedTask;
        };

        /// <summary>
        /// reject pricipal and remove cookie.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task RejectPrincipalAsync(
            CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(_openIdOptions.SignOutScheme ?? _openIdOptions.SignInScheme);
        }

        private async Task RenewIfAccessTokenIsAboutToExpire(CookieValidatePrincipalContext context)
        {
            var refreshToken = context.Properties.GetTokenValue("refresh_token");

            if (refreshToken is null) return;
            // Use the refresh token to get a new access token, then update the tokens in the authentication properties.
            var newTokens = await RenewTokensUsingRefreshToken(refreshToken);
            if (newTokens is null || newTokens?.AccessToken is null)
            {
                // Refresh token expired, revoke the cookie and force the user to log in again
                await RejectPrincipalAsync(context);
                return;
            }

            context.Properties.UpdateTokenValue("access_token", newTokens?.AccessToken);
            // context.Properties.UpdateTokenValue("id_token", newTokens?.AccessToken);
            context.Properties.UpdateTokenValue("refresh_token", newTokens?.RefreshToken);
            context.Properties.UpdateTokenValue("refresh_token_expires", newTokens.RefreshTokenExpiresAt?.ToString("o"));
            // Update the cookie with the new tokens
            context.ShouldRenew = true;
        }

        private async Task<TokenResponse?> RenewTokensUsingRefreshToken(string refreshToken)
        {
            // Call your token server with the refresh token and parse the response to get new access and refresh tokens
            // Return the new tokens and refresh token expiration as a tuple
            var httpClient = _httpClientFactory.CreateClient("token_client");

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token");
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", "interactivewebclient" },  //TODO
               // { "client_secret", "your_client_secret" },
                { "refresh_token", refreshToken },
                // Add any other required parameters for your token server
            });
            var response = await httpClient.SendAsync(tokenRequest);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return tokenResponse;
            }
            else
            {
                return null;
            }
        }

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; } // not using

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("refresh_token_expires_in")] // optional response based on id provider
            public int RefreshTokenExpiresIn { get; set; } // not using

            public DateTimeOffset? AccessTokenExpiresAt => AccessToken.GetExpiryDateFromToken();
            public DateTimeOffset? RefreshTokenExpiresAt => RefreshToken.GetExpiryDateFromToken();

            //[JsonPropertyName("id_token")]
            //public string? IdToken { get; set; }
            //[JsonPropertyName("scope")]
            //public string? Scope { get; set; }
            //[JsonPropertyName("token_type")]
            //public string? TokenType { get; set; }
        }
    }
}
