using Microsoft.EntityFrameworkCore;
using YourAppNamespace.Models;

namespace YourAppNamespace.Data
{
    public class RedisProvisioningContext : DbContext
    {
        public RedisProvisioningContext(DbContextOptions<RedisProvisioningContext> options)
            : base(options)
        {
        }

        public DbSet<Database> Databases { get; set; }
        public DbSet<DatabaseDatacenter> DatabaseDatacenters { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<RequestDetail> RequestDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1) DATABASE ENTITY
            modelBuilder.Entity<Database>(entity =>
            {
                // PK
                entity.HasKey(e => e.DatabaseId);

                // Table name optional if you want to override
                // entity.ToTable("Databases");

                // Column constraints
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Environment)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.AppId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Lob)
                      .HasMaxLength(100);

                entity.Property(e => e.Module)
                      .HasMaxLength(100);

                entity.Property(e => e.Email)
                      .HasMaxLength(200);
                      // You can do .HasConversion(...) for email, or custom checks if needed

                entity.Property(e => e.Ticket)
                      .HasMaxLength(100);

                entity.Property(e => e.Status)
                      .HasMaxLength(50);

                // One-to-many
                entity.HasMany(d => d.Datacenters)
                      .WithOne(dc => dc.Database)
                      .HasForeignKey(dc => dc.DatabaseId);
            });

            // 2) DATABASE DATACENTER ENTITY
            modelBuilder.Entity<DatabaseDatacenter>(entity =>
            {
                entity.HasKey(e => e.DatacenterId);

                entity.Property(e => e.DC)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Size)
                      .HasMaxLength(50);

                entity.Property(e => e.Status)
                      .HasMaxLength(50);

                // Unique index for (DatabaseId, DC)
                entity.HasIndex(e => new { e.DatabaseId, e.DC })
                      .IsUnique(true);
            });

            // 3) REQUEST ENTITY
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(e => e.RequestId);

                // Possibly specify a max length for Operation & Status
                entity.Property(e => e.Operation)
                      .HasMaxLength(10);

                entity.Property(e => e.Status)
                      .HasMaxLength(50);

                entity.Property(e => e.Message)
                      .HasMaxLength(500);

                // Relationship to Database
                entity.HasOne(r => r.Database)
                      .WithMany() // or .WithMany("Requests") if you add a nav property in Database
                      .HasForeignKey(r => r.DatabaseId);

                // One-to-many
                entity.HasMany(r => r.RequestDetails)
                      .WithOne(rd => rd.Request)
                      .HasForeignKey(rd => rd.RequestId);
            });

            // 4) REQUEST DETAIL ENTITY
            modelBuilder.Entity<RequestDetail>(entity =>
            {
                entity.HasKey(e => e.DetailId);

                entity.Property(e => e.DC)
                      .HasMaxLength(100);

                entity.Property(e => e.Action)
                      .HasMaxLength(10);

                entity.Property(e => e.Status)
                      .HasMaxLength(50);

                entity.Property(e => e.Message)
                      .HasMaxLength(500);

                // If storing a JSON string with diffs, you might do:
                // entity.Property(e => e.FieldChanges).HasColumnType("text");
            });

            // ... Any additional fluent configs ...
        }
    }
}