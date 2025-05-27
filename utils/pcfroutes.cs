using System; using System.Collections.Concurrent; using System.Collections.Generic; using System.IO; using System.Linq; using System.Net.Http; using System.Net.Http.Headers; using System.Text; using System.Threading.Tasks; using Newtonsoft.Json.Linq; using YamlDotNet.Serialization; using YamlDotNet.Serialization.NamingConventions;

public class FoundationConfig { public List<Foundation> Foundations { get; set; } }

public class Foundation { public string Name { get; set; } public string Api { get; set; } public bool Active { get; set; }

[YamlIgnore]
public string ClientId { get; set; }

[YamlIgnore]
public string ClientSecret { get; set; }

public string UaaUrl => Api.Replace("api.", "uaa.") + "/oauth/token";

}

public static class YamlReader { public static FoundationConfig Load(string path) { var yaml = File.ReadAllText(path); var deserializer = new DeserializerBuilder() .WithNamingConvention(CamelCaseNamingConvention.Instance) .Build(); return deserializer.Deserialize<FoundationConfig>(yaml); } }

public static class TokenFetcher { public static async Task<string> GetAccessTokenAsync(string uaaUrl, string clientId, string clientSecret) { using var client = new HttpClient(); var request = new HttpRequestMessage(HttpMethod.Post, uaaUrl); request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

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

public class PcfApiClient { private readonly HttpClient _client;

public PcfApiClient(string apiUrl, string token)
{
    _client = new HttpClient { BaseAddress = new Uri(apiUrl) };
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

public async Task<List<JToken>> GetPaginatedResourcesAsync(string endpoint)
{
    var results = new List<JToken>();
    string? nextUrl = endpoint;

    while (!string.IsNullOrEmpty(nextUrl))
    {
        var resp = await _client.GetAsync(nextUrl);
        resp.EnsureSuccessStatusCode();
        var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
        results.AddRange(json["resources"]?.ToList() ?? new List<JToken>());

        var nextToken = json["pagination"]?["next"]?["href"];
        if (nextToken == null || nextToken.Type == JTokenType.Null)
        {
            nextUrl = null;
        }
        else
        {
            nextUrl = nextToken.ToString();
            if (nextUrl.StartsWith(_client.BaseAddress.ToString()))
                nextUrl = nextUrl.Replace(_client.BaseAddress.ToString(), "");
        }
    }

    return results;
}

public async Task<List<(string Org, string App, string Route)>> FetchAllRelationshipsAsync()
{
    var orgsTask = GetPaginatedResourcesAsync("/v3/organizations");
    var spacesTask = GetPaginatedResourcesAsync("/v3/spaces");
    var appsTask = GetPaginatedResourcesAsync("/v3/apps");
    var routesTask = GetPaginatedResourcesAsync("/v3/routes");

    await Task.WhenAll(orgsTask, spacesTask, appsTask, routesTask);

    var orgs = orgsTask.Result;
    var spaces = spacesTask.Result;
    var apps = appsTask.Result;
    var routes = routesTask.Result;

    var orgMap = orgs.ToDictionary(o => o["guid"]!.ToString(), o => o["name"]!.ToString());
    var spaceToOrgMap = spaces.ToDictionary(
        s => s["guid"]!.ToString(),
        s => s["relationships"]?["organization"]?["data"]?["guid"]?.ToString() ?? ""
    );
    var appMap = apps.ToDictionary(
        a => a["guid"]!.ToString(),
        a => (a["name"]?.ToString() ?? "Unknown", a["relationships"]?["space"]?["data"]?["guid"]?.ToString())
    );

    var results = new ConcurrentBag<(string Org, string App, string Route)>();

    var tasks = routes.Select(route => Task.Run(() =>
    {
        var url = route["url"]?.ToString() ?? "Unknown";
        var appRef = route["relationships"]?["app"]?["data"]?["guid"]?.ToString();

        if (string.IsNullOrEmpty(appRef)) return;

        if (!appMap.TryGetValue(appRef, out var appInfo)) return;
        var (appName, spaceGuid) = appInfo;

        if (string.IsNullOrEmpty(spaceGuid)) return;
        if (!spaceToOrgMap.TryGetValue(spaceGuid, out var orgGuid)) return;

        var orgName = orgMap.GetValueOrDefault(orgGuid ?? "", "Unknown");
        results.Add((orgName, appName, url));
    }));

    await Task.WhenAll(tasks);
    return results.ToList();
}

}

public static class CsvExporter { public static void Export(string path, List<(string Foundation, string Org, string App, string Route)> data) { using var writer = new StreamWriter(path); writer.WriteLine("Foundation,Org,App Name,Route");

foreach (var (foundation, org, app, route) in data)
    {
        var line = $"{Escape(foundation)},{Escape(org)},{Escape(app)},{Escape(route)}";
        writer.WriteLine(line);
    }
}

private static string Escape(string input)
{
    if (input.Contains(",") || input.Contains("\"") || input.Contains("\n"))
        return $"\"{input.Replace("\"", "\"\"")}\"";
    return input;
}

}

class Program { static async Task Main(string[] args) { if (args.Length < 2) { Console.WriteLine("Usage: app.exe <clientId> <clientSecret>"); return; }

string globalClientId = args[0];
    string globalClientSecret = args[1];

    var config = YamlReader.Load("foundations.yml");
    var results = new ConcurrentBag<(string Foundation, string Org, string App, string Route)>();

    var tasks = config.Foundations.Where(f => f.Active).Select(async foundation =>
    {
        try
        {
            foundation.ClientId = globalClientId;
            foundation.ClientSecret = globalClientSecret;

            Console.WriteLine($"[{foundation.Name}] Authenticating...");
            var token = await TokenFetcher.GetAccessTokenAsync(foundation.UaaUrl, foundation.ClientId, foundation.ClientSecret);
            var client = new PcfApiClient(foundation.Api, token);
            var apps = await client.FetchAllRelationshipsAsync();

            foreach (var (org, app, route) in apps)
                results.Add((foundation.Name, org, app, route));

            Console.WriteLine($"[{foundation.Name}] Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] {foundation.Name} error: {ex.Message}");
        }
    });

    await Task.WhenAll(tasks);
    CsvExporter.Export("cf_apps.csv", results.ToList());
    Console.WriteLine("CSV export completed.");
}

}

