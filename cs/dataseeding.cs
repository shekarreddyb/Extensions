using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DbContextExtensions
{
    public static async Task UseAsyncSeeding<TContext>(this IServiceProvider serviceProvider, string jsonDirectory) where TContext : DbContext
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var dbSetProperties = dbContext.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        foreach (var dbSetProperty in dbSetProperties)
        {
            var entityType = dbSetProperty.PropertyType.GetGenericArguments().First();
            var jsonFilePath = Path.Combine(jsonDirectory, $"{entityType.Name}.json");

            if (!File.Exists(jsonFilePath))
                continue; // Skip if the JSON file does not exist

            var jsonData = await File.ReadAllTextAsync(jsonFilePath);
            var entities = JsonSerializer.Deserialize(jsonData, typeof(List<>).MakeGenericType(entityType)) as IEnumerable;

            if (entities == null || !entities.Cast<object>().Any()) continue;

            var dbSet = dbSetProperty.GetValue(dbContext);
            var addMethod = dbSet?.GetType().GetMethod("AddRangeAsync");

            if (addMethod != null)
                await (Task)addMethod.Invoke(dbSet, new object[] { entities, default });

            await dbContext.SaveChangesAsync();
        }
    }
}




var builder = WebApplication.CreateBuilder(args);

// Register DbContext
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Define JSON directory path
var jsonDirectory = Path.Combine(app.Environment.ContentRootPath, "SeedData");

// Apply EF Core migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();
    await dbContext.Database.MigrateAsync(); // Ensures database is created and migrated

    // Call UseAsyncSeeding extension method
    await app.Services.UseAsyncSeeding<YourDbContext>(jsonDirectory);
}

app.Run();









using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class JsonSeeder
{
    private readonly DbContext _context;
    private readonly string _jsonDirectory;

    public JsonSeeder(DbContext context, string jsonDirectory)
    {
        _context = context;
        _jsonDirectory = jsonDirectory;
    }

    public async Task SeedDataAsync()
    {
        var dbSetProperties = _context.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        foreach (var dbSetProperty in dbSetProperties)
        {
            var entityType = dbSetProperty.PropertyType.GetGenericArguments().First();
            var jsonFilePath = Path.Combine(_jsonDirectory, $"{entityType.Name}.json");

            if (!File.Exists(jsonFilePath))
                continue; // Skip if file does not exist

            var jsonData = await File.ReadAllTextAsync(jsonFilePath);
            var entities = JsonSerializer.Deserialize(jsonData, typeof(List<>).MakeGenericType(entityType)) as System.Collections.IEnumerable;

            if (entities == null) continue;

            var dbSet = dbSetProperty.GetValue(_context);
            var addMethod = dbSet.GetType().GetMethod("AddRange");

            addMethod?.Invoke(dbSet, new object[] { entities });
        }

        await _context.SaveChangesAsync();
    }
}








using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options) { }

    // Define DbSets
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    // Seed Data from JSON
    public async Task UseAsyncSeeding()
    {
        var jsonDirectory = Path.Combine(AppContext.BaseDirectory, "data");

        if (!Directory.Exists(jsonDirectory))
        {
            Console.WriteLine($"JSON directory not found: {jsonDirectory}");
            return;
        }

        Console.WriteLine($"Seeding from JSON directory: {jsonDirectory}");

        var dbSetProperties = this.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        foreach (var dbSetProperty in dbSetProperties)
        {
            var entityType = dbSetProperty.PropertyType.GetGenericArguments().First();
            var jsonFilePath = Path.Combine(jsonDirectory, $"{entityType.Name}.json");

            if (!File.Exists(jsonFilePath))
                continue; // Skip if file does not exist

            var jsonData = await File.ReadAllTextAsync(jsonFilePath);
            var entities = JsonSerializer.Deserialize(jsonData, typeof(List<>).MakeGenericType(entityType)) as IEnumerable;

            if (entities == null || !entities.Cast<object>().Any()) continue;

            var dbSet = dbSetProperty.GetValue(this);
            var addMethod = dbSet?.GetType().GetMethod("AddRangeAsync");

            if (addMethod != null)
                await (Task)addMethod.Invoke(dbSet, new object[] { entities, default });

            await this.SaveChangesAsync();
        }
    }
}
