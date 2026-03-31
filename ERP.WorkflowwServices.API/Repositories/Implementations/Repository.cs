using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ERP.WorkflowwServices.API.Repositories.Implementations
{
    public class Repository<T, TKey> : IRepository<T, TKey> where T : class
    {
        protected readonly WorkflowDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public Repository(WorkflowDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // ===============================
        // GET BY ID
        // ===============================
        public async Task<T?> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        // ===============================
        // GET ALL (⚠️ Avoid for large data)
        // ===============================
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        // ===============================
        // FIND WITH FILTER
        // ===============================
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        // ===============================
        // QUERY (BEST METHOD 🔥)
        // ===============================
        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        // ===============================
        // ADD
        // ===============================
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        // ===============================
        // UPDATE (SAFE VERSION)
        // ===============================
        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        // ===============================
        // REMOVE
        // ===============================
        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        // ===============================
        // ANY
        // ===============================
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        // ===============================
        // SAVE
        // ===============================
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<(List<T> Data, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? filter, int pageNumber, int pageSize)
        {
            var query = _dbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            var total = await query.CountAsync();

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (data, total);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task<bool> ExistsAsync(TKey id)
        {
            var entity = await GetByIdAsync(id);
            return entity != null;
        }

        public IQueryable<T> Query(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
                query = query.Include(include);

            return query;
        }

        public void UpdateFields(T entity, params Expression<Func<T, object>>[] updatedProperties)
        {
            _dbSet.Attach(entity);

            foreach (var property in updatedProperties)
            {
                _context.Entry(entity).Property(property).IsModified = true;
            }
        }        
    }
}
