using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories.Interfaces;

namespace ERP.WorkflowwServices.API.Services
{
    public class MenuService:IMenuService
    {
        private readonly IRepository<MenuItem, Guid> _repository;
        public MenuService(IRepository<MenuItem, Guid> repository)
        {
            _repository = repository;
        }

        // ===============================
        // GET MENU TREE (Sidebar API)
        // ===============================
        public async Task<List<MenuItem>> GetMenuTreeAsync()
        {
            var items = (await _repository.GetAllAsync())
                .Where(x => x.IsVisible && x.IsActive)
                .OrderBy(x => x.OrderNo)
                .ToList();

            return BuildTree(items);
        }

        // ===============================
        // GET BY ID
        // ===============================
        public async Task<MenuItem?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // ===============================
        // CREATE MENU
        // ===============================
        public async Task<MenuItem> CreateAsync(MenuItem menu)
        {
            menu.Id = Guid.NewGuid();

            await _repository.AddAsync(menu);
            await _repository.SaveChangesAsync();

            return menu;
        }

        // ===============================
        // UPDATE MENU
        // ===============================
        public async Task UpdateAsync(MenuItem menu)
        {
            _repository.Update(menu);
            await _repository.SaveChangesAsync();
        }

        // ===============================
        // SOFT DELETE
        // ===============================
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
                throw new Exception("Menu not found");

            entity.IsDeleted = true;

            _repository.Update(entity);
            await _repository.SaveChangesAsync();
        }

        // ===============================
        // CHECK DUPLICATE
        // ===============================
        public async Task<bool> ExistsAsync(string label, Guid? parentId)
        {
            return await _repository.AnyAsync(x =>
                x.Label == label &&
                x.ParentId == parentId &&
                !x.IsDeleted);
        }

        // ===============================
        // TREE BUILDER
        // ===============================
        private List<MenuItem> BuildTree(List<MenuItem> items)
        {
            var lookup = items.ToDictionary(x => x.Id);

            var roots = new List<MenuItem>();

            foreach (var item in items)
            {
                if (item.ParentId != null &&
                    lookup.ContainsKey(item.ParentId.Value))
                {
                    var parent = lookup[item.ParentId.Value];

                    parent.Children ??= new List<MenuItem>();
                    parent.Children.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            return roots;
        }
    }
}
