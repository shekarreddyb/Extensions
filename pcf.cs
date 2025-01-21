public class PCFAppDataFetcher
{
    public static async Task Main(string[] args)
    {
        var foundations = new List<string> { "foundation1_url", "foundation2_url" }; // Replace with your foundation URLs
        using var httpClient = new HttpClient(); // Create the HttpClient instance here.
        
        var token = await FetchTokenAsync(httpClient, "auth_url", "client_id", "client_secret"); // Replace with your auth details

        var appData = new List<AppInfo>();

        await Parallel.ForEachAsync(foundations, async (foundation, _) =>
        {
            var foundationApps = await FetchFoundationApps(httpClient, foundation, token);
            lock (appData)
            {
                appData.AddRange(foundationApps);
            }
        });

        SaveToCsv(appData, "output.csv");
    }

    private static async Task<string> FetchTokenAsync(HttpClient httpClient, string authUrl, string clientId, string clientSecret)
    {
        var tokenRequest = new
        {
            grant_type = "client_credentials",
            client_id = clientId,
            client_secret = clientSecret
        };

        var response = await httpClient.PostAsync(authUrl, new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        return tokenResponse?.AccessToken ?? throw new Exception("Failed to fetch token.");
    }

    private static async Task<List<AppInfo>> FetchFoundationApps(HttpClient httpClient, string foundationUrl, string token)
    {
        var appsData = new List<AppInfo>();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgs = await GetAsync<List<Org>>(httpClient, foundationUrl + "/v3/organizations");

        foreach (var org in orgs)
        {
            var spaces = await GetAsync<List<Space>>(httpClient, foundationUrl + $"/v3/organizations/{org.Guid}/spaces");

            foreach (var space in spaces)
            {
                var apps = await GetAsync<List<App>>(httpClient, foundationUrl + $"/v3/spaces/{space.Guid}/apps");

                foreach (var app in apps)
                {
                    appsData.Add(new AppInfo
                    {
                        OrgName = org.Name,
                        SpaceName = space.Name,
                        AppName = app.Name,
                        InstanceCount = app.Instances,
                        InstanceMemoryQuota = app.Memory,
                        InstanceDiskQuota = app.DiskQuota,
                        AppMemoryQuota = app.Memory * app.Instances,
                        AppDiskQuota = app.DiskQuota * app.Instances
                    });
                }
            }
        }

        return appsData;
    }

    private static async Task<T> GetAsync<T>(HttpClient httpClient, string url)
    {
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content) ?? throw new Exception($"Failed to fetch data from {url}");
    }

    private static void SaveToCsv(IEnumerable<AppInfo> data, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteRecords(data);
    }

    // Models remain the same
}