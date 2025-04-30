// ---------------------------
// 1. DDD Value Object: OrganizationalUnits
// ---------------------------
public class OrganizationalUnits : ValueObject
{
    public IReadOnlyList<string> Values { get; }

    public OrganizationalUnits(IEnumerable<string> values)
    {
        if (values == null || !values.Any())
            throw new ArgumentException("At least one OU is required", nameof(values));

        Values = values.Select(v => v.Trim())
                       .Where(v => !string.IsNullOrWhiteSpace(v))
                       .Distinct()
                       .ToList();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var item in Values.OrderBy(x => x))
            yield return item;
    }

    public override string ToString() => string.Join(", ", Values);
}

// ---------------------------
// 2. Base ValueObject class (optional if you don't already have it)
// ---------------------------
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(1, (hash, obj) => HashCode.Combine(hash, obj?.GetHashCode() ?? 0));
}

// ---------------------------
// 3. Entity: TlsCertificate
// ---------------------------
public class TlsCertificate
{
    public Guid Id { get; private set; }

    public string Cn { get; private set; }

    public OrganizationalUnits Ous { get; private set; }

    // Constructor for EF
    private TlsCertificate() { }

    public TlsCertificate(string cn, IEnumerable<string> ous)
    {
        Id = Guid.NewGuid();
        Cn = cn;
        Ous = new OrganizationalUnits(ous);
    }
}

// ---------------------------
// 4. EF Core Configuration (inside OnModelCreating or separate config class)
// ---------------------------
builder.Entity<TlsCertificate>()
    .Property(x => x.Ous)
    .HasConversion(
        ous => JsonSerializer.Serialize(ous.Values, (JsonSerializerOptions)null),
        json => new OrganizationalUnits(JsonSerializer.Deserialize<List<string>>(json) ?? new())
    )
    .HasColumnType("nvarchar(max)");