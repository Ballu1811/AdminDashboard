using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ERP.WorkflowwServices.API.Services
{
    public class ModuleService : IModuleService
    {
        private readonly IUnitOfWork _uow;
        private readonly IUserContext _userContext;
        public ModuleService(IUnitOfWork uow, IUserContext userContext)
        {
            _uow = uow;
            _userContext = userContext;
        }

        // ===============================
        // GET ALL
        // ===============================
        public async Task<PagedResult<ModuleDto>> GetAllAsync(FilterModel filter)
        {
            var query = _uow.Modules.Query().IgnoreQueryFilters().AsNoTracking()
                .Where(module => module.TenantId == _userContext.TenantId);

            if (filter.Status == 0)
                query = query.Where(module => !module.IsDeleted && module.IsActive);
            else if (filter.Status == 1)
                query = query.Where(module => module.IsDeleted);

            if (!string.IsNullOrWhiteSpace(filter.KeyWord))
            {
                var keyword = filter.KeyWord.Trim();
                query = query.Where(x =>
                    x.Name.Contains(keyword) ||
                    x.Code.Contains(keyword) ||
                    x.ModuleKey.Contains(keyword) ||
                    (x.Category != null && x.Category.Contains(keyword)) ||
                    (x.Description != null && x.Description.Contains(keyword)));
            }

            query = ApplyColumnFilters(query, filter.Filters);

            var total = await query.CountAsync();

            query = ApplySort(query, filter.SortField ?? filter.SortBy, filter.SortOrder ?? filter.SortDirection);
            if (!filter.ExportExcel)
            {
                var pageNumber = Math.Max(1, filter.PageNumber);
                var pageSize = Math.Max(1, filter.PageSize);
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            var data = await query
                .Select(x => new ModuleDto
                {
                    Id = x.Id,
                    ParentId = x.ParentId,
                    Name = x.Name,
                    Code = x.Code,
                    ModuleKey = x.ModuleKey,
                    ModuleType = x.ModuleType,
                    Version = x.Version,
                    Icon = x.Icon,
                    DefaultRoute = x.DefaultRoute,
                    ComponentKey = x.ComponentKey,
                    ThemeKey = x.ThemeKey,
                    OrderNo = x.OrderNo,

                    IsDefault = x.IsDefault,
                    IsLicensed = x.IsLicensed,
                    IsSystemModule = x.IsSystemModule,
                    ShowInSidebar = x.ShowInSidebar,
                    ShowInTopbar = x.ShowInTopbar,

                    Category = x.Category,
                    Permission = x.Permission,

                    IsActive = x.IsActive,
                    IsVisible = x.IsVisible,
                    IsDeleted = x.IsDeleted,

                    Description = x.Description
                }).ToListAsync();

            return new PagedResult<ModuleDto>
            {
                Total = total,
                Data = data
            };
        }

        private static IQueryable<Module> ApplySort(IQueryable<Module> query, string? sortField, string? sortOrder)
        {
            var descending = !string.IsNullOrWhiteSpace(sortField) &&
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            return sortField?.Trim().ToLowerInvariant() switch
            {
                "name" => descending ? query.OrderByDescending(module => module.Name).ThenBy(module => module.Id) : query.OrderBy(module => module.Name).ThenBy(module => module.Id),
                "code" => descending ? query.OrderByDescending(module => module.Code).ThenBy(module => module.Id) : query.OrderBy(module => module.Code).ThenBy(module => module.Id),
                "category" => descending ? query.OrderByDescending(module => module.Category).ThenBy(module => module.Id) : query.OrderBy(module => module.Category).ThenBy(module => module.Id),
                "icon" => descending ? query.OrderByDescending(module => module.Icon).ThenBy(module => module.Id) : query.OrderBy(module => module.Icon).ThenBy(module => module.Id),
                "islicensed" => descending ? query.OrderByDescending(module => module.IsLicensed).ThenBy(module => module.Id) : query.OrderBy(module => module.IsLicensed).ThenBy(module => module.Id),
                "isactive" => descending ? query.OrderByDescending(module => module.IsActive).ThenBy(module => module.Id) : query.OrderBy(module => module.IsActive).ThenBy(module => module.Id),
                "orderno" => descending ? query.OrderByDescending(module => module.OrderNo).ThenBy(module => module.Id) : query.OrderBy(module => module.OrderNo).ThenBy(module => module.Id),
                _ => descending ? query.OrderByDescending(module => module.OrderNo).ThenBy(module => module.Id) : query.OrderBy(module => module.OrderNo).ThenBy(module => module.Id)
            };
        }

        private static IQueryable<Module> ApplyColumnFilters(
            IQueryable<Module> query,
            Dictionary<string, List<ERP.WorkflowwServices.API.DTOs.FilterModels.GridColumnFilter>>? filters)
        {
            if (filters == null) return query;

            foreach (var (field, constraints) in filters)
            {
                var parameter = Expression.Parameter(typeof(Module), "module");
                Expression? combined = null;
                var useOr = constraints?.Any(item => string.Equals(item.Operator, "or", StringComparison.OrdinalIgnoreCase)) == true;

                foreach (var constraint in constraints ?? new List<ERP.WorkflowwServices.API.DTOs.FilterModels.GridColumnFilter>())
                {
                    var body = BuildFilterExpression(parameter, field, constraint);
                    if (body == null) continue;
                    combined = combined == null
                        ? body
                        : useOr ? Expression.OrElse(combined, body) : Expression.AndAlso(combined, body);
                }

                if (combined != null)
                    query = query.Where(Expression.Lambda<Func<Module, bool>>(combined, parameter));
            }

            return query;
        }

        private static Expression? BuildFilterExpression(
            ParameterExpression parameter,
            string field,
            ERP.WorkflowwServices.API.DTOs.FilterModels.GridColumnFilter filter)
        {
            var json = filter.Value;
            if (json.ValueKind is System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined) return null;

            var mode = (filter.MatchMode ?? "contains").Trim().ToLowerInvariant();
            var key = field.Trim().ToLowerInvariant();
            var propertyName = key switch
            {
                "name" => nameof(Module.Name),
                "code" => nameof(Module.Code),
                "category" => nameof(Module.Category),
                "icon" => nameof(Module.Icon),
                "orderno" => nameof(Module.OrderNo),
                "islicensed" => nameof(Module.IsLicensed),
                "isactive" => nameof(Module.IsActive),
                _ => null
            };
            if (propertyName == null) return null;

            var member = Expression.Property(parameter, propertyName);
            if (member.Type == typeof(string))
            {
                var value = json.ValueKind == System.Text.Json.JsonValueKind.String ? json.GetString() : json.ToString();
                if (string.IsNullOrEmpty(value)) return null;
                var constant = Expression.Constant(value);
                if (mode is "equals" or "isequal" or "is")
                    return Expression.Equal(member, constant);
                if (mode is "notequals" or "isnot")
                    return Expression.NotEqual(member, constant);

                var method = mode switch
                {
                    "startswith" => nameof(string.StartsWith),
                    "endswith" => nameof(string.EndsWith),
                    "notcontains" => nameof(string.Contains),
                    _ => nameof(string.Contains)
                };
                var call = Expression.Call(member, typeof(string).GetMethod(method, new[] { typeof(string) })!, constant);
                var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
                return mode == "notcontains" ? Expression.OrElse(Expression.Not(notNull), Expression.Not(call)) : Expression.AndAlso(notNull, call);
            }

            if (member.Type == typeof(int) && int.TryParse(json.ToString(), out var number))
            {
                var constant = Expression.Constant(number);
                return mode switch
                {
                    "gt" => Expression.GreaterThan(member, constant),
                    "gte" => Expression.GreaterThanOrEqual(member, constant),
                    "lt" => Expression.LessThan(member, constant),
                    "lte" => Expression.LessThanOrEqual(member, constant),
                    "notequals" => Expression.NotEqual(member, constant),
                    _ => Expression.Equal(member, constant)
                };
            }

            if (member.Type == typeof(bool) && bool.TryParse(json.ToString(), out var boolean))
                return Expression.Equal(member, Expression.Constant(boolean));

            return null;
        }

        // ===============================
        // GET BY ID
        // ===============================
        public async Task<ModuleDto?> GetByIdAsync(Guid id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        // ===============================
        // CREATE MODULE
        // ===============================
        public async Task<ModuleDto> CreateAsync(ModuleCreateDto dto)
        {
            await ValidateParentAsync(dto.ParentId, null);
            // 🔥 Duplicate check
            var exists = await _uow.Modules.AnyAsync(x =>
                (x.Name == dto.Name || x.Code == dto.Code || x.ModuleKey == dto.ModuleKey) && !x.IsDeleted);

            if (exists)
                throw new Exception("Module already exists");

            var entity = new Module
            {
                Id = Guid.NewGuid(),
                ParentId = dto.ParentId,

                // 🔹 Basic
                Name = dto.Name,
                Code = dto.Code,
                ModuleKey = dto.ModuleKey,
                ModuleType = dto.ModuleType,
                Version = dto.Version,
                Icon = dto.Icon,
                DefaultRoute = dto.DefaultRoute,
                ComponentKey = dto.ComponentKey,
                ThemeKey = dto.ThemeKey,
                OrderNo = dto.OrderNo,

                // 🔹 Control
                IsLicensed = dto.IsLicensed,
                IsSystemModule = dto.IsSystemModule,
                ShowInSidebar = dto.ShowInSidebar,
                ShowInTopbar = dto.ShowInTopbar,
                Permission = dto.Permission,
                Category = dto.Category,
                Description = dto.Description,

                // 🔹 Visibility
                IsActive = dto.IsActive,
                IsVisible = dto.IsVisible
            };

            await _uow.Modules.AddAsync(entity);
            await _uow.SaveChangesAsync();

            return await GetByIdAsync(entity.Id) ?? throw new Exception("Creation failed");
        }

        // ===============================
        // UPDATE MODULE
        // ===============================
        public async Task UpdateAsync(ModuleUpdateDto dto)
        {
            var entity = await _uow.Modules.GetByIdAsync(dto.Id);

            if (entity == null)
                throw new Exception("Module not found");

            await ValidateParentAsync(dto.ParentId, dto.Id);

            // 🔥 Duplicate check (exclude self)
            var isDuplicate = await _uow.Modules.AnyAsync(x =>
                (x.Name == dto.Name || x.Code == dto.Code || x.ModuleKey == dto.ModuleKey) &&
                x.Id != dto.Id &&
                !x.IsDeleted);

            if (isDuplicate)
                throw new Exception("Module name/code already exists");

            // ✅ Update
            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.ModuleKey = dto.ModuleKey;
            entity.ModuleType = dto.ModuleType;
            entity.Version = dto.Version;
            entity.Icon = dto.Icon;
            entity.DefaultRoute = dto.DefaultRoute;
            entity.ComponentKey = dto.ComponentKey;
            entity.ThemeKey = dto.ThemeKey;
            entity.ParentId = dto.ParentId;
            entity.OrderNo = dto.OrderNo;

            entity.IsLicensed = dto.IsLicensed;
            entity.IsSystemModule = dto.IsSystemModule;
            entity.ShowInSidebar = dto.ShowInSidebar;
            entity.ShowInTopbar = dto.ShowInTopbar;
            entity.Permission = dto.Permission;
            entity.Category = dto.Category;
            entity.Description = dto.Description;

            entity.IsActive = dto.IsActive;
            entity.IsVisible = dto.IsVisible;

            _uow.Modules.Update(entity);
            await _uow.SaveChangesAsync();
        }

        // ===============================
        // DELETE (SOFT DELETE)
        // ===============================
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _uow.Modules.GetByIdAsync(id);

            if (entity == null)
                throw new Exception("Module not found");

            entity.IsDeleted = true;

            _uow.Modules.Update(entity);
            await _uow.SaveChangesAsync();
        }

        // ===============================
        // CHECK DUPLICATE
        // ===============================
        public async Task<bool> ExistsAsync(string name, string code)
        {
            return await _uow.Modules.AnyAsync(x =>
                (x.Name == name || x.Code == code) &&
                !x.IsDeleted);
        }

        // ==========================================
        // 🔥 BASE QUERY (same like MenuService)
        // ==========================================
        private IQueryable<ModuleDto> BaseQuery()
        {
            return _uow.Modules.Query().IgnoreQueryFilters().AsNoTracking()
               .Where(x => x.TenantId == _userContext.TenantId && !x.IsDeleted)
               .Select(x => new ModuleDto
               {
                   Id = x.Id,
                   ParentId = x.ParentId,
                   Name = x.Name,
                   Code = x.Code,
                   ModuleKey = x.ModuleKey,
                   ModuleType = x.ModuleType,
                   Version = x.Version,
                   Icon = x.Icon,
                   DefaultRoute = x.DefaultRoute,
                   ComponentKey = x.ComponentKey,
                   ThemeKey = x.ThemeKey,
                   OrderNo = x.OrderNo,

                   IsDefault = x.IsDefault,
                   IsLicensed = x.IsLicensed,
                   IsSystemModule = x.IsSystemModule,
                   ShowInSidebar = x.ShowInSidebar,
                   ShowInTopbar = x.ShowInTopbar,

                   Category = x.Category,
                   Permission = x.Permission,

                   IsActive = x.IsActive,
                   IsVisible = x.IsVisible,

                   Description = x.Description
               }).OrderBy(x => x.OrderNo);
        }

        private async Task ValidateParentAsync(Guid? parentId, Guid? currentModuleId)
        {
            if (!parentId.HasValue)
                return;
            if (parentId == Guid.Empty || parentId == currentModuleId ||
                !await _uow.Modules.AnyAsync(x => x.Id == parentId.Value && x.IsActive && !x.IsDeleted))
                throw new InvalidOperationException("Select a different active parent module.");
        }
    }
}
