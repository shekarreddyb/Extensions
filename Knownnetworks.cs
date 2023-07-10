var host = Dns.GetHostEntry("your-domain.com");
var knownProxies = new List<IPAddress>();
foreach (var ip in host.AddressList)
{
    knownProxies.Add(ip);
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownProxies = knownProxies
});
