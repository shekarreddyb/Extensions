public static void SeedEntitiesFromJson<TUnitOfWork>(
    IServiceProvider services,
    string folderPath,
    Action<string>? log = null)
    where TUnitOfWork : class, IUnitOfWork
{
    var uow = services.GetRequiredService<TUnitOfWork>();
    var db = services.GetRequiredService<DbContext>();

    var allEntityTypes = db.Model.GetEntityTypes()
        .Select(t => t.ClrType)
        .Where(t => typeof(IRootEntity).IsAssignableFrom(t) && !t.IsAbstract)
        .ToDictionary(t => t.Name, t => t);

    List<string> order = allEntityTypes.Keys.ToList(); // default unordered

    var orderFile = Path.Combine(folderPath, "_order.txt");
    if (File.Exists(orderFile))
    {
        order = File.ReadAllLines(orderFile)
            .Select(line => line.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name) && allEntityTypes.ContainsKey(name))
            .ToList();
    }

    foreach (var name in order)
    {
        var type = allEntityTypes[name];
        var path = Path.Combine(folderPath, $"{name}.json");
        if (!File.Exists(path)) continue;

        var doc = JsonDocument.Parse(File.ReadAllText(path));
        var repo = RepositoryLocator.ResolveRepository(type, uow);
        var add = repo.GetType().GetMethod("Add")!;

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var entity = ConventionBasedEntityBuilder.BuildEntityFromJson(element, type);
            add.Invoke(repo, new[] { entity });
        }

        log?.Invoke($"Seeded {name} entities.");
    }

    uow.SaveChangesAsync().GetAwaiter().GetResult();
}