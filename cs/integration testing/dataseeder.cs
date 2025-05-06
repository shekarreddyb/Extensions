// ---------- Interfaces ----------

public interface IEntityBuilder<T>
{
    T Build(JObject source);
}

public interface IRepository<T> where T : class
{
    void Add(T entity);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}

// ---------- Domain Model ----------

public class User
{
    private readonly List<Address> _addresses = new();
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public IReadOnlyCollection<Address> Addresses => _addresses;

    public User(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public void AddAddress(string city, string country)
    {
        _addresses.Add(new Address(city, country));
    }
}

public class Address
{
    public string City { get; }
    public string Country { get; }

    public Address(string city, string country)
    {
        City = city;
        Country = country;
    }
}

// ---------- Builder ----------

public class UserBuilder : IEntityBuilder<User>
{
    public User Build(JObject source)
    {
        var id = Guid.Parse(source["id"]!.ToString());
        var name = source["name"]!.ToString();
        var user = new User(id, name);

        foreach (var addr in source["addresses"]!)
        {
            user.AddAddress(addr["city"]!.ToString(), addr["country"]!.ToString());
        }

        return user;
    }
}

// ---------- Builder Discovery ----------

public static class EntityBuilderFactory
{
    private static readonly Dictionary<string, object> _builders = new();

    static EntityBuilderFactory()
    {
        var builderTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityBuilder<>)))
            .ToList();

        foreach (var type in builderTypes)
        {
            var entityType = type.GetInterfaces()
                .First(i => i.GetGenericTypeDefinition() == typeof(IEntityBuilder<>))
                .GetGenericArguments()[0];

            _builders[entityType.Name] = Activator.CreateInstance(type)!;
        }
    }

    public static (Type entityType, object builder) ResolveBuilder(string entityName)
    {
        if (!_builders.TryGetValue(entityName, out var builder))
            throw new InvalidOperationException($"No builder for '{entityName}'");

        var entityType = builder.GetType().GetInterfaces()
            .First(i => i.GetGenericTypeDefinition() == typeof(IEntityBuilder<>))
            .GetGenericArguments()[0];

        return (entityType, builder);
    }
}

public static class RepositoryLocator
{
    public static object ResolveRepository(Type entityType, IUnitOfWork uow)
    {
        var repoProp = uow.GetType()
            .GetProperties()
            .FirstOrDefault(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
                p.PropertyType.GetGenericArguments()[0] == entityType);

        if (repoProp == null)
            throw new InvalidOperationException($"No repository found for type '{entityType.Name}'");

        return repoProp.GetValue(uow)!;
    }
}

// ---------- Seeding Extension ----------

public static class WebAppFactorySeedingExtensions
{
    public static void SeedScenario<TUnitOfWork>(
        this WebApplicationFactory<Program> factory,
        string scenarioName,
        Action<string>? log = null)
        where TUnitOfWork : class, IUnitOfWork
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var uow = services.GetRequiredService<TUnitOfWork>();

        var scenarioDir = Path.Combine(AppContext.BaseDirectory, "SeedData", scenarioName);
        if (!Directory.Exists(scenarioDir))
            throw new DirectoryNotFoundException($"No seed directory for scenario: {scenarioName}");

        foreach (var file in Directory.GetFiles(scenarioDir, "*.json"))
        {
            var json = File.ReadAllText(file);
            var jArray = JArray.Parse(json);
            var entityName = Path.GetFileNameWithoutExtension(file).Split('-')[0];

            var (entityType, builder) = EntityBuilderFactory.ResolveBuilder(entityName);
            var buildMethod = builder.GetType().GetMethod("Build")!;
            var repo = RepositoryLocator.ResolveRepository(entityType, uow);
            var addMethod = repo.GetType().GetMethod("Add")!;

            log?.Invoke($"Seeding {jArray.Count} {entityName}(s)...");

            foreach (var jObj in jArray.OfType<JObject>())
            {
                var entity = buildMethod.Invoke(builder, new object[] { jObj });
                addMethod.Invoke(repo, new[] { entity });
            }
        }

        uow.SaveChangesAsync().GetAwaiter().GetResult();
        log?.Invoke("Seeding completed.");
    }
}

// ---------- Usage in Test ----------

// Setup your test class like this:
public class MyTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MyTest(CustomWebApplicationFactory factory)
    {
        // Use unique DB per test run (parallel-safe)
        var dbName = $"TestDb_{Guid.NewGuid()}";
        factory.UseInMemoryDatabase(dbName);

        factory.SeedScenario<IUnitOfWork>("CreateUser", Console.WriteLine);
        _client = factory.CreateClient();
    }
}

// Optional: Add helper for test-specific in-memory DB names
public static class WebAppFactoryInMemoryDbExtensions
{
    public static void UseInMemoryDatabase(this WebApplicationFactory<Program> factory, string dbName)
    {
        factory.Services.GetRequiredService<IServiceCollection>()
            .RemoveAll<DbContextOptions<AppDbContext>>(); // or your concrete type

        factory.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }
}





/SeedData/
  CreateUser/
    User-seed.json
  ApproveRequest/
    Admin-seed.json
    Request-seed.json





factory.SeedScenario<IUnitOfWork>("CreateUser", Console.WriteLine);


