public static class WebAppFactorySeedExtensions
{
    public static void SeedWith<TDbContext>(this WebApplicationFactory<Program> factory, Action<TDbContext> seedAction)
        where TDbContext : DbContext
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        db.Database.EnsureDeleted();  // Ensures fresh state for each test
        db.Database.EnsureCreated();  // Recreates schema

        seedAction(db);               // Run your custom seeding logic

        db.SaveChanges();             // Save changes after seeding
    }
}




public class ItemApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ItemApiTests(CustomWebApplicationFactory factory)
    {
        // Seed your in-memory DB
        factory.SeedWith<AppDbContext>(db =>
        {
            db.Items.AddRange(
                new Item { Id = 1, Name = "Seeded Item 1" },
                new Item { Id = 2, Name = "Seeded Item 2" }
            );
        });

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_ReturnsSeededData()
    {
        var response = await _client.GetAsync("/api/items");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Seeded Item 1", content);
        Assert.Contains("Seeded Item 2", content);
    }
}





