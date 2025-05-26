using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public string Token { get; set; }
    public bool Active { get; set; }
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
            var content = JObject.Parse(await resp.Content.ReadAsStringAsync());

            var pageResources = content["resources"]?.ToList() ?? new List<JToken>();
            results.AddRange(pageResources);

            nextUrl = content["pagination"]?["next"]?["href"]?.ToString();

            // Convert to relative URL if full
            if (!string.IsNullOrEmpty(nextUrl) && nextUrl.StartsWith(_client.BaseAddress.ToString()))
            {
                nextUrl = nextUrl.Replace(_client.BaseAddress.ToString(), "");
            }
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
            var row = i + 2;
            var (foundation, app, org, route) = data[i];
            sheet.Cell(row, 1).Value = foundation;
            sheet.Cell(row, 2).Value = app;
            sheet.Cell(row, 3).Value = org;
            sheet.Cell(row, 4).Value = route;
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
            Console.WriteLine($"Querying foundation: {foundation.Name}");

            try
            {
                var client = new PcfApiClient(foundation.Api, foundation.Token);
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
        Console.WriteLine("Export complete.");
    }
}