using System.Linq.Expressions;

namespace ERP.WorkflowwServices.API.Repositories.Interfaces
{
    public interface IRepository<T,TKey> where T : class
    {
        Task<T?> GetByIdAsync(TKey id);

        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        void Update(T entity);

        void Remove(T entity);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);       
        IQueryable<T> Query();
        Task<int> SaveChangesAsync();
    }
}
