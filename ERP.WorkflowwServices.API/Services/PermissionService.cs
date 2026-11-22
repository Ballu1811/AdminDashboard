using System.Data;
using System.Text.RegularExpressions;
using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class PermissionService(WorkflowDbContext db, IUserContext user) : IPermissionService
{
    private const int MaximumBatchSize = 1000;
    private static readonly Regex PermissionKeyPattern = new("^[A-Z0-9][A-Z0-9._-]*$", RegexOptions.Compiled);

    public async Task<PagedResult<PermissionListDto>> GetAllAsync(PermissionFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Permissions.IgnoreQueryFilters().AsNoTracking()
            .Where(permission => permission.TenantId == user.TenantId);

        if (filter.Status == 0) query = query.Where(permission => !permission.IsDeleted && permission.IsActive);
        else if (filter.Status == 1) query = query.Where(permission => permission.IsDeleted);
        else if (filter.Status == 2) query = query.Where(permission => !permission.IsDeleted && !permission.IsActive);
        else if (filter.Status.HasValue) throw new InvalidOperationException("Permission status filter is invalid.");

        if (filter.ModuleId.HasValue) query = query.Where(permission => permission.ModuleId == filter.ModuleId.Value);
        if (filter.SubModuleId.HasValue) query = query.Where(permission => permission.SubModuleId == filter.SubModuleId.Value);
        if (filter.MenuId.HasValue) query = query.Where(permission => permission.MenuId == filter.MenuId.Value);
        if (!string.IsNullOrWhiteSpace(filter.PermissionType))
        {
            var type = PermissionTypeCatalog.Normalize(filter.PermissionType);
            query = query.Where(permission => permission.PermissionType == type);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(permission =>
                permission.PermissionName.Contains(search) ||
                permission.PermissionKey.Contains(search) ||
                (permission.Description != null && permission.Description.Contains(search)));
        }

        query = ApplySort(query, filter.SortField, filter.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 250);
        var records = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(permission => permission.Module)
            .Include(permission => permission.SubModule)
            .Include(permission => permission.Menu)
            .ToListAsync(cancellationToken);

        return new PagedResult<PermissionListDto>
        {
            Total = total,
            Data = records.Select(ToListDto).ToList()
        };
    }

    public async Task<PermissionDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await db.Permissions.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Id == id && item.TenantId == user.TenantId)
            .Include(item => item.Module)
            .Include(item => item.SubModule)
            .Include(item => item.Menu)
            .FirstOrDefaultAsync(cancellationToken);

        return permission is null ? null : ToDetailsDto(permission);
    }

    public async Task<PermissionDto> CreateAsync(PermissionSaveRequest request, CancellationToken cancellationToken = default) =>
        await SaveAsync(request, null, cancellationToken);

    public async Task<PermissionDto?> UpdateAsync(Guid id, PermissionSaveRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await db.Permissions.IgnoreQueryFilters().AnyAsync(permission =>
            permission.Id == id && permission.TenantId == user.TenantId && !permission.IsDeleted, cancellationToken);
        if (!exists) return null;
        return await SaveAsync(request, id, cancellationToken);
    }

    private async Task<PermissionDto> SaveAsync(PermissionSaveRequest request, Guid? id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var name = request.PermissionName?.Trim() ?? string.Empty;
        var key = request.PermissionKey?.Trim().ToUpperInvariant() ?? string.Empty;
        var type = PermissionTypeCatalog.Normalize(request.PermissionType);
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
            throw new InvalidOperationException("Permission name is required and must be at most 150 characters.");
        if (string.IsNullOrWhiteSpace(key) || key.Length > 150 || !PermissionKeyPattern.IsMatch(key))
            throw new InvalidOperationException("Permission key must be at most 150 characters and contain only letters, numbers, periods, underscores, or hyphens.");
        if (request.SortOrder < 0)
            throw new InvalidOperationException("Sort order cannot be negative.");
        if (description?.Length > 500)
            throw new InvalidOperationException("Description must be at most 500 characters.");

        await ValidateMappingAsync(request.ModuleId, request.SubModuleId, request.MenuId, cancellationToken);

        if (await db.Permissions.IgnoreQueryFilters().AnyAsync(permission =>
            permission.TenantId == user.TenantId && !permission.IsDeleted &&
            permission.PermissionKey == key && permission.Id != id, cancellationToken))
            throw new InvalidOperationException("A permission with this key already exists in this tenant.");

        Permission? entity = null;
        if (id.HasValue)
        {
            entity = await db.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(permission =>
                permission.Id == id.Value && permission.TenantId == user.TenantId && !permission.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Permission was not found.");
            if (entity.IsSystemDefined)
                throw new InvalidOperationException("System-defined permissions cannot be edited.");
        }

        entity ??= new Permission
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId
        };
        entity.ModuleId = request.ModuleId;
        entity.SubModuleId = request.SubModuleId;
        entity.MenuId = request.MenuId;
        entity.PermissionName = name;
        entity.PermissionKey = key;
        entity.PermissionType = type;
        entity.Description = description;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        if (!id.HasValue) db.Permissions.Add(entity);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var details = await GetByIdAsync(entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Permission was saved but could not be loaded.");
        return ToDto(details);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await FindIncludingArchivedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Permission was not found.");
        if (permission.IsDeleted) throw new InvalidOperationException("Permission is already archived.");
        if (permission.IsSystemDefined) throw new InvalidOperationException("System-defined permissions cannot be archived.");

        permission.IsDeleted = true;
        permission.DeletedAt = DateTime.UtcNow;
        permission.DeletedBy = user.UserId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var permission = await FindIncludingArchivedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Permission was not found.");
        if (!permission.IsDeleted) throw new InvalidOperationException("Permission is not archived.");
        if (await HasActiveKeyConflictAsync(permission.PermissionKey, permission.Id, cancellationToken))
            throw new InvalidOperationException("Another active permission already uses this key in the tenant.");

        await ValidateMappingAsync(permission.ModuleId, permission.SubModuleId, permission.MenuId, cancellationToken);
        permission.IsDeleted = false;
        permission.DeletedAt = null;
        permission.DeletedBy = null;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> BulkArchiveAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        ValidateBatch(selectedIds.Count, "archive");
        if (selectedIds.Count == 0) return 0;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var permissions = await db.Permissions.IgnoreQueryFilters()
            .Where(permission => selectedIds.Contains(permission.Id) && permission.TenantId == user.TenantId)
            .ToListAsync(cancellationToken);
        if (permissions.Count != selectedIds.Count) throw new KeyNotFoundException("One or more permissions were not found.");
        if (permissions.Any(permission => permission.IsDeleted))
            throw new InvalidOperationException("One or more selected permissions are already archived.");
        if (permissions.Any(permission => permission.IsSystemDefined))
            throw new InvalidOperationException("System-defined permissions cannot be archived.");

        var now = DateTime.UtcNow;
        foreach (var permission in permissions)
        {
            permission.IsDeleted = true;
            permission.DeletedAt = now;
            permission.DeletedBy = user.UserId;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return permissions.Count;
    }

    public async Task<int> BulkRestoreAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        ValidateBatch(selectedIds.Count, "restore");
        if (selectedIds.Count == 0) return 0;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var permissions = await db.Permissions.IgnoreQueryFilters()
            .Where(permission => selectedIds.Contains(permission.Id) && permission.TenantId == user.TenantId)
            .ToListAsync(cancellationToken);
        if (permissions.Count != selectedIds.Count) throw new KeyNotFoundException("One or more permissions were not found.");
        if (permissions.Any(permission => !permission.IsDeleted))
            throw new InvalidOperationException("One or more selected permissions are not archived.");
        if (permissions.GroupBy(permission => permission.PermissionKey, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Selected permissions contain duplicate keys.");

        foreach (var permission in permissions)
        {
            if (await HasActiveKeyConflictAsync(permission.PermissionKey, permission.Id, cancellationToken))
                throw new InvalidOperationException("Another active permission already uses a selected key in the tenant.");
            await ValidateMappingAsync(permission.ModuleId, permission.SubModuleId, permission.MenuId, cancellationToken);
        }

        foreach (var permission in permissions)
        {
            permission.IsDeleted = false;
            permission.DeletedAt = null;
            permission.DeletedBy = null;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return permissions.Count;
    }

    public async Task<IReadOnlyList<PermissionDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default) =>
        await db.Permissions.AsNoTracking()
            .Where(permission => permission.TenantId == user.TenantId && permission.IsActive && !permission.IsDeleted)
            .OrderBy(permission => permission.SortOrder)
            .ThenBy(permission => permission.PermissionName)
            .Select(permission => new PermissionDropdownDto(
                permission.Id, permission.PermissionName, permission.PermissionKey, permission.PermissionType))
            .ToListAsync(cancellationToken);

    public IReadOnlyList<PermissionTypeDto> GetTypes() => PermissionTypeCatalog.Types;

    private async Task ValidateMappingAsync(Guid moduleId, Guid? subModuleId, Guid? menuId, CancellationToken cancellationToken)
    {
        if (moduleId == Guid.Empty || !await db.Modules.IgnoreQueryFilters().AnyAsync(module =>
            module.Id == moduleId && module.TenantId == user.TenantId && !module.IsDeleted && module.IsActive,
            cancellationToken))
            throw new InvalidOperationException("Select an active module in this tenant.");

        if (subModuleId.HasValue && !await db.SubModules.IgnoreQueryFilters().AnyAsync(subModule =>
            subModule.Id == subModuleId.Value && subModule.TenantId == user.TenantId &&
            subModule.ParentId == moduleId && !subModule.IsDeleted && subModule.IsActive,
            cancellationToken))
            throw new InvalidOperationException("The selected submodule must be active and belong to the selected module.");

        if (!menuId.HasValue) return;
        var menuIsInModule = await db.MenuItems.IgnoreQueryFilters().AnyAsync(menu =>
            menu.Id == menuId.Value && menu.TenantId == user.TenantId &&
            !menu.IsDeleted && menu.IsActive &&
            (menu.ModuleId == moduleId || db.MenuPlacements.IgnoreQueryFilters().Any(placement =>
                placement.TenantId == user.TenantId && placement.MenuId == menu.Id &&
                placement.ModuleId == moduleId && !placement.IsDeleted)),
            cancellationToken);
        if (!menuIsInModule)
            throw new InvalidOperationException("The selected menu must be active and belong to the selected module.");
    }

    private async Task<Permission?> FindIncludingArchivedAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(permission =>
            permission.Id == id && permission.TenantId == user.TenantId, cancellationToken);

    private Task<bool> HasActiveKeyConflictAsync(string key, Guid excludeId, CancellationToken cancellationToken) =>
        db.Permissions.IgnoreQueryFilters().AnyAsync(permission =>
            permission.TenantId == user.TenantId && !permission.IsDeleted &&
            permission.PermissionKey == key && permission.Id != excludeId, cancellationToken);

    private static void ValidateBatch(int count, string operation)
    {
        if (count > MaximumBatchSize)
            throw new InvalidOperationException($"At most {MaximumBatchSize} permissions can be selected for {operation}.");
    }

    private static IQueryable<Permission> ApplySort(IQueryable<Permission> query, string? field, string? order)
    {
        var descending = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (field ?? "permissionName").Trim().ToLowerInvariant() switch
        {
            "permissionkey" => descending
                ? query.OrderByDescending(permission => permission.PermissionKey).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.PermissionKey).ThenBy(permission => permission.Id),
            "modulename" => descending
                ? query.OrderByDescending(permission => permission.Module!.Name).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.Module!.Name).ThenBy(permission => permission.Id),
            "submodulename" => descending
                ? query.OrderByDescending(permission => permission.SubModule!.Name).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.SubModule!.Name).ThenBy(permission => permission.Id),
            "menuname" => descending
                ? query.OrderByDescending(permission => permission.Menu!.Label).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.Menu!.Label).ThenBy(permission => permission.Id),
            "permissiontype" => descending
                ? query.OrderByDescending(permission => permission.PermissionType).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.PermissionType).ThenBy(permission => permission.Id),
            "sortorder" => descending
                ? query.OrderByDescending(permission => permission.SortOrder).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.SortOrder).ThenBy(permission => permission.Id),
            "issystemdefined" => descending
                ? query.OrderByDescending(permission => permission.IsSystemDefined).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.IsSystemDefined).ThenBy(permission => permission.Id),
            "isactive" => descending
                ? query.OrderByDescending(permission => permission.IsActive).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.IsActive).ThenBy(permission => permission.Id),
            "createdat" => descending
                ? query.OrderByDescending(permission => permission.CreatedAt).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.CreatedAt).ThenBy(permission => permission.Id),
            _ => descending
                ? query.OrderByDescending(permission => permission.PermissionName).ThenBy(permission => permission.Id)
                : query.OrderBy(permission => permission.PermissionName).ThenBy(permission => permission.Id)
        };
    }

    private PermissionListDto ToListDto(Permission permission) => new(
        permission.Id,
        permission.ModuleId,
        permission.SubModuleId,
        permission.MenuId,
        permission.PermissionName,
        permission.PermissionKey,
        permission.PermissionType,
        permission.Description,
        permission.SortOrder,
        permission.IsSystemDefined,
        permission.IsActive,
        permission.IsDeleted,
        ToUtcOffset(permission.CreatedAt),
        RelatedName(permission.Module?.TenantId, permission.Module?.Name),
        RelatedName(permission.SubModule?.TenantId, permission.SubModule?.Name),
        RelatedName(permission.Menu?.TenantId, permission.Menu?.Label));

    private PermissionDetailsDto ToDetailsDto(Permission permission) => new(
        permission.Id,
        permission.ModuleId,
        permission.SubModuleId,
        permission.MenuId,
        permission.PermissionName,
        permission.PermissionKey,
        permission.PermissionType,
        permission.Description,
        permission.SortOrder,
        permission.IsSystemDefined,
        permission.IsActive,
        permission.IsDeleted,
        ToUtcOffset(permission.CreatedAt),
        permission.CreatedBy,
        permission.UpdatedAt.HasValue ? ToUtcOffset(permission.UpdatedAt.Value) : null,
        permission.UpdatedBy,
        permission.DeletedAt.HasValue ? ToUtcOffset(permission.DeletedAt.Value) : null,
        permission.DeletedBy,
        RelatedName(permission.Module?.TenantId, permission.Module?.Name),
        RelatedName(permission.SubModule?.TenantId, permission.SubModule?.Name),
        RelatedName(permission.Menu?.TenantId, permission.Menu?.Label));

    private static PermissionDto ToDto(PermissionDetailsDto details) => new(
        details.Id,
        details.ModuleId,
        details.SubModuleId,
        details.MenuId,
        details.PermissionName,
        details.PermissionKey,
        details.PermissionType,
        details.Description,
        details.SortOrder,
        details.IsSystemDefined,
        details.IsActive,
        details.IsDeleted,
        details.CreatedAt,
        details.ModuleName,
        details.SubModuleName,
        details.MenuName);

    private string? RelatedName(Guid? tenantId, string? value) =>
        tenantId == user.TenantId ? value : null;

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return new DateTimeOffset(utc);
    }
}
