using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class;
        IRepository<Users, Guid> Users { get; }
        IRepository<RefreshToken, int> RefreshTokens { get; }
        IRepository<MenuItem, Guid> Menus { get; }
        IRepository<Module, Guid> Modules { get; }        
        IRepository<WFEvent, Guid> WFEvent { get; }        
        Task<int> SaveChangesAsync();
    }
}
