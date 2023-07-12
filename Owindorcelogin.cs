public void Configuration(IAppBuilder app)
{
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
        LoginPath = new PathString("/Account/Login") // change this to your login path
    });

    app.Use(async (context, next) =>
    {
        if (context.Authentication.User?.Identity == null || !context.Authentication.User.Identity.IsAuthenticated)
        {
            context.Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = context.Request.Path.ToString() },
                OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }
        else
        {
            await next.Invoke();
        }
    });

    // Setup your OpenID Connect options
    var oidcOptions = new OpenIdConnectAuthenticationOptions
    {
        // Set your options here
    };

    // Then add the middleware
    app.UseOpenIdConnectAuthentication(oidcOptions);
}
