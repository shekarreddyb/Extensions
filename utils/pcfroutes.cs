using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ClosedXML.Excel;

public class FoundationConfig
{
    public List<Foundation> Foundations { get; set; }
}

public class Foundation
{
    public string Name { get; set; }
    public string Api { get; set; }
    public bool Active { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    public string UaaUrl => Api.Replace("api.", "uaa.") + "/oauth/token";
}

public static class YamlReader
{
    public static FoundationConfig Load(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<FoundationConfig>(yaml);
    }
}

public static class TokenFetcher
{
    public static async Task<string> GetAccessTokenAsync(string uaaUrl, string clientId, string clientSecret)
    {
        using var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post, uaaUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))
        );

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "response_type", "token" }
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        return json["access_token"]?.ToString() ?? throw new Exception("Token fetch failed");
    }
}

public class PcfApiClient
{
    private readonly HttpClient _client;

    public PcfApiClient(string baseUrl, string token)
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<(string AppName, string OrgName, string Route)>> GetAppDetailsAsync()
    {
        var appResources = await GetPaginatedResourcesAsync("/v3/apps");
        var results = new List<(string, string, string)>();

        foreach (var app in appResources)
        {
            var appName = app["name"]?.ToString();
            var appGuid = app["guid"]?.ToString();
            if (string.IsNullOrEmpty(appGuid)) continue;

            var spaceGuid = app["relationships"]?["space"]?["data"]?["guid"]?.ToString();
            if (string.IsNullOrEmpty(spaceGuid)) continue;

            var spaceResp = await _client.GetAsync($"/v3/spaces/{spaceGuid}");
            var space = JObject.Parse(await spaceResp.Content.ReadAsStringAsync());

            var orgGuid = space["relationships"]?["organization"]?["data"]?["guid"]?.ToString();
            if (string.IsNullOrEmpty(orgGuid)) continue;

            var orgResp = await _client.GetAsync($"/v3/organizations/{orgGuid}");
            var org = JObject.Parse(await orgResp.Content.ReadAsStringAsync());
            var orgName = org["name"]?.ToString() ?? "Unknown";

            var routeResources = await GetPaginatedResourcesAsync($"/v3/apps/{appGuid}/routes");
            var routeHostnames = routeResources.Select(r => r["url"]?.ToString() ?? "Unknown");

            foreach (var route in routeHostnames)
            {
                results.Add((appName, orgName, route));
            }
        }

        return results;
    }

    private async Task<List<JToken>> GetPaginatedResourcesAsync(string endpoint)
    {
        var results = new List<JToken>();
        string? nextUrl = endpoint;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var resp = await _client.GetAsync(nextUrl);
            resp.EnsureSuccessStatusCode();
            var json = JObject.Parse(await resp.Content.ReadAsStringAsync());

            results.AddRange(json["resources"]?.ToList() ?? new List<JToken>());

            nextUrl = json["pagination"]?["next"]?["href"]?.ToString();

            if (!string.IsNullOrEmpty(nextUrl) && nextUrl.StartsWith(_client.BaseAddress.ToString()))
                nextUrl = nextUrl.Replace(_client.BaseAddress.ToString(), "");
        }

        return results;
    }
}

public static class ExcelExporter
{
    public static void Export(string path, List<(string Foundation, string App, string Org, string Route)> data)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("CF Apps");
        sheet.Cell(1, 1).Value = "Foundation";
        sheet.Cell(1, 2).Value = "App";
        sheet.Cell(1, 3).Value = "Org";
        sheet.Cell(1, 4).Value = "Route";

        for (int i = 0; i < data.Count; i++)
        {
            var (foundation, app, org, route) = data[i];
            sheet.Cell(i + 2, 1).Value = foundation;
            sheet.Cell(i + 2, 2).Value = app;
            sheet.Cell(i + 2, 3).Value = org;
            sheet.Cell(i + 2, 4).Value = route;
        }

        workbook.SaveAs(path);
    }
}

class Program
{
    static async Task Main()
    {
        var config = YamlReader.Load("foundations.yml");
        var allData = new List<(string Foundation, string App, string Org, string Route)>();

        foreach (var foundation in config.Foundations.Where(f => f.Active))
        {
            try
            {
                Console.WriteLine($"Authenticating with {foundation.Name}...");
                var token = await TokenFetcher.GetAccessTokenAsync(foundation.UaaUrl, foundation.ClientId, foundation.ClientSecret);

                Console.WriteLine($"Querying apps in {foundation.Name}...");
                var client = new PcfApiClient(foundation.Api, token);
                var apps = await client.GetAppDetailsAsync();

                foreach (var (app, org, route) in apps)
                    allData.Add((foundation.Name, app, org, route));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {foundation.Name}: {ex.Message}");
            }
        }

        ExcelExporter.Export("cf_apps.xlsx", allData);
        Console.WriteLine("Excel export complete.");
    }
}