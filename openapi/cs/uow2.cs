using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace YourAppNamespace
{
    // ===============================
    // 1. BASE INTERFACE (SOFT DELETE + AUDIT FIELDS)
    // ===============================
    public interface IAuditableEntity
    {
        Guid Id { get; set; }
        DateTime CreatedOn { get; set; }
        string CreatedBy { get; set; }
        DateTime? ModifiedOn { get; set; }
        string ModifiedBy { get; set; }
        bool IsDeleted { get; set; }
    }

    // ===============================
    // 2. ENTITY EXAMPLES (DATABASE)
    // ===============================
    public class Database : IAuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }
        public string AppId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }

    // ===============================
    // 3. INTERCEPTOR (SOFT DELETE & AUDIT)
    // ===============================
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly string _currentUser;
        public AuditInterceptor(string currentUser = "System")
        {
            _currentUser = currentUser;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ApplyAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAuditFields(DbContext context)
        {
            if (context == null) return;

            foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedOn = DateTime.UtcNow;
                        entry.Entity.CreatedBy = _currentUser;
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedOn = DateTime.UtcNow;
                        entry.Entity.ModifiedBy = _currentUser;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.ModifiedOn = DateTime.UtcNow;
                        entry.Entity.ModifiedBy = _currentUser;
                        break;
                }
            }
        }
    }

    // ===============================
    // 4. DATABASE CONTEXT (GLOBAL QUERY FILTER)
    // ===============================
    public class RedisProvisioningContext : DbContext
    {
        private readonly AuditInterceptor _auditInterceptor;

        public RedisProvisioningContext(DbContextOptions<RedisProvisioningContext> options, AuditInterceptor auditInterceptor)
            : base(options)
        {
            _auditInterceptor = auditInterceptor;
        }

        public DbSet<Database> Databases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var filter = Expression.Lambda(
                        Expression.Equal(
                            Expression.Property(parameter, nameof(IAuditableEntity.IsDeleted)),
                            Expression.Constant(false)
                        ),
                        parameter
                    );
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditInterceptor);
        }
    }

    // ===============================
    // 5. GENERIC REPOSITORY
    // ===============================
    public interface IGenericRepository<T> where T : class, IAuditableEntity
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void SoftDelete(T entity);
        Task<int> SaveChangesAsync();
    }

    public class GenericRepository<T> : IGenericRepository<T> where T : class, IAuditableEntity
    {
        private readonly RedisProvisioningContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(RedisProvisioningContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void SoftDelete(T entity)
        {
            entity.IsDeleted = true;
            _dbSet.Update(entity);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }

    // ===============================
    // 6. UNIT OF WORK
    // ===============================
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Database> Databases { get; }
        Task<int> SaveChangesAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly RedisProvisioningContext _context;
        private IGenericRepository<Database> _databases;

        public UnitOfWork(RedisProvisioningContext context)
        {
            _context = context;
        }

        public IGenericRepository<Database> Databases =>
            _databases ??= new GenericRepository<Database>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

    // ===============================
    // 7. SERVICE CONFIGURATION (DEPENDENCY INJECTION)
    // ===============================
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<AuditInterceptor>();

            builder.Services.AddDbContext<RedisProvisioningContext>((serviceProvider, options) =>
            {
                var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(interceptor);
            });

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            var app = builder.Build();
            app.Run();
        }
    }
}