using System.Data;
using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class RolePermissionMappingService(WorkflowDbContext db, IUserContext user) : IRolePermissionMappingService
{
    private const int MaximumPermissionCount = 5000;

    public async Task<IReadOnlyList<RolePermissionRoleDto>> GetRolesAsync(
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var roles = db.Roles.IgnoreQueryFilters().AsNoTracking()
            .Where(role => role.TenantId == user.TenantId && !role.IsDeleted && role.IsActive);

        if (departmentId.HasValue)
        {
            var departmentExists = await db.Departments.IgnoreQueryFilters().AnyAsync(department =>
                department.Id == departmentId.Value && department.TenantId == user.TenantId &&
                !department.IsDeleted && department.IsActive, cancellationToken);
            if (!departmentExists)
                throw new KeyNotFoundException("The selected active department was not found.");

            roles = roles.Where(role => db.RoleGroupMappings.IgnoreQueryFilters().Any(mapping =>
                mapping.TenantId == user.TenantId && mapping.RoleId == role.Id &&
                !mapping.IsDeleted && mapping.IsActive &&
                mapping.RoleGroup.TenantId == user.TenantId &&
                !mapping.RoleGroup.IsDeleted && mapping.RoleGroup.IsActive &&
                mapping.RoleGroup.DepartmentId == departmentId.Value));
        }

        return await roles.OrderBy(role => role.Priority)
            .ThenBy(role => role.Name)
            .Select(role => new RolePermissionRoleDto(role.Id, role.Name, role.Code, role.IsSystem))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RolePermissionItemDto>> GetPermissionTreeAsync(
        CancellationToken cancellationToken = default)
    {
        var permissions = await LoadActivePermissionsAsync(cancellationToken);
        return permissions.Select(permission => ToDto(permission, false)).ToList();
    }

    public async Task<RolePermissionMappingDto?> GetRoleMappingAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await FindActiveRoleAsync(roleId, cancellationToken);
        if (role is null) return null;

        var permissions = await LoadActivePermissionsAsync(cancellationToken);
        var permissionIds = permissions.Select(permission => permission.Id).ToHashSet();
        var grantedIds = (await db.RolePermissions.AsNoTracking()
            .Where(mapping => mapping.TenantId == user.TenantId && mapping.RoleId == roleId &&
                mapping.IsGranted && permissionIds.Contains(mapping.PermissionId))
            .Select(mapping => mapping.PermissionId)
            .ToListAsync(cancellationToken)).ToHashSet();

        return new RolePermissionMappingDto(
            role.Id,
            role.Name,
            permissions.Select(permission => ToDto(permission, grantedIds.Contains(permission.Id))).ToList());
    }

    public async Task<RolePermissionMappingDto> SaveRoleMappingAsync(
        Guid roleId,
        RolePermissionSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Permissions is null || request.Permissions.Count > MaximumPermissionCount)
            throw new InvalidOperationException("The permission mapping is invalid or exceeds the supported size.");
        if (request.Permissions.Any(permission => permission.PermissionId == Guid.Empty) ||
            request.Permissions.Select(permission => permission.PermissionId).Distinct().Count() != request.Permissions.Count)
            throw new InvalidOperationException("Permission identifiers must be valid and unique.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var role = await FindActiveRoleAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException("The selected active role was not found.");
        var permissions = await LoadActivePermissionsAsync(cancellationToken);
        var catalogIds = permissions.Select(permission => permission.Id).ToHashSet();
        var requestedIds = request.Permissions.Select(permission => permission.PermissionId).ToHashSet();
        if (!requestedIds.SetEquals(catalogIds))
            throw new InvalidOperationException("The active permission catalog changed. Reload the mapping before saving.");

        var desiredGrantedIds = request.Permissions
            .Where(permission => permission.IsGranted)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        var mappings = await db.RolePermissions.IgnoreQueryFilters()
            .Where(mapping => mapping.TenantId == user.TenantId && mapping.RoleId == roleId)
            .OrderByDescending(mapping => mapping.CreatedAt)
            .ToListAsync(cancellationToken);

        var mappingByPermission = mappings
            .GroupBy(mapping => mapping.PermissionId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var mapping in mappings.Where(mapping => !mapping.IsDeleted))
        {
            if (desiredGrantedIds.Contains(mapping.PermissionId))
            {
                mapping.IsGranted = true;
                continue;
            }

            mapping.IsGranted = false;
            mapping.IsDeleted = true;
            mapping.DeletedAt = DateTime.UtcNow;
            mapping.DeletedBy = user.UserId;
        }

        foreach (var permissionId in desiredGrantedIds)
        {
            if (mappingByPermission.TryGetValue(permissionId, out var existing))
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                }

                existing.IsGranted = true;
                continue;
            }

            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                RoleId = roleId,
                PermissionId = permissionId,
                IsGranted = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RolePermissionMappingDto(
            role.Id,
            role.Name,
            permissions.Select(permission => ToDto(permission, desiredGrantedIds.Contains(permission.Id))).ToList());
    }

    private Task<Roles?> FindActiveRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        db.Roles.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(role => role.Id == roleId && role.TenantId == user.TenantId &&
                !role.IsDeleted && role.IsActive, cancellationToken);

    private async Task<List<Permission>> LoadActivePermissionsAsync(CancellationToken cancellationToken)
    {
        var records = await db.Permissions.IgnoreQueryFilters().AsNoTracking()
            .Where(permission => permission.TenantId == user.TenantId &&
                !permission.IsDeleted && permission.IsActive)
            .Include(permission => permission.Module)
            .Include(permission => permission.SubModule)
            .Include(permission => permission.Menu)
            .ToListAsync(cancellationToken);

        return records.Where(permission =>
                permission.Module is { IsDeleted: false, IsActive: true } module &&
                module.TenantId == user.TenantId &&
                (permission.SubModule == null ||
                    permission.SubModule is { IsDeleted: false, IsActive: true } subModule &&
                    subModule.TenantId == user.TenantId && subModule.ParentId == permission.ModuleId) &&
                (permission.Menu == null ||
                    permission.Menu is { IsDeleted: false, IsActive: true } menu &&
                    menu.TenantId == user.TenantId && menu.ModuleId == permission.ModuleId))
            .OrderBy(permission => permission.Module!.OrderNo)
            .ThenBy(permission => permission.SubModule == null ? int.MaxValue : permission.SubModule.OrderNo)
            .ThenBy(permission => permission.Menu == null ? int.MaxValue : permission.Menu.OrderNo)
            .ThenBy(permission => permission.SortOrder)
            .ThenBy(permission => permission.PermissionName)
            .ToList();
    }

    private static RolePermissionItemDto ToDto(Permission permission, bool isGranted) => new(
        permission.Id,
        permission.ModuleId,
        permission.Module!.Name,
        permission.Module.OrderNo,
        permission.SubModuleId,
        permission.SubModule?.Name,
        permission.SubModule?.OrderNo ?? int.MaxValue,
        permission.MenuId,
        permission.Menu?.Label,
        permission.Menu?.OrderNo ?? int.MaxValue,
        permission.PermissionName,
        permission.PermissionKey,
        permission.PermissionType,
        permission.SortOrder,
        permission.IsSystemDefined,
        isGranted);
}
