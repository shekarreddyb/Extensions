public static JsonDocument MergeJsonFiles(string? commonFilePath, string? scenarioFilePath)
{
    var records = new Dictionary<string, JsonElement>();

    // Load and merge common file if it exists
    if (!string.IsNullOrEmpty(commonFilePath) && File.Exists(commonFilePath))
    {
        var commonJson = File.ReadAllText(commonFilePath);
        using var commonDoc = JsonDocument.Parse(commonJson);
        foreach (var element in commonDoc.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("id", out var idProp))
            {
                var id = idProp.GetString();
                if (id != null)
                    records[id] = element;
            }
        }
    }

    // Load and merge scenario file if it exists
    if (!string.IsNullOrEmpty(scenarioFilePath) && File.Exists(scenarioFilePath))
    {
        var scenarioJson = File.ReadAllText(scenarioFilePath);
        using var scenarioDoc = JsonDocument.Parse(scenarioJson);
        foreach (var element in scenarioDoc.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("id", out var idProp))
            {
                var id = idProp.GetString();
                if (id != null)
                    records[id] = element;
            }
        }
    }

    // Convert merged dictionary to JSON array
    var mergedArray = JsonSerializer.SerializeToUtf8Bytes(records.Values);
    return JsonDocument.Parse(mergedArray);
}