using ERP.WorkflowwServices.API.Models;
using System.Linq.Expressions;

namespace ERP.WorkflowwServices.API.Repositories.Interfaces
{
    public interface IRepository<T, TKey> where T : class
    {
        Task<T?> GetByIdAsync(TKey id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        IQueryable<T> Query();

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        Task<(List<T> Data, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? filter, int pageNumber, int pageSize);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(TKey id);

        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        void Update(T entity);
        void UpdateFields(T entity, params Expression<Func<T, object>>[] properties);

        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
