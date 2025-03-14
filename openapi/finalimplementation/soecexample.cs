public class DatabaseByEnvironmentSpecification : BaseSpecification<Database>
{
    public DatabaseByEnvironmentSpecification(string environment, int page, int pageSize)
        : base(d => d.Environment == environment)
    {
        ApplyPaging(page, pageSize);
        ApplyOrderBy(d => d.Name);
    }
}

public class SomeService
{
    private readonly IUnitOfWork _unitOfWork;

    public SomeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task SearchDatabases(string environment, int page, int pageSize)
    {
        var spec = new DatabaseByEnvironmentSpecification(environment, page, pageSize);
        var results = await _unitOfWork.Databases.SearchAsync(spec);

        foreach (var db in results)
        {
            Console.WriteLine($"Database: {db.Name}");
        }
    }
}