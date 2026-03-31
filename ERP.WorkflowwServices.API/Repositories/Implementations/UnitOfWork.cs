using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;

namespace ERP.WorkflowwServices.API.Repositories.Implementations
{
    public class UnitOfWork:IUnitOfWork
    {
        private readonly WorkflowDbContext _context;

        // 🔥 Repository cache (important)
        private readonly Dictionary<string, object> _repositories = new();

        public UnitOfWork(WorkflowDbContext context)
        {
            _context = context;
        }

        // ===============================
        // GENERIC REPOSITORY ACCESS
        // ===============================
        public IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class
        {
            var type = typeof(TEntity).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repoInstance = new Repository<TEntity, TKey>(_context);
                _repositories[type] = repoInstance;
            }

            return (IRepository<TEntity, TKey>)_repositories[type];
        }

        // ===============================
        // OPTIONAL SHORTCUTS (Readable code)
        // ===============================
        public IRepository<Users, Guid> Users => Repository<Users, Guid>();
        public IRepository<RefreshToken, int> RefreshTokens => Repository<RefreshToken, int>();
        public IRepository<MenuItem, Guid> Menus => Repository<MenuItem, Guid>();
        public IRepository<Module, Guid> Modules => Repository<Module, Guid>();
        public IRepository<WFEvent, Guid> WFEvent => Repository<WFEvent, Guid>();

        // ===============================
        // SAVE
        // ===============================
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
