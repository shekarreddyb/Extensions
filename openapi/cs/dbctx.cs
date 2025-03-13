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
            // 1) Database entity
            modelBuilder.Entity<Database>(entity =>
            {
                entity.HasKey(e => e.DatabaseId);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AppId).IsRequired().HasMaxLength(100);

                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Ticket).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasMany(d => d.Datacenters)
                      .WithOne(dc => dc.Database)
                      .HasForeignKey(dc => dc.DatabaseId);
            });

            // 2) DatabaseDatacenter entity
            modelBuilder.Entity<DatabaseDatacenter>(entity =>
            {
                entity.HasKey(e => e.DatacenterId);

                entity.Property(e => e.DC).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Size).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasIndex(e => new { e.DatabaseId, e.DC }).IsUnique();
            });

            // 3) Request entity
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(e => e.RequestId);

                entity.Property(e => e.Operation).HasMaxLength(10);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Message).HasMaxLength(500);

                // Relationship to Database (optional, if it references a single DB)
                entity.HasOne(r => r.Database)
                      .WithMany() // or define a "Requests" collection in Database if you like
                      .HasForeignKey(r => r.DatabaseId);

                // One-to-many relationship with RequestDetail
                entity.HasMany(r => r.RequestDetails)
                      .WithOne(rd => rd.Request)
                      .HasForeignKey(rd => rd.RequestId);
            });

            // 4) RequestDetail entity
            modelBuilder.Entity<RequestDetail>(entity =>
            {
                entity.HasKey(e => e.DetailId);

                entity.Property(e => e.DC).HasMaxLength(100);
                entity.Property(e => e.Action).HasMaxLength(10);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Message).HasMaxLength(500);
            });
        }
    }
}