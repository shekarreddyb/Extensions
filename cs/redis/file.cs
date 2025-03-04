using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IRedisBdbService
{
    Task<BdbResponse?> CreateDatabaseAsync(string baseUri, RedisDatabaseRequest request);
    Task<BdbResponse?> GetDatabaseAsync(string baseUri, string databaseId);
    Task<BdbResponse?> UpdateDatabaseAsync(string baseUri, string databaseId, RedisDatabaseRequest request);
    Task<bool> DeleteDatabaseAsync(string baseUri, string databaseId);
    Task<TLSConfigResponse?> UpdateTLSConfigAsync(string baseUri, TLSConfigRequest request);
}

public interface ICrdbService
{
    Task<CrdbResponse?> CreateCrdbAsync(string baseUri, CrdbRequest request);
    Task<CrdbResponse?> GetCrdbAsync(string baseUri, string crdbId);
    Task<CrdbResponse?> UpdateCrdbAsync(string baseUri, string crdbId, CrdbRequest request);
    Task<bool> DeleteCrdbAsync(string baseUri, string crdbId);
}

public class RedisBdbService : IRedisBdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RedisBdbService> _logger;

    public RedisBdbService(HttpClient httpClient, ILogger<RedisBdbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BdbResponse?> CreateDatabaseAsync(string baseUri, RedisDatabaseRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{baseUri}/v1/bdbs", request);
        return await response.Content.ReadFromJsonAsync<BdbResponse>();
    }

    public async Task<BdbResponse?> GetDatabaseAsync(string baseUri, string databaseId)
    {
        var response = await _httpClient.GetAsync($"{baseUri}/v1/bdbs/{databaseId}");
        return await response.Content.ReadFromJsonAsync<BdbResponse>();
    }

    public async Task<BdbResponse?> UpdateDatabaseAsync(string baseUri, string databaseId, RedisDatabaseRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{baseUri}/v1/bdbs/{databaseId}", request);
        return await response.Content.ReadFromJsonAsync<BdbResponse>();
    }

    public async Task<bool> DeleteDatabaseAsync(string baseUri, string databaseId)
    {
        var response = await _httpClient.DeleteAsync($"{baseUri}/v1/bdbs/{databaseId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<TLSConfigResponse?> UpdateTLSConfigAsync(string baseUri, TLSConfigRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{baseUri}/v1/tls", request);
        return await response.Content.ReadFromJsonAsync<TLSConfigResponse>();
    }
}

public class CrdbService : ICrdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrdbService> _logger;

    public CrdbService(HttpClient httpClient, ILogger<CrdbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CrdbResponse?> CreateCrdbAsync(string baseUri, CrdbRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{baseUri}/v1/crdbs", request);
        return await response.Content.ReadFromJsonAsync<CrdbResponse>();
    }

    public async Task<CrdbResponse?> GetCrdbAsync(string baseUri, string crdbId)
    {
        var response = await _httpClient.GetAsync($"{baseUri}/v1/crdbs/{crdbId}");
        return await response.Content.ReadFromJsonAsync<CrdbResponse>();
    }

    public async Task<CrdbResponse?> UpdateCrdbAsync(string baseUri, string crdbId, CrdbRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{baseUri}/v1/crdbs/{crdbId}", request);
        return await response.Content.ReadFromJsonAsync<CrdbResponse>();
    }

    public async Task<bool> DeleteCrdbAsync(string baseUri, string crdbId)
    {
        var response = await _httpClient.DeleteAsync($"{baseUri}/v1/crdbs/{crdbId}");
        return response.IsSuccessStatusCode;
    }
}

public class RedisDatabaseRequest
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("memory_size")] public int MemorySize { get; set; }
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("replication")] public bool Replication { get; set; }
    [JsonPropertyName("aof_persistence")] public bool AofPersistence { get; set; }
}

public class CrdbRequest
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("regions")] public List<string> Regions { get; set; }
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("replication")] public bool Replication { get; set; }
}

public class BdbResponse
{
    [JsonPropertyName("uid")] public string Uid { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("memory_size")] public int MemorySize { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("replication")] public bool Replication { get; set; }
    [JsonPropertyName("sharding")] public bool Sharding { get; set; }
    [JsonPropertyName("tls_enabled")] public bool TlsEnabled { get; set; }
    [JsonPropertyName("modules")] public List<string> Modules { get; set; }
}

public class CrdbResponse
{
    [JsonPropertyName("crdb_id")] public string CrdbId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("regions")] public List<string> Regions { get; set; }
    [JsonPropertyName("tls_enabled")] public bool TlsEnabled { get; set; }
}

public class TLSConfigRequest
{
    [JsonPropertyName("certificate")] public string Certificate { get; set; }
    [JsonPropertyName("private_key")] public string PrivateKey { get; set; }
    [JsonPropertyName("ca_certificate")] public string CaCertificate { get; set; }
}

public class TLSConfigResponse
{
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("certificate")] public string Certificate { get; set; }
}