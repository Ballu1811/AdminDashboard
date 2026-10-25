using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _uow;
        private readonly IUserContext _userContext;
        private readonly WorkflowDbContext _context;
        public MenuService(IUnitOfWork uow, IUserContext userContext, WorkflowDbContext context)
        {
            _uow = uow;
            _userContext = userContext;
            _context = context;
        }

        // ===============================
        // GET MENU TREE (Sidebar API)
        // ===============================
        public async Task<List<MenuDto>> GetMenuTreeAsync()
        {
            return await GetPlacementTreeAsync(null);
        }

        public async Task<List<MenuDto>> GetMenuTreeByModuleAsync(Guid moduleId)
        {
            return await GetModuleMenuTreeAsync(moduleId);
        }

        public async Task<List<MenuDto>> GetMenuTreeByModuleNameAsync(string moduleName)
        {
            var normalizedName = moduleName.Trim();
            var moduleId = await _context.Modules.IgnoreQueryFilters().AsNoTracking()
                .Where(module => module.TenantId == _userContext.TenantId &&
                    !module.IsDeleted && module.IsActive && module.IsVisible &&
                    module.Name == normalizedName)
                .Select(module => (Guid?)module.Id)
                .FirstOrDefaultAsync();

            return moduleId.HasValue
                ? await GetModuleMenuTreeAsync(moduleId.Value)
                : new List<MenuDto>();
        }

        private async Task<List<MenuDto>> GetModuleMenuTreeAsync(Guid moduleId)
        {
            var moduleExists = await _context.Modules.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(module => module.Id == moduleId &&
                    module.TenantId == _userContext.TenantId &&
                    !module.IsDeleted && module.IsActive && module.IsVisible);
            if (!moduleExists) return new List<MenuDto>();

            var nodes = await _context.MenuItems.IgnoreQueryFilters().AsNoTracking()
                .Where(menu => menu.TenantId == _userContext.TenantId &&
                    menu.ModuleId == moduleId && !menu.IsDeleted &&
                    menu.IsActive && menu.IsVisible && menu.ShowInSidebar && !menu.IsHidden)
                .Select(menu => new MenuDto
                {
                    Id = menu.Id,
                    MenuItemId = menu.Id,
                    Label = menu.Label,
                    Description = menu.Description,
                    ComponentKey = menu.ComponentKey,
                    IsLicensed = menu.IsLicensed,
                    Route = menu.Route,
                    Icon = menu.Icon,
                    ParentId = menu.ParentId,
                    ModuleId = menu.ModuleId,
                    ModuleName = menu.ModuleMaster == null ? null : menu.ModuleMaster.Name,
                    OrderNo = menu.OrderNo,
                    MenuType = menu.MenuType,
                    Permission = menu.Permission,
                    Tooltip = menu.Tooltip,
                    IsExternal = menu.IsExternal,
                    ExternalUrl = menu.ExternalUrl,
                    Target = menu.Target,
                    QueryParams = menu.QueryParams,
                    Badge = menu.Badge,
                    Color = menu.Color,
                    CssClass = menu.CssClass,
                    IsActive = menu.IsActive,
                    IsVisible = menu.IsVisible,
                    IsDeleted = menu.IsDeleted,
                    ShowInSidebar = menu.ShowInSidebar,
                    ShowInTopbar = menu.ShowInTopbar,
                    IsDefault = menu.IsDefault,
                    IsHidden = menu.IsHidden,
                    IsSystem = menu.IsSystem,
                    DefaultExpanded = menu.DefaultExpanded,
                    Children = new List<MenuDto>()
                })
                .ToListAsync();

            var byId = nodes.ToDictionary(menu => menu.Id);
            foreach (var menu in nodes)
            {
                if (menu.ParentId.HasValue && byId.TryGetValue(menu.ParentId.Value, out var parent))
                    parent.Children.Add(menu);
            }

            var roots = nodes
                .Where(menu => !menu.ParentId.HasValue &&
                    string.Equals(menu.MenuType, "group", StringComparison.OrdinalIgnoreCase))
                .OrderBy(menu => menu.OrderNo)
                .ThenBy(menu => menu.Label)
                .ToList();

            SortMenuTree(roots);
            ComposeMenuRoutes(roots, null);
            return roots;
        }

        public async Task<List<SidebarModuleDto>> GetSidebarModulesAsync()
        {
            return await _context.Modules.IgnoreQueryFilters().AsNoTracking()
                .Where(module => module.TenantId == _userContext.TenantId &&
                    !module.IsDeleted && module.IsActive && module.IsVisible && module.ShowInSidebar)
                .OrderBy(module => module.OrderNo)
                .ThenBy(module => module.Name)
                .Select(module => new SidebarModuleDto
                {
                    Id = module.Id,
                    Name = module.Name,
                    Icon = module.Icon,
                    DefaultRoute = module.DefaultRoute,
                    ComponentKey = module.ComponentKey,
                    Category = module.Category,
                    OrderNo = module.OrderNo
                })
                .ToListAsync();
        }


        // ======================================================
        // GET ALL (FLAT LIST)
        // ======================================================
        public async Task<PagedResult<MenuDto>> GetAllAsync(FilterModel filter)
        {
            var query = _uow.Menus.Query().IgnoreQueryFilters().AsNoTracking()
                .Where(menu => menu.TenantId == _userContext.TenantId);

            if (filter.Status == 0)
                query = query.Where(menu => !menu.IsDeleted && menu.IsActive);
            else if (filter.Status == 1)
                query = query.Where(menu => menu.IsDeleted);

            if (filter.ModuleId.HasValue) query = query.Where(menu => menu.ModuleId == filter.ModuleId);
            if (filter.ParentId.HasValue) query = query.Where(menu => menu.ParentId == filter.ParentId);
            query = ApplyGridFilters(query, filter.Filters);

            if (filter.HaveFilter && !string.IsNullOrWhiteSpace(filter.KeyWord))
            {
                var keyword = filter.KeyWord.Trim();
                query = query.Where(x =>
                    x.Label.Contains(keyword) ||
                    x.Code.Contains(keyword) ||
                    x.MenuKey.Contains(keyword) ||
                    (x.Route != null && x.Route.Contains(keyword)) ||
                    (x.Parent != null && x.Parent.TenantId == _userContext.TenantId && x.Parent.Label.Contains(keyword)) ||
                    (x.ModuleMaster != null && x.ModuleMaster.TenantId == _userContext.TenantId && x.ModuleMaster.Name.Contains(keyword)));
            }

            var total = await query.CountAsync();
            var sortBy = filter.SortField ?? filter.SortBy;
            var descending = !string.IsNullOrWhiteSpace(sortBy) &&
                string.Equals(filter.SortOrder ?? filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            query = sortBy?.ToLowerInvariant() switch
            {
                "name" or "label" => descending ? query.OrderByDescending(x => x.Label).ThenBy(x => x.Id) : query.OrderBy(x => x.Label).ThenBy(x => x.Id),
                "code" => descending ? query.OrderByDescending(x => x.Code).ThenBy(x => x.Id) : query.OrderBy(x => x.Code).ThenBy(x => x.Id),
                "orderno" => descending ? query.OrderByDescending(x => x.OrderNo).ThenBy(x => x.Id) : query.OrderBy(x => x.OrderNo).ThenBy(x => x.Id),
                "menutype" => descending ? query.OrderByDescending(x => x.MenuType).ThenBy(x => x.Id) : query.OrderBy(x => x.MenuType).ThenBy(x => x.Id),
                "isactive" => descending ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Id) : query.OrderBy(x => x.IsActive).ThenBy(x => x.Id),
                "route" => descending ? query.OrderByDescending(x => x.Route).ThenBy(x => x.Id) : query.OrderBy(x => x.Route).ThenBy(x => x.Id),
                "module" or "modulename" => descending ? query.OrderByDescending(x => x.ModuleMaster!.Name).ThenBy(x => x.Id) : query.OrderBy(x => x.ModuleMaster!.Name).ThenBy(x => x.Id),
                "parentmenuname" => descending ? query.OrderByDescending(x => x.Parent!.Label).ThenBy(x => x.Id) : query.OrderBy(x => x.Parent!.Label).ThenBy(x => x.Id),
                _ => query.OrderBy(x => x.OrderNo).ThenBy(x => x.Label).ThenBy(x => x.Id)
            };

            if (!filter.ExportExcel)
            {
                var pageNumber = Math.Max(1, filter.PageNumber);
                var pageSize = Math.Clamp(filter.PageSize, 1, 1000);
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            // ✅ Projection (DTO)
            var data = await query
                .Select(x => new MenuDto
                {
                    Id = x.Id,
                    Label = x.Label,
                    Description = x.Description,
                    ComponentKey = x.ComponentKey,
                    IsLicensed = x.IsLicensed,
                    Route = x.Route,
                    Icon = x.Icon,
                    ParentId = x.ParentId,
                    ModuleId = x.ModuleId,
                    ModuleName = x.ModuleMaster == null || x.ModuleMaster.TenantId != _userContext.TenantId ? null : x.ModuleMaster.Name,
                    ParentMenuName = x.Parent == null || x.Parent.TenantId != _userContext.TenantId ? null : x.Parent.Label,
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
                    IsDeleted = x.IsDeleted,

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
            var menuType = NormalizeMenuType(dto.MenuType);
            ValidateRoute(menuType, dto.Route);
            ValidateMenuFields(dto.Label, dto.Description, dto.ComponentKey, dto.Icon, dto.Route);
            await ValidateMenuHierarchyAsync(dto.ModuleId, dto.ParentId, dto.OrderNo, menuType);

            var menuId = Guid.NewGuid();

            var entity = new MenuItem
            {
                Id = menuId,

                // 🔹 Basic
                Label = dto.Label.Trim(),
                Code = $"MENU_{menuId:N}",
                MenuKey = $"menu_{menuId:N}",
                Description = dto.Description,
                ComponentKey = dto.ComponentKey,
                IsLicensed = dto.IsLicensed,
                Route = menuType == "separator" ? null : NormalizeRoute(dto.Route),
                Icon = dto.Icon,
                ParentId = dto.ParentId,
                OrderNo = dto.OrderNo,

                // 🔹 Behavior
                MenuType = menuType,
                Permission = dto.Permission,
                Tooltip = dto.Tooltip,

                // 🔹 External
                IsExternal = dto.IsExternal,
                ExternalUrl = dto.ExternalUrl,
                Target = dto.Target ?? "_self",

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
            _context.MenuPlacements.Add(new MenuPlacement
            {
                Id = Guid.NewGuid(),
                TenantId = _userContext.TenantId,
                ModuleId = dto.ModuleId,
                MenuId = entity.Id,
                ParentMenuId = dto.ParentId,
                OrderNo = dto.OrderNo
            });
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

            var previousModuleId = entity.ModuleId;

            var menuType = NormalizeMenuType(model.MenuType);
            ValidateRoute(menuType, model.Route);
            ValidateMenuFields(model.Label, model.Description, model.ComponentKey, model.Icon, model.Route);
            await ValidateMenuHierarchyAsync(model.ModuleId, model.ParentId, model.OrderNo, menuType, model.Id);

            // ✅ Basic
            entity.Label = model.Label.Trim();
            entity.Description = model.Description;
            entity.ComponentKey = model.ComponentKey;
            entity.IsLicensed = model.IsLicensed;
            entity.ModuleId = model.ModuleId;
            entity.ParentId = model.ParentId;
            entity.OrderNo = model.OrderNo;
            entity.Icon = model.Icon;
            entity.Route = menuType == "separator" ? null : NormalizeRoute(model.Route);

            // ✅ Behavior
            entity.Permission = model.Permission;
            entity.MenuType = menuType;
            entity.Tooltip = model.Tooltip;

            // ✅ External
            entity.IsExternal = model.IsExternal;
            entity.ExternalUrl = model.ExternalUrl;
            entity.Target = model.Target ?? "_self";

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

            try
            {
                _uow.Menus.Update(entity);
                await SyncPrimaryPlacementAsync(
                    entity.Id,
                    previousModuleId,
                    model.ModuleId,
                    model.ParentId,
                    model.OrderNo);
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private async Task SyncPrimaryPlacementAsync(
            Guid menuId,
            Guid? previousModuleId,
            Guid? targetModuleId,
            Guid? parentMenuId,
            int orderNo)
        {
            var placements = _context.MenuPlacements.IgnoreQueryFilters()
                .Where(placement => placement.TenantId == _userContext.TenantId &&
                    placement.MenuId == menuId && !placement.IsDeleted);

            var currentPlacement = await placements.FirstOrDefaultAsync(placement =>
                placement.ModuleId == previousModuleId);
            var targetPlacement = previousModuleId == targetModuleId
                ? currentPlacement
                : await placements.FirstOrDefaultAsync(placement => placement.ModuleId == targetModuleId);

            if (targetPlacement != null)
            {
                targetPlacement.ParentMenuId = parentMenuId;
                targetPlacement.OrderNo = orderNo;
                if (currentPlacement != null && currentPlacement.Id != targetPlacement.Id)
                    _context.MenuPlacements.Remove(currentPlacement);
                return;
            }

            if (currentPlacement != null)
            {
                currentPlacement.ModuleId = targetModuleId;
                currentPlacement.ParentMenuId = parentMenuId;
                currentPlacement.OrderNo = orderNo;
                return;
            }

            _context.MenuPlacements.Add(new MenuPlacement
            {
                Id = Guid.NewGuid(),
                TenantId = _userContext.TenantId,
                ModuleId = targetModuleId,
                MenuId = menuId,
                ParentMenuId = parentMenuId,
                OrderNo = orderNo
            });
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
        public async Task<bool> ExistsAsync(string label, Guid? moduleId, Guid? parentId, Guid? excludeId = null)
        {
            return await _context.MenuItems.IgnoreQueryFilters().AnyAsync(menu =>
                menu.TenantId == _userContext.TenantId &&
                menu.Id != excludeId &&
                menu.Label == label.Trim() &&
                menu.ModuleId == moduleId &&
                menu.ParentId == parentId &&
                !menu.IsDeleted);
        }

        public async Task<List<MenuPlacementDto>> GetPlacementsAsync()
        {
            return await PlacementQuery()
                .AsNoTracking()
                .OrderBy(placement => placement.Module == null ? string.Empty : placement.Module.Name)
                .ThenBy(placement => placement.ParentMenu == null ? string.Empty : placement.ParentMenu.Label)
                .ThenBy(placement => placement.OrderNo)
                .Select(placement => new MenuPlacementDto
                {
                    Id = placement.Id,
                    MenuId = placement.MenuId,
                    MenuLabel = placement.Menu!.Label,
                    ModuleId = placement.ModuleId,
                    ModuleName = placement.Module == null ? "Global" : placement.Module.Name,
                    ParentMenuId = placement.ParentMenuId,
                    ParentMenuLabel = placement.ParentMenu == null ? null : placement.ParentMenu.Label,
                    OrderNo = placement.OrderNo
                })
                .ToListAsync();
        }

        public async Task<MenuPlacementDto?> GetPlacementByIdAsync(Guid id)
        {
            return await PlacementQuery()
                .AsNoTracking()
                .Where(placement => placement.Id == id)
                .Select(placement => new MenuPlacementDto
                {
                    Id = placement.Id,
                    MenuId = placement.MenuId,
                    MenuLabel = placement.Menu!.Label,
                    ModuleId = placement.ModuleId,
                    ModuleName = placement.Module == null ? "Global" : placement.Module.Name,
                    ParentMenuId = placement.ParentMenuId,
                    ParentMenuLabel = placement.ParentMenu == null ? null : placement.ParentMenu.Label,
                    OrderNo = placement.OrderNo
                })
                .FirstOrDefaultAsync();
        }

        public async Task<MenuPlacementDto> CreatePlacementAsync(MenuPlacementCreateDto model)
        {
            await ValidatePlacementAsync(model.MenuId, model.ModuleId, model.ParentMenuId, model.OrderNo);

            if (await PlacementQuery().AnyAsync(placement =>
                placement.MenuId == model.MenuId && placement.ModuleId == model.ModuleId))
                throw new InvalidOperationException("This menu is already linked to the selected module");

            var placement = new MenuPlacement
            {
                Id = Guid.NewGuid(),
                MenuId = model.MenuId,
                ModuleId = model.ModuleId,
                ParentMenuId = model.ParentMenuId,
                OrderNo = model.OrderNo
            };

            await _context.MenuPlacements.AddAsync(placement);
            await _context.SaveChangesAsync();

            return await GetPlacementByIdAsync(placement.Id)
                ?? throw new InvalidOperationException("Menu link creation failed");
        }

        public async Task UpdatePlacementAsync(MenuPlacementUpdateDto model)
        {
            var placement = await PlacementQuery()
                .FirstOrDefaultAsync(item => item.Id == model.Id);

            if (placement == null)
                throw new KeyNotFoundException("Menu link not found");

            await ValidatePlacementAsync(model.MenuId, model.ModuleId, model.ParentMenuId, model.OrderNo, model.Id);

            if (placement.ModuleId != model.ModuleId && await PlacementQuery().AnyAsync(item =>
                item.ModuleId == placement.ModuleId && item.ParentMenuId == placement.MenuId))
                throw new InvalidOperationException("Move or unlink child menus before changing this module");

            if (await PlacementQuery().AnyAsync(item =>
                item.Id != model.Id && item.MenuId == model.MenuId && item.ModuleId == model.ModuleId))
                throw new InvalidOperationException("This menu is already linked to the selected module");

            placement.MenuId = model.MenuId;
            placement.ModuleId = model.ModuleId;
            placement.ParentMenuId = model.ParentMenuId;
            placement.OrderNo = model.OrderNo;
            await _context.SaveChangesAsync();
        }

        public async Task DeletePlacementAsync(Guid id)
        {
            var placement = await PlacementQuery().FirstOrDefaultAsync(item => item.Id == id);
            if (placement == null)
                throw new KeyNotFoundException("Menu link not found");

            if (await PlacementQuery().AnyAsync(item =>
                item.ModuleId == placement.ModuleId && item.ParentMenuId == placement.MenuId))
                throw new InvalidOperationException("Move or unlink child menus before removing this parent");

            placement.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // 🔥 BASE QUERY (CORE OPTIMIZATION)
        // ==========================================
        private IQueryable<MenuDto> BaseQuery()
        {
            return _uow.Menus.Query().IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.TenantId == _userContext.TenantId)
                .Select(x => new MenuDto
                {
                    Id = x.Id,
                    Label = x.Label,
                    Description = x.Description,
                    ComponentKey = x.ComponentKey,
                    IsLicensed = x.IsLicensed,
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
                    IsDeleted = x.IsDeleted,

                    ShowInSidebar = x.ShowInSidebar,
                    ShowInTopbar = x.ShowInTopbar,

                    IsDefault = x.IsDefault,
                    IsHidden = x.IsHidden,
                    IsSystem = x.IsSystem,

                    DefaultExpanded = x.DefaultExpanded,

                    Children = new List<MenuDto>()
                }).OrderBy(x => x.ParentId).ThenBy(x => x.OrderNo);
        }

        private IQueryable<MenuPlacement> PlacementQuery()
        {
            return _context.MenuPlacements.IgnoreQueryFilters()
                .Where(placement => placement.TenantId == _userContext.TenantId &&
                    !placement.IsDeleted &&
                    placement.Menu != null &&
                    placement.Menu.TenantId == _userContext.TenantId &&
                    !placement.Menu.IsDeleted);
        }

        private async Task<List<MenuDto>> GetPlacementTreeAsync(Guid? moduleId)
        {
            var query = PlacementQuery()
                .Where(placement => placement.Menu!.IsActive && placement.Menu.IsVisible &&
                    placement.Menu.ShowInSidebar && !placement.Menu.IsHidden &&
                    (!placement.ModuleId.HasValue || placement.Module != null &&
                        placement.Module.TenantId == _userContext.TenantId && !placement.Module.IsDeleted &&
                        placement.Module.IsActive && placement.Module.IsVisible));

            if (moduleId.HasValue)
            {
                query = query.Where(placement => placement.ModuleId == moduleId || placement.ModuleId == null);
            }

            var placements = await query
                .Include(placement => placement.Menu)
                .Include(placement => placement.Module)
                .OrderBy(placement => placement.ModuleId)
                .ThenBy(placement => placement.OrderNo)
                .ThenBy(placement => placement.Menu!.Label)
                .ToListAsync();

            var placementByMenu = placements.ToDictionary(
                placement => (placement.ModuleId, placement.MenuId));

            var nodes = placements.Select(placement =>
            {
                var parentId = (Guid?)null;
                if (placement.ParentMenuId.HasValue)
                {
                    if (placementByMenu.TryGetValue((placement.ModuleId, placement.ParentMenuId.Value), out var parent) ||
                        placement.ModuleId.HasValue && placementByMenu.TryGetValue((null, placement.ParentMenuId.Value), out parent))
                        parentId = parent.Id;
                }

                var menu = placement.Menu!;
                return new MenuDto
                {
                    Id = placement.Id,
                    MenuItemId = menu.Id,
                    Label = menu.Label,
                    Description = menu.Description,
                    ComponentKey = menu.ComponentKey,
                    IsLicensed = menu.IsLicensed,
                    Route = menu.Route,
                    Icon = menu.Icon,
                    ParentId = parentId,
                    ModuleId = placement.ModuleId,
                    ModuleName = placement.Module?.Name,
                    OrderNo = placement.OrderNo,
                    MenuType = menu.MenuType,
                    Permission = menu.Permission,
                    Tooltip = menu.Tooltip,
                    IsExternal = menu.IsExternal,
                    ExternalUrl = menu.ExternalUrl,
                    Target = menu.Target,
                    QueryParams = menu.QueryParams,
                    Badge = menu.Badge,
                    Color = menu.Color,
                    CssClass = menu.CssClass,
                    IsActive = menu.IsActive,
                    IsVisible = menu.IsVisible,
                    IsDeleted = menu.IsDeleted,
                    ShowInSidebar = menu.ShowInSidebar,
                    ShowInTopbar = menu.ShowInTopbar,
                    IsDefault = menu.IsDefault,
                    IsHidden = menu.IsHidden,
                    IsSystem = menu.IsSystem,
                    DefaultExpanded = menu.DefaultExpanded,
                    Children = new List<MenuDto>()
                };
            }).ToList();

            return BuildTree(nodes);
        }

        private async Task ValidatePlacementAsync(Guid menuId, Guid? moduleId, Guid? parentMenuId, int orderNo, Guid? currentPlacementId = null)
        {
            if (orderNo < 0)
                throw new InvalidOperationException("Display position cannot be negative");

            if (moduleId.HasValue && !await _context.Modules.IgnoreQueryFilters().AnyAsync(module =>
                module.Id == moduleId && module.TenantId == _userContext.TenantId &&
                !module.IsDeleted && module.IsActive && module.IsVisible))
                throw new KeyNotFoundException("Module not found or inactive");

            var menu = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(item =>
                item.Id == menuId && item.TenantId == _userContext.TenantId &&
                !item.IsDeleted && item.IsActive && item.IsVisible);
            if (menu == null)
                throw new KeyNotFoundException("Menu not found or inactive");

            if (parentMenuId == menuId)
                throw new InvalidOperationException("A menu cannot be its own parent");

            if (parentMenuId.HasValue)
            {
                var parentPlacement = await PlacementQuery()
                    .Include(item => item.Menu)
                    .FirstOrDefaultAsync(item =>
                        (item.ModuleId == moduleId || item.ModuleId == null) &&
                        item.MenuId == parentMenuId && item.Id != currentPlacementId);

                if (parentPlacement?.Menu == null || !parentPlacement.Menu.IsActive ||
                    !parentPlacement.Menu.IsVisible || parentPlacement.Menu.IsHidden)
                    throw new InvalidOperationException("Parent menu must already be linked to the selected module");
                if (string.Equals(parentPlacement.Menu.MenuType, "separator", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("A separator cannot contain submenu items");

                var ancestorId = parentPlacement.ParentMenuId;
                while (ancestorId.HasValue)
                {
                    if (ancestorId == menuId)
                        throw new InvalidOperationException("Circular menu hierarchy detected");

                    ancestorId = await PlacementQuery()
                        .Where(item => (item.ModuleId == moduleId || item.ModuleId == null) && item.MenuId == ancestorId)
                        .OrderBy(item => item.ModuleId == moduleId ? 0 : 1)
                        .Select(item => item.ParentMenuId)
                        .FirstOrDefaultAsync();
                }
            }
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

            SortMenuTree(roots);
            ComposeMenuRoutes(roots, null);
            return roots;
        }

        private static void SortMenuTree(IEnumerable<MenuDto> items)
        {
            foreach (var item in items)
            {
                item.Children = item.Children
                    .OrderBy(child => child.OrderNo)
                    .ThenBy(child => child.Label)
                    .ToList();
                SortMenuTree(item.Children);
            }
        }

        private static void ComposeMenuRoutes(IEnumerable<MenuDto> items, string? parentRoute)
        {
            foreach (var item in items)
            {
                var fullRoute = CombineMenuRoutes(parentRoute, item.Route);
                if (!string.IsNullOrWhiteSpace(item.Route))
                    item.Route = fullRoute;

                ComposeMenuRoutes(item.Children, fullRoute ?? parentRoute);
            }
        }

        private static string? CombineMenuRoutes(string? parentRoute, string? route)
        {
            var segment = route?.Trim().Trim('/');
            var parent = parentRoute?.Trim().Trim('/');

            if (string.IsNullOrEmpty(segment))
                return string.IsNullOrEmpty(parent) ? null : $"/{parent}";

            if (string.IsNullOrEmpty(parent))
                return $"/{segment}";

            if (segment.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
                segment.StartsWith($"{parent}/", StringComparison.OrdinalIgnoreCase))
                return $"/{segment}";

            return $"/{parent}/{segment}";
        }

        private static string NormalizeMenuType(string? menuType)
        {
            var normalized = string.IsNullOrWhiteSpace(menuType)
                ? "link"
                : menuType.Trim().ToLowerInvariant();

            // Menu Master calls navigable records "Page"; the existing navigation
            // contract calls the same item a "link".
            if (normalized == "page") normalized = "link";

            if (normalized is not ("link" or "group" or "separator"))
                throw new Exception("Menu type must be Link, Group, or Separator");

            return normalized;
        }

        private static void ValidateRoute(string menuType, string? route)
        {
            if (menuType == "link" && string.IsNullOrWhiteSpace(route))
                throw new Exception("Route is required for link menu");
        }

        private static void ValidateMenuFields(string? label, string? description, string? componentKey, string? icon, string? route)
        {
            if (string.IsNullOrWhiteSpace(label) || label.Trim().Length > 100)
                throw new InvalidOperationException("Menu name is required and cannot exceed 100 characters");
            if (description?.Length > 500) throw new InvalidOperationException("Description cannot exceed 500 characters");
            if (componentKey?.Length > 150) throw new InvalidOperationException("Component key cannot exceed 150 characters");
            if (icon?.Length > 100) throw new InvalidOperationException("Icon class cannot exceed 100 characters");
            if (route?.Length > 250) throw new InvalidOperationException("Route cannot exceed 250 characters");
        }

        private static string? NormalizeRoute(string? route)
        {
            return string.IsNullOrWhiteSpace(route) ? null : route.Trim();
        }

        private static IQueryable<MenuItem> ApplyGridFilters(IQueryable<MenuItem> query,
            Dictionary<string, List<ERP.WorkflowwServices.API.DTOs.FilterModels.GridColumnFilter>>? filters)
        {
            if (filters == null) return query;
            foreach (var (field, constraints) in filters)
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(MenuItem), "menu");
                System.Linq.Expressions.Expression? combined = null;
                var useOr = constraints?.Any(item => string.Equals(item.Operator, "or", StringComparison.OrdinalIgnoreCase)) == true;
                foreach (var constraint in constraints ?? new List<ERP.WorkflowwServices.API.DTOs.FilterModels.GridColumnFilter>())
                {
                    var body = BuildMenuFilterExpression(parameter, field, constraint);
                    if (body == null) continue;
                    combined = combined == null ? body : useOr
                        ? System.Linq.Expressions.Expression.OrElse(combined, body)
                        : System.Linq.Expressions.Expression.AndAlso(combined, body);
                }
                if (combined != null)
                    query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<MenuItem, bool>>(combined, parameter));
            }
            return query;
        }

        private static System.Linq.Expressions.Expression? BuildMenuFilterExpression(
            System.Linq.Expressions.ParameterExpression parameter, string field,
            ERP.WorkflowwServices.API.DTOs.FilterModels.GridColumnFilter filter)
        {
            var json = filter.Value;
            if (json.ValueKind is System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined) return null;
            var value = json.ValueKind == System.Text.Json.JsonValueKind.String ? json.GetString() ?? string.Empty : json.ToString();
            var mode = (filter.MatchMode ?? "contains").Trim().ToLowerInvariant();
            var key = field.Trim().ToLowerInvariant();
            System.Linq.Expressions.Expression member;
            switch (key)
            {
                case "label": member = System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.Label)); break;
                case "code": member = System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.Code)); break;
                case "menukey": member = System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.MenuKey)); break;
                case "route": member = System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.Route)); break;
                case "parentmenuname":
                    member = System.Linq.Expressions.Expression.Condition(
                        System.Linq.Expressions.Expression.AndAlso(
                            System.Linq.Expressions.Expression.NotEqual(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.Parent)), System.Linq.Expressions.Expression.Constant(null, typeof(MenuItem))),
                            System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.Parent)), nameof(MenuItem.TenantId)), System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.TenantId)))),
                        System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.Parent)), nameof(MenuItem.Label)),
                        System.Linq.Expressions.Expression.Constant(null, typeof(string)));
                    break;
                case "modulename": case "module":
                    member = System.Linq.Expressions.Expression.Condition(
                        System.Linq.Expressions.Expression.AndAlso(
                            System.Linq.Expressions.Expression.NotEqual(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.ModuleMaster)), System.Linq.Expressions.Expression.Constant(null, typeof(Module))),
                            System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.ModuleMaster)), nameof(Module.TenantId)), System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.TenantId)))),
                        System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.ModuleMaster)), nameof(Module.Name)),
                        System.Linq.Expressions.Expression.Constant(null, typeof(string)));
                    break;
                case "menutype":
                    var menuType = value.Equals("page", StringComparison.OrdinalIgnoreCase) ? "link" : value.ToLowerInvariant();
                    member = System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.MenuType));
                    value = menuType;
                    break;
                case "orderno":
                    if (!int.TryParse(value, out var order)) return null;
                    member = System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.OrderNo));
                    var orderConstant = System.Linq.Expressions.Expression.Constant(order);
                    return mode switch
                    {
                        "gt" => System.Linq.Expressions.Expression.GreaterThan(member, orderConstant),
                        "gte" => System.Linq.Expressions.Expression.GreaterThanOrEqual(member, orderConstant),
                        "lt" => System.Linq.Expressions.Expression.LessThan(member, orderConstant),
                        "lte" => System.Linq.Expressions.Expression.LessThanOrEqual(member, orderConstant),
                        _ => System.Linq.Expressions.Expression.Equal(member, orderConstant)
                    };
                case "isactive":
                    if (!bool.TryParse(value, out var active)) return null;
                    return System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Property(parameter, nameof(MenuItem.IsActive)), System.Linq.Expressions.Expression.Constant(active));
                default: return null;
            }

            var constant = System.Linq.Expressions.Expression.Constant(value);
            if (mode is "equals" or "isequal" or "is") return System.Linq.Expressions.Expression.Equal(member, constant);
            if (mode is "notequals" or "isnot") return System.Linq.Expressions.Expression.NotEqual(member, constant);
            var methodName = mode switch
            {
                "startswith" => nameof(string.StartsWith), "endswith" => nameof(string.EndsWith), _ => nameof(string.Contains)
            };
            var call = System.Linq.Expressions.Expression.Call(member, typeof(string).GetMethod(methodName, new[] { typeof(string) })!, constant);
            var notNull = System.Linq.Expressions.Expression.NotEqual(member, System.Linq.Expressions.Expression.Constant(null, typeof(string)));
            var matches = System.Linq.Expressions.Expression.AndAlso(notNull, call);
            return mode == "notcontains" ? System.Linq.Expressions.Expression.Not(matches) : matches;
        }

        private async Task ValidateMenuHierarchyAsync(Guid? moduleId, Guid? parentId, int orderNo,
            string menuType, Guid? currentId = null)
        {
            if (!moduleId.HasValue || !await _context.Modules.IgnoreQueryFilters().AnyAsync(module =>
                module.Id == moduleId && module.TenantId == _userContext.TenantId &&
                !module.IsDeleted && module.IsActive))
                throw new InvalidOperationException("Select an active module");
            if (orderNo < 0) throw new InvalidOperationException("Display order cannot be negative");
            var tenantMenus = _context.MenuItems.IgnoreQueryFilters().Where(menu =>
                menu.TenantId == _userContext.TenantId && !menu.IsDeleted);

            if (!parentId.HasValue) return;
            if (parentId == currentId) throw new InvalidOperationException("A menu cannot be its own parent");
            var allTenantMenus = _context.MenuItems.IgnoreQueryFilters().Where(menu =>
                menu.TenantId == _userContext.TenantId);
            var parent = await allTenantMenus.FirstOrDefaultAsync(menu => menu.Id == parentId);
            var currentParentId = currentId.HasValue
                ? await allTenantMenus.Where(menu => menu.Id == currentId).Select(menu => menu.ParentId).FirstOrDefaultAsync()
                : null;
            var isExistingParent = currentId.HasValue && currentParentId == parentId;
            if (parent == null || ((parent.IsDeleted || !parent.IsActive || !parent.IsVisible) && !isExistingParent))
                throw new InvalidOperationException("Parent menu must exist and be active");
            if (parent.ModuleId != moduleId)
                throw new InvalidOperationException("Parent and child menus must belong to the same module");
            if (parent.MenuType.Equals("separator", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A separator cannot contain child menus");

            var ancestorId = parent.ParentId;
            var visited = new HashSet<Guid> { parent.Id };
            while (ancestorId.HasValue)
            {
                if (ancestorId == currentId || !visited.Add(ancestorId.Value))
                    throw new InvalidOperationException("Circular menu hierarchy detected");
                ancestorId = await tenantMenus.Where(menu => menu.Id == ancestorId.Value)
                    .Select(menu => menu.ParentId).FirstOrDefaultAsync();
            }
        }

    }
}
