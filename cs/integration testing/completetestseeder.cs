public static class EfCoreJsonSeeder
{
    public static void UseSeeding(
        this DbContext dbContext,
        string dbName,
        Action<string>? log = null)
    {
        var commonPath = Path.Combine(AppContext.BaseDirectory, "SeedData");
        var scenarioPath = Path.Combine(commonPath, dbName);

        var entityTypes = dbContext.Model.GetEntityTypes()
            .Select(e => e.ClrType)
            .Where(t => typeof(IRootEntity).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        var order = LoadEntityOrder(commonPath);
        var orderedTypes = order.Any() 
            ? entityTypes.OrderBy(t => order.IndexOf(t.Name)).ToList() 
            : entityTypes;

        foreach (var entityType in orderedTypes)
        {
            var entityName = entityType.Name;
            var commonFile = Path.Combine(commonPath, $"{entityName}.json");
            var scenarioFile = Path.Combine(scenarioPath, $"{entityName}.json");

            // Merge JSON from common and scenario folders
            var json = MergeJsonFiles(commonFile, scenarioFile);
            if (json == null) continue;

            var set = dbContext.Set(entityType);
            foreach (var element in json.RootElement.EnumerateArray())
            {
                var entity = LoadEntityFromJson(dbContext, element, entityType);
                set.Add(entity);
            }

            log?.Invoke($"Seeded {entityName} entities.");
        }

        dbContext.SaveChanges();
        log?.Invoke("Seeding completed.");
    }

    private static List<string> LoadEntityOrder(string path)
    {
        var orderFile = Path.Combine(path, "_order.txt");
        if (!File.Exists(orderFile)) return new List<string>();

        return File.ReadAllLines(orderFile)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static JsonDocument? MergeJsonFiles(string commonFilePath, string scenarioFilePath)
    {
        var records = new Dictionary<string, JsonElement>();

        // Load common file
        if (File.Exists(commonFilePath))
        {
            var json = File.ReadAllText(commonFilePath);
            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("id", out var idProp))
                {
                    var id = idProp.GetString();
                    if (id != null) records[id] = element;
                }
            }
        }

        // Override with scenario file
        if (File.Exists(scenarioFilePath))
        {
            var json = File.ReadAllText(scenarioFilePath);
            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("id", out var idProp))
                {
                    var id = idProp.GetString();
                    if (id != null) records[id] = element;
                }
            }
        }

        if (!records.Any()) return null;

        var mergedArray = JsonSerializer.SerializeToUtf8Bytes(records.Values);
        return JsonDocument.Parse(mergedArray);
    }

    private static object LoadEntityFromJson(DbContext dbContext, JsonElement json, Type entityType)
    {
        var entityTypeConfig = dbContext.Model.FindEntityType(entityType);
        if (entityTypeConfig == null)
            throw new InvalidOperationException($"Entity type {entityType.Name} is not mapped.");

        var instance = Activator.CreateInstance(entityType);

        foreach (var property in entityTypeConfig.GetProperties())
        {
            if (!json.TryGetProperty(property.Name, out var jsonProp)) continue;

            var value = ConvertJsonValue(jsonProp, property.ClrType, property);
            property.PropertyInfo?.SetValue(instance, value);
        }

        foreach (var navigation in entityTypeConfig.GetNavigations())
        {
            if (!json.TryGetProperty(navigation.Name, out var navProp)) continue;

            if (navigation.IsCollection)
            {
                SetBackedCollection(instance, navigation, navProp);
            }
            else
            {
                var related = LoadEntityFromJson(dbContext, navProp, navigation.ClrType);
                navigation.PropertyInfo?.SetValue(instance, related);
            }
        }

        return instance;
    }

    private static void SetBackedCollection(object instance, INavigation navigation, JsonElement jsonArray)
    {
        var field = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name.Contains(navigation.Name, StringComparison.OrdinalIgnoreCase));

        if (field == null) return;

        var list = field.GetValue(instance) as IList;
        if (list == null) throw new InvalidOperationException("Backed list is not initialized.");

        var itemType = field.FieldType.GetGenericArguments().First();
        foreach (var itemElement in jsonArray.EnumerateArray())
        {
            var item = JsonSerializer.Deserialize(itemElement.GetRawText(), itemType);
            list.Add(item);
        }
    }

    private static object ConvertJsonValue(JsonElement jsonProp, Type targetType, IProperty property)
    {
        if (property.GetValueConverter() is ValueConverter converter)
        {
            var providerValue = JsonSerializer.Deserialize(jsonProp.GetRawText(), converter.ProviderClrType);
            return converter.ConvertFromProvider(providerValue);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, jsonProp.GetString()!);
        }

        return JsonSerializer.Deserialize(jsonProp.GetRawText(), targetType);
    }
}