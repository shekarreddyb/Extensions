using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourAppNamespace.Data
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(Guid id);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourAppNamespace.Data
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            // This assumes T has a Guid primary key recognized by EF.
            // If your PK is not always a Guid or not always named "Id",
            // you may need a more flexible approach.
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }
    }
}

using System;
using System.Threading.Tasks;

namespace YourAppNamespace.Data
{
    public interface IUnitOfWork : IDisposable
    {
        // Expose repositories for each entity you need:
        IGenericRepository<Foo> Foos { get; }
        IGenericRepository<Bar> Bars { get; }
        // ... add more repositories as needed, or use a method like GetRepository<T>()

        Task<int> SaveChangesAsync();
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace YourAppNamespace.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;

        // Backing fields for repositories
        private IGenericRepository<Foo> _fooRepository;
        private IGenericRepository<Bar> _barRepository;
        
        public UnitOfWork(DbContext context)
        {
            _context = context;
        }

        // Example of typed properties
        public IGenericRepository<Foo> Foos 
            => _fooRepository ??= new GenericRepository<Foo>(_context);

        public IGenericRepository<Bar> Bars 
            => _barRepository ??= new GenericRepository<Bar>(_context);

        // If you want a dynamic approach, you could do:
        // public IGenericRepository<T> GetRepository<T>() where T : class
        // {
        //     return new GenericRepository<T>(_context);
        // }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}


public class SomeBusinessService
{
    private readonly IUnitOfWork _unitOfWork;

    public SomeBusinessService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task DoSomeWorkAsync()
    {
        // Access the 'Foos' repository
        var allFoos = await _unitOfWork.Foos.GetAllAsync();

        // Create a new Foo
        var newFoo = new Foo { FooId = Guid.NewGuid(), Name = "Test Foo" };
        await _unitOfWork.Foos.AddAsync(newFoo);

        // Access the 'Bars' repository
        var bar = await _unitOfWork.Bars.GetByIdAsync(Guid.Parse("some-guid"));
        if (bar != null)
        {
            bar.Description = "Updated Bar";
            _unitOfWork.Bars.Update(bar);
        }

        // Commit all changes in one go
        await _unitOfWork.SaveChangesAsync();
    }
}