using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ERP.WorkflowwServices.API.Repositories.Implementations
{
    public class Repository<T,TKey>:IRepository<T,TKey> where T:class
    {
        protected readonly WorkflowDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public Repository(WorkflowDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }       

        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
