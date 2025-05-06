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

            var itemType = backingField.FieldType.GetGenericArguments()[0];
            var pluralMethodName = $"Add{Char.ToUpper(fieldName[1]) + fieldName.Substring(2)}"; // AddAddresses
            var singularMethodName = $"Add{itemType.Name}"; // AddAddress

            var pluralMethod = entityType.GetMethod(pluralMethodName, new[] { typeof(IEnumerable<>).MakeGenericType(itemType) });
            var singularMethod = entityType.GetMethod(singularMethodName, new[] { itemType });

            var listType = typeof(List<>).MakeGenericType(itemType);
            var deserializedItems = JsonSerializer.Deserialize(prop.Value.GetRawText(), listType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pluralMethod != null)
            {
                // Preferred path: call AddAddresses(List<T>)
                pluralMethod.Invoke(instance, new[] { deserializedItems });
            }
            else if (singularMethod != null && deserializedItems is IEnumerable<object> itemList)
            {
                // Fallback: call AddAddress(T) for each item
                foreach (var item in itemList)
                    singularMethod.Invoke(instance, new[] { item });
            }
        }

        // Optionally bind scalar properties (id, name, etc.)
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