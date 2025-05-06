public static class ConventionBasedEntityBuilder
{
    public static object BuildEntityFromJson(JsonElement json, Type entityType)
    {
        var instance = Activator.CreateInstance(entityType, nonPublic: true)
                       ?? throw new InvalidOperationException($"Could not create instance of {entityType.Name}");

        foreach (var prop in json.EnumerateObject())
        {
            var fieldName = prop.Name; // e.g., "_addresses"
            if (!fieldName.StartsWith("_")) continue;

            var backingField = entityType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (backingField == null || !IsList(backingField.FieldType)) continue;

            var itemType = backingField.FieldType.GetGenericArguments().First();

            // Convert "_addresses" => "Addresses"
            var propertyName = Char.ToUpper(fieldName[1]) + fieldName.Substring(2);
            var methodName = $"Add{propertyName}";

            var method = entityType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException($"Method {methodName} not found on {entityType.Name}");

            // Deserialize list into correct type
            var listType = typeof(List<>).MakeGenericType(itemType);
            var data = JsonSerializer.Deserialize(prop.Value.GetRawText(), listType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            method.Invoke(instance, new[] { data });
        }

        // Optionally bind simple scalar props (id, name, etc.)
        foreach (var scalar in json.EnumerateObject())
        {
            if (scalar.Name.StartsWith("_")) continue;

            var propInfo = entityType.GetProperty(scalar.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo != null && propInfo.CanWrite)
            {
                var value = JsonSerializer.Deserialize(scalar.Value.GetRawText(), propInfo.PropertyType);
                propInfo.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static bool IsList(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
}







public static class ConventionBasedSeeder
{
    public static void SeedEntitiesFromJson<TUnitOfWork>(
        IServiceProvider services,
        string folderPath,
        Action<string>? log = null)
        where TUnitOfWork : class, IUnitOfWork
    {
        var uow = services.GetRequiredService<TUnitOfWork>();
        var db = services.GetRequiredService<DbContext>();

        var rootTypes = db.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(t => typeof(IRootEntity).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        foreach (var type in rootTypes)
        {
            var path = Path.Combine(folderPath, $"{type.Name}.json");
            if (!File.Exists(path))
            {
                log?.Invoke($"Skipping {type.Name}: no JSON file.");
                continue;
            }

            var doc = JsonDocument.Parse(File.ReadAllText(path));
            var repo = RepositoryLocator.ResolveRepository(type, uow);
            var add = repo.GetType().GetMethod("Add")!;

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var entity = ConventionBasedEntityBuilder.BuildEntityFromJson(element, type);
                add.Invoke(repo, new[] { entity });
            }

            log?.Invoke($"Seeded {type.Name}.");
        }

        uow.SaveChangesAsync().GetAwaiter().GetResult();
    }
}

