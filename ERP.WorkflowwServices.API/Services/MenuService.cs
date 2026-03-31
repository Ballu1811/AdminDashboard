using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _uow;
        public MenuService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ===============================
        // GET MENU TREE (Sidebar API)
        // ===============================
        public async Task<List<MenuDto>> GetMenuTreeAsync()
        {
            var items = await BaseQuery().ToListAsync();

            return BuildTree(items);
        }

        public async Task<List<MenuDto>> GetMenuTreeByModuleAsync(Guid moduleId)
        {
            var items = await BaseQuery()
                .Where(x => x.ModuleId == moduleId || x.ModuleId == null)
                .ToListAsync();

            return BuildTree(items);
        }


        // ======================================================
        // GET ALL (FLAT LIST)
        // ======================================================
        public async Task<PagedResult<MenuDto>> GetAllAsync(FilterModel filter)
        {
            bool? checkstatus = null;

            if (filter.Status <= 1)
                checkstatus = filter.Status > 0;

            var query = _uow.Menus.Query().AsNoTracking().Where(x => (checkstatus == null || x.IsDeleted == checkstatus) && x.IsActive);

            if (filter.HaveFilter && !string.IsNullOrWhiteSpace(filter.KeyWord))
            {
                var keyword = filter.KeyWord.Trim();
                query = query.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Label) && x.Label.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Route) && x.Route.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (x.ParentId != null && x.ParentId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            var total = await query.CountAsync();
            if (!filter.ExportExcel)
            {
                query = query.Skip((filter.PageNumber - 1) * filter.PageSize)
                 .Take(filter.PageSize);
            }

            // ✅ Projection (DTO)
            var data = await query
                .OrderBy(x => x.OrderNo)
                .Select(x => new MenuDto
                {
                    Id = x.Id,
                    Label = x.Label,
                    Route = x.Route,
                    Icon = x.Icon,
                    ParentId = x.ParentId,
                    ModuleId = x.ModuleId,
                    OrderNo = x.OrderNo,

                    MenuType = x.MenuType,
                    Permission = x.Permission,
                    Tooltip = x.Tooltip,

                    IsExternal = x.IsExternal,
                    ExternalUrl = x.ExternalUrl,
                    Target = x.Target,
                    QueryParams = x.QueryParams,

                    Badge = x.Badge,
                    Color = x.Color,
                    CssClass = x.CssClass,

                    IsActive = x.IsActive,
                    IsVisible = x.IsVisible,

                    ShowInSidebar = x.ShowInSidebar,
                    ShowInTopbar = x.ShowInTopbar,

                    IsDefault = x.IsDefault,
                    IsHidden = x.IsHidden,
                    IsSystem = x.IsSystem,

                    DefaultExpanded = x.DefaultExpanded,

                    Children = new List<MenuDto>()
                }).ToListAsync();

            return new PagedResult<MenuDto>
            {
                Total = total,
                Data = data
            };
        }

        // ===============================
        // GET BY ID
        // ===============================
        public async Task<MenuDto?> GetByIdAsync(Guid id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        // ===============================
        // CREATE MENU
        // ===============================
        public async Task<MenuDto> CreateAsync(MenuCreateDto dto)
        {
            var entity = new MenuItem
            {
                Id = Guid.NewGuid(),

                // 🔹 Basic
                Label = dto.Label,
                Route = dto.Route,
                Icon = dto.Icon,
                ParentId = dto.ParentId,
                OrderNo = dto.OrderNo,

                // 🔹 Behavior
                MenuType = string.IsNullOrEmpty(dto.MenuType) ? "link" : dto.MenuType,
                Permission = dto.Permission,
                Tooltip = dto.Tooltip,

                // 🔹 External
                IsExternal = dto.IsExternal,
                ExternalUrl = dto.ExternalUrl,
                Target = dto.Target ?? "_self",

                // 🔹 Module (🔥 VERY IMPORTANT)
                ModuleId = dto.ModuleId,

                Badge = dto.Badge,
                Color = dto.Color,
                CssClass = dto.CssClass,

                IsActive = dto.IsActive,
                IsVisible = dto.IsVisible,

                ShowInSidebar = dto.ShowInSidebar,
                ShowInTopbar = dto.ShowInTopbar,

                IsDefault = dto.IsDefault,
                IsHidden = dto.IsHidden,
                IsSystem = dto.IsSystem,

                // 🔹 UI State
                DefaultExpanded = dto.DefaultExpanded
            };

            await _uow.Menus.AddAsync(entity);
            await _uow.SaveChangesAsync();

            // 🔥 return DTO
            return await GetByIdAsync(entity.Id) ?? throw new Exception("Creation failed");
        }

        // ===============================
        // UPDATE MENU
        // ===============================
        public async Task UpdateAsync(MenuUpdateDto model)
        {
            var entity = await _uow.Menus.GetByIdAsync(model.Id);

            if (entity == null)
                throw new Exception("Menu not found");

            // ❌ Self parent
            if (model.ParentId == model.Id)
                throw new Exception("Menu cannot be its own parent");

            // ❌ Circular tree
            if (await IsCircular(model.Id, model.ParentId))
                throw new Exception("Circular hierarchy detected");

            // ✅ Basic
            entity.Label = model.Label;
            entity.Icon = model.Icon;
            entity.Route = model.Route;
            entity.ParentId = model.ParentId;
            entity.OrderNo = model.OrderNo;

            // ✅ Behavior
            entity.Permission = model.Permission;
            entity.MenuType = string.IsNullOrEmpty(model.MenuType) ? "link" : model.MenuType;
            entity.Tooltip = model.Tooltip;

            // ✅ External
            entity.IsExternal = model.IsExternal;
            entity.ExternalUrl = model.ExternalUrl;
            entity.Target = model.Target ?? "_self";

            // ✅ Module (🔥 IMPORTANT)
            entity.ModuleId = model.ModuleId;

            entity.Badge = model.Badge;
            entity.Color = model.Color;
            entity.CssClass = model.CssClass;

            entity.ShowInSidebar = model.ShowInSidebar;
            entity.ShowInTopbar = model.ShowInTopbar;

            entity.IsDefault = model.IsDefault;
            entity.IsHidden = model.IsHidden;
            entity.IsSystem = model.IsSystem;

            // ✅ UI
            entity.DefaultExpanded = model.DefaultExpanded;

            // ✅ Visibility
            entity.IsActive = model.IsActive;
            entity.IsVisible = model.IsVisible;

            // 🔥 Validation
            if (entity.MenuType == "link" && string.IsNullOrEmpty(entity.Route))
                throw new Exception("Route is required for link menu");

            if (entity.MenuType == "group")
                entity.Route = null;

            try
            {
                _uow.Menus.Update(entity);
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        // ===============================
        // SOFT DELETE
        // ===============================
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _uow.Menus.GetByIdAsync(id);

            if (entity == null)
                throw new Exception("Menu not found");

            entity.IsDeleted = true;

            _uow.Menus.Update(entity);

            await _uow.SaveChangesAsync();
        }

        // ===============================
        // CHECK DUPLICATE
        // ===============================
        public async Task<bool> ExistsAsync(string label, Guid? parentId)
        {
            return await _uow.Menus.AnyAsync(x =>
                x.Label == label &&
                x.ParentId == parentId &&
                !x.IsDeleted);
        }

        // ==========================================
        // 🔥 BASE QUERY (CORE OPTIMIZATION)
        // ==========================================
        private IQueryable<MenuDto> BaseQuery()
        {
            return _uow.Menus.Query().AsNoTracking()
                .Where(x => x.IsVisible && x.IsActive && !x.IsDeleted)
                .Select(x => new MenuDto
                {
                    Id = x.Id,
                    Label = x.Label,
                    Route = x.Route,
                    Icon = x.Icon,
                    ParentId = x.ParentId,
                    ModuleId = x.ModuleId,
                    OrderNo = x.OrderNo,

                    MenuType = x.MenuType,
                    Permission = x.Permission,
                    Tooltip = x.Tooltip,

                    IsExternal = x.IsExternal,
                    ExternalUrl = x.ExternalUrl,
                    Target = x.Target,
                    QueryParams = x.QueryParams,

                    Badge = x.Badge,
                    Color = x.Color,
                    CssClass = x.CssClass,

                    IsActive = x.IsActive,
                    IsVisible = x.IsVisible,

                    ShowInSidebar = x.ShowInSidebar,
                    ShowInTopbar = x.ShowInTopbar,

                    IsDefault = x.IsDefault,
                    IsHidden = x.IsHidden,
                    IsSystem = x.IsSystem,

                    DefaultExpanded = x.DefaultExpanded,

                    Children = new List<MenuDto>()
                }).OrderBy(x => x.ParentId).ThenBy(x => x.OrderNo);
        }

        // ===============================
        // TREE BUILDER
        // ===============================
        private List<MenuDto> BuildTree(List<MenuDto> items)
        {
            var dict = new Dictionary<Guid, MenuDto>(items.Count);
            var roots = new List<MenuDto>();

            // prepare dictionary
            foreach (var item in items)
                dict[item.Id] = item;

            foreach (var item in items)
            {
                if (item.ParentId.HasValue && dict.TryGetValue(item.ParentId.Value, out var parent))
                {
                    parent.Children.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            return roots;
        }

        private void SortChildren(IEnumerable<MenuItem> items)
        {
            foreach (var item in items)
            {
                if (item.Children != null && item.Children.Any())
                {
                    item.Children = item.Children.OrderBy(x => x.OrderNo).ToList();
                    SortChildren(item.Children);
                }
            }
        }

        private async Task<bool> IsCircular(Guid id, Guid? parentId)
        {
            while (parentId != null)
            {
                if (parentId == id)
                    return true;

                var parent = await _uow.Menus.GetByIdAsync(parentId.Value);
                parentId = parent?.ParentId;
            }

            return false;
        }
    }
}
