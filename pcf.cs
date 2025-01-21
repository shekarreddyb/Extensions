using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

public class PCFAppDataFetcher
{
    public static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient();

        // Fetch the list of foundations
        var username = "your_username"; // Replace with your username
        var password = "your_password"; // Replace with your password
        var foundationApiUrl = "https://example.com/foundations"; // Replace with the actual URL
        var foundations = await FetchFoundationsAsync(httpClient, foundationApiUrl, username, password);

        var appData = new List<AppInfo>();

        // Process each foundation in parallel
        await Parallel.ForEachAsync(foundations, async (foundation, _) =>
        {
            var tokenUrl = $"https://{foundation}/oauth/token"; // Form the token URL
            var foundationApiUrl = $"https://{foundation}/api"; // Form the foundation API base URL
            var token = await FetchTokenAsync(httpClient, tokenUrl, username, password);

            var foundationApps = await FetchFoundationApps(httpClient, foundationApiUrl, token);
            lock (appData)
            {
                appData.AddRange(foundationApps);
            }
        });

        SaveToCsv(appData, "output.csv");
    }

    private static async Task<List<string>> FetchFoundationsAsync(HttpClient httpClient, string url, string username, string password)
    {
        // Set Basic Authentication header
        var authHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        // Make the request
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Deserialize the foundation names
        return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
    }

    private static async Task<string> FetchTokenAsync(HttpClient httpClient, string tokenUrl, string username, string password)
    {
        // Construct the request body for password flow
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", username },
            { "password", password }
        });

        // Set Authorization header for token request
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "Y2Y6");

        // Make the token API call
        var response = await httpClient.PostAsync(tokenUrl, tokenRequest);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        return tokenResponse?.AccessToken ?? throw new Exception("Failed to fetch token.");
    }

    private static async Task<List<AppInfo>> FetchFoundationApps(HttpClient httpClient, string foundationUrl, string token)
    {
        var appsData = new List<AppInfo>();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgs = await GetAllPaginatedAsync<Org>(httpClient, foundationUrl + "/v3/organizations");

        foreach (var org in orgs)
        {
            var spaces = await GetAllPaginatedAsync<Space>(httpClient, foundationUrl + $"/v3/organizations/{org.Guid}/spaces");

            foreach (var space in spaces)
            {
                var apps = await GetAllPaginatedAsync<App>(httpClient, foundationUrl + $"/v3/spaces/{space.Guid}/apps");

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

    private static async Task<List<T>> GetAllPaginatedAsync<T>(HttpClient httpClient, string baseUrl)
    {
        var results = new List<T>();
        var url = baseUrl;

        do
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var page = JsonSerializer.Deserialize<PaginatedResponse<T>>(content);

            if (page?.Resources != null)
            {
                results.AddRange(page.Resources);
            }

            // Update the URL to the next page
            url = page?.Pagination?.Next;
        }
        while (!string.IsNullOrEmpty(url));

        return results;
    }

    private static void SaveToCsv(IEnumerable<AppInfo> data, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteRecords(data);
    }

    // Models
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }

    public class Org
    {
        public string Guid { get; set; }   // Unique identifier for the organization
        public string Name { get; set; }   // Name of the organization
    }

    public class Space
    {
        public string Guid { get; set; }   // Unique identifier for the space
        public string Name { get; set; }   // Name of the space
    }

    public class App
    {
        public string Name { get; set; }            // Name of the application
        public int Instances { get; set; }         // Number of instances of the app
        public int Memory { get; set; }            // Memory quota per instance (in MB or GB)
        public int DiskQuota { get; set; }         // Disk quota per instance (in MB or GB)
    }

    public class AppInfo
    {
        public string OrgName { get; set; }          // Organization name
        public string SpaceName { get; set; }        // Space name
        public string AppName { get; set; }          // Application name
        public int InstanceCount { get; set; }       // Number of instances
        public int InstanceMemoryQuota { get; set; } // Memory quota per instance (in MB or GB)
        public int InstanceDiskQuota { get; set; }   // Disk quota per instance (in MB or GB)
        public int AppMemoryQuota { get; set; }      // Total memory quota for the app (InstanceMemoryQuota * InstanceCount)
        public int AppDiskQuota { get; set; }        // Total disk quota for the app (InstanceDiskQuota * InstanceCount)
    }

    public class PaginatedResponse<T>
    {
        public List<T> Resources { get; set; } // List of items in the current page
        public Pagination Pagination { get; set; }
    }

    public class Pagination
    {
        public string Next { get; set; } // URL for the next page
    }
}