
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-auth-server"; // The URL of the authorization server
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    "https://your-jwks-url",
                    new OpenIdConnectConfigurationRetriever());
                var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);
                return openIdConfig.SigningKeys;
            }
        };
    });

