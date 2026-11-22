using System.Data;
using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class RoleGroupService(WorkflowDbContext db, IUserContext user) : IRoleGroupService
{
    public async Task<PagedResult<RoleGroupDto>> GetAllAsync(RoleGroupFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = user.TenantId;
        var query = db.RoleGroups.IgnoreQueryFilters().AsNoTracking()
            .Where(group => group.TenantId == tenantId);

        if (filter.Status == 0) query = query.Where(group => !group.IsDeleted && group.IsActive);
        else if (filter.Status == 1) query = query.Where(group => group.IsDeleted);
        else if (filter.Status == 2) query = query.Where(group => !group.IsDeleted && !group.IsActive);
        if (filter.CompanyId.HasValue) query = query.Where(group => group.CompanyId == filter.CompanyId);
        if (filter.DepartmentId.HasValue) query = query.Where(group => group.DepartmentId == filter.DepartmentId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(group => group.GroupName.Contains(search) || group.GroupCode.Contains(search) ||
                (group.Description != null && group.Description.Contains(search)));
        }

        query = ApplySort(query, filter.SortField, filter.SortOrder, tenantId);
        var total = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 250);
        var records = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Include(group => group.Department)
            .Include(group => group.RoleMappings.Where(mapping =>
                mapping.TenantId == tenantId && mapping.IsActive && !mapping.IsDeleted))
                .ThenInclude(mapping => mapping.Role)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleGroupDto> { Total = total, Data = records.Select(ToDto).ToList() };
    }

    public async Task<RoleGroupDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var tenantId = user.TenantId;
        var group = await db.RoleGroups.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Id == id && item.TenantId == tenantId)
            .Include(item => item.Department)
            .Include(item => item.RoleMappings.Where(mapping =>
                mapping.TenantId == tenantId && mapping.IsActive && !mapping.IsDeleted))
                .ThenInclude(mapping => mapping.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return group is null ? null : ToDto(group);
    }

    public async Task<RoleGroupDto> SaveAsync(RoleGroupSaveRequest request, long? id = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var groupName = request.GroupName.Trim();
        var normalizedName = groupName.ToUpperInvariant();
        var groupCode = request.GroupCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(groupCode))
            throw new InvalidOperationException("Group name and code are required.");
        if (groupName.Length > 150 || groupCode.Length > 50 || !System.Text.RegularExpressions.Regex.IsMatch(groupCode, "^[A-Z0-9_-]+$"))
            throw new InvalidOperationException("Group name or code has an invalid format or length.");

        var department = await ValidateDepartmentAsync(request.DepartmentId, request.CompanyId, cancellationToken);
        var companyId = department?.CompanyId ?? request.CompanyId;

        var duplicate = await db.RoleGroups.IgnoreQueryFilters().AnyAsync(group =>
            group.TenantId == user.TenantId && group.CompanyId == companyId && !group.IsDeleted &&
            (group.GroupCode == groupCode || group.NormalizedGroupName == normalizedName) && group.Id != id,
            cancellationToken);
        if (duplicate) throw new InvalidOperationException("A role group with this name or code already exists in the company.");

        RoleGroup? entity = null;
        if (id.HasValue)
        {
            entity = await db.RoleGroups.IgnoreQueryFilters()
                .Include(group => group.RoleMappings.Where(mapping =>
                    mapping.TenantId == user.TenantId && !mapping.IsDeleted))
                .FirstOrDefaultAsync(group => group.Id == id.Value && group.TenantId == user.TenantId && !group.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Role group was not found.");
        }

        var roleIds = request.RoleIds ?? [];
        if (roleIds.Count == 0 || roleIds.Any(roleId => roleId == Guid.Empty))
            throw new InvalidOperationException("Select at least one valid role.");
        if (roleIds.Count != roleIds.Distinct().Count())
            throw new InvalidOperationException("Role mappings cannot contain duplicate roles.");

        var selectedRoleIds = roleIds.ToHashSet();
        var currentlyAssignedRoleIds = entity?.RoleMappings
            .Where(mapping => mapping.IsActive && !mapping.IsDeleted)
            .Select(mapping => mapping.RoleId)
            .ToHashSet() ?? [];
        var roles = await db.Roles.IgnoreQueryFilters()
            .Where(role => role.TenantId == user.TenantId && selectedRoleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
        if (roles.Count != selectedRoleIds.Count)
            throw new InvalidOperationException("One or more selected roles do not exist in this tenant.");
        if (roles.Any(role => (role.IsDeleted || !role.IsActive) && !currentlyAssignedRoleIds.Contains(role.Id)))
            throw new InvalidOperationException("Inactive or archived roles cannot be newly assigned.");

        entity ??= new RoleGroup { TenantId = user.TenantId };
        entity.CompanyId = companyId;
        entity.GroupName = groupName;
        entity.NormalizedGroupName = normalizedName;
        entity.GroupCode = groupCode;
        entity.Description = NormalizeOptional(request.Description);
        entity.DepartmentId = department?.Id;
        entity.Remarks = NormalizeOptional(request.Remarks);
        entity.IsActive = request.IsActive;
        if (!id.HasValue) db.RoleGroups.Add(entity);

        var remainingRoleIds = selectedRoleIds;
        foreach (var mapping in entity.RoleMappings)
        {
            if (mapping.IsDeleted) continue;
            if (remainingRoleIds.Remove(mapping.RoleId))
            {
                mapping.IsActive = true;
                continue;
            }

            mapping.IsActive = false;
            mapping.IsDeleted = true;
            mapping.DeletedAt = DateTime.UtcNow;
            mapping.DeletedBy = user.UserId;
        }
        foreach (var roleId in remainingRoleIds)
            entity.RoleMappings.Add(new RoleGroupMapping { TenantId = user.TenantId, RoleId = roleId, IsActive = true });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var group = await db.RoleGroups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == user.TenantId && !item.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Role group was not found.");
        group.IsDeleted = true;
        group.DeletedAt = DateTime.UtcNow;
        group.DeletedBy = user.UserId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var group = await db.RoleGroups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == user.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Role group was not found.");
        if (!group.IsDeleted) throw new InvalidOperationException("Role group is already active.");

        await EnsureUniqueOnRestoreAsync([group], cancellationToken);
        var department = await ValidateDepartmentAsync(group.DepartmentId, group.CompanyId, cancellationToken);
        if (department is not null && department.CompanyId != group.CompanyId)
            throw new InvalidOperationException("The role group's department does not belong to its company.");
        group.IsDeleted = false;
        group.DeletedAt = null;
        group.DeletedBy = null;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> BulkArchiveAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        if (selectedIds.Count == 0) return 0;
        if (selectedIds.Count > 1000) throw new InvalidOperationException("Archive at most 1000 role groups per request.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var records = await db.RoleGroups.IgnoreQueryFilters()
            .Where(group => selectedIds.Contains(group.Id) && group.TenantId == user.TenantId)
            .ToListAsync(cancellationToken);
        if (records.Count != selectedIds.Count) throw new KeyNotFoundException("One or more role groups were not found.");
        if (records.Any(group => group.IsDeleted)) throw new InvalidOperationException("One or more selected role groups are already archived.");
        foreach (var group in records)
        {
            group.IsDeleted = true;
            group.DeletedAt = DateTime.UtcNow;
            group.DeletedBy = user.UserId;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return records.Count;
    }

    public async Task<int> BulkRestoreAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        if (selectedIds.Count == 0) return 0;
        if (selectedIds.Count > 1000) throw new InvalidOperationException("Restore at most 1000 role groups per request.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var records = await db.RoleGroups.IgnoreQueryFilters()
            .Where(group => selectedIds.Contains(group.Id) && group.TenantId == user.TenantId)
            .ToListAsync(cancellationToken);
        if (records.Count != selectedIds.Count) throw new KeyNotFoundException("One or more role groups were not found.");
        if (records.Any(group => !group.IsDeleted)) throw new InvalidOperationException("One or more selected role groups are not archived.");

        await EnsureUniqueOnRestoreAsync(records, cancellationToken);
        foreach (var group in records)
        {
            var department = await ValidateDepartmentAsync(group.DepartmentId, group.CompanyId, cancellationToken);
            if (department is not null && department.CompanyId != group.CompanyId)
                throw new InvalidOperationException("A role group's department does not belong to its company.");
        }

        foreach (var group in records)
        {
            group.IsDeleted = false;
            group.DeletedAt = null;
            group.DeletedBy = null;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return records.Count;
    }

    public Task<List<RoleGroupDropdownDto>> GetDropdownAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = db.RoleGroups.AsNoTracking().Where(group => group.IsActive && !group.IsDeleted);
        if (companyId.HasValue)
            query = query.Where(group => group.CompanyId == companyId || group.CompanyId == null);
        return query.OrderBy(group => group.GroupName)
            .Select(group => new RoleGroupDropdownDto(group.Id, group.GroupName, group.GroupCode, group.CompanyId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoleGroupRoleDto>?> GetRolesAsync(long id, CancellationToken cancellationToken = default)
    {
        var group = await GetByIdAsync(id, cancellationToken);
        return group?.AssignedRoles;
    }

    private async Task<Department?> ValidateDepartmentAsync(Guid? departmentId, Guid? requestedCompanyId, CancellationToken cancellationToken)
    {
        if (!departmentId.HasValue)
        {
            await ValidateCompanyAsync(requestedCompanyId, cancellationToken);
            return null;
        }

        var department = await db.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(item =>
            item.Id == departmentId && item.TenantId == user.TenantId && item.IsActive && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Select an active department in this tenant.");
        if (requestedCompanyId.HasValue && department.CompanyId != requestedCompanyId)
            throw new InvalidOperationException("The selected department does not belong to the selected company.");
        await ValidateCompanyAsync(department.CompanyId, cancellationToken);
        return department;
    }

    private async Task ValidateCompanyAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId.HasValue && !await db.Companies.AnyAsync(company =>
            company.Id == companyId && company.IsActive, cancellationToken))
            throw new InvalidOperationException("Select an active company.");
    }

    private async Task EnsureUniqueOnRestoreAsync(IReadOnlyCollection<RoleGroup> groups, CancellationToken cancellationToken)
    {
        if (groups.GroupBy(group => (group.CompanyId, group.GroupCode)).Any(group => group.Count() > 1) ||
            groups.GroupBy(group => (group.CompanyId, group.NormalizedGroupName)).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Selected role groups contain duplicate names or codes within a company.");

        var groupIds = groups.Select(group => group.Id).ToArray();
        foreach (var group in groups)
        {
            await ValidateCompanyAsync(group.CompanyId, cancellationToken);
            var duplicate = await db.RoleGroups.IgnoreQueryFilters().AnyAsync(other =>
                other.TenantId == user.TenantId && other.CompanyId == group.CompanyId && !other.IsDeleted &&
                (other.GroupCode == group.GroupCode || other.NormalizedGroupName == group.NormalizedGroupName) &&
                !groupIds.Contains(other.Id), cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("Another active role group already uses this name or code in the company.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static IQueryable<RoleGroup> ApplySort(
        IQueryable<RoleGroup> query, string? field, string? direction, Guid tenantId)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (field ?? "groupName").ToLowerInvariant() switch
        {
            "groupcode" or "code" => descending
                ? query.OrderByDescending(group => group.GroupCode).ThenBy(group => group.Id)
                : query.OrderBy(group => group.GroupCode).ThenBy(group => group.Id),
            "departmentname" => descending
                ? query.OrderByDescending(group => group.Department != null &&
                    group.Department.TenantId == tenantId && group.Department.CompanyId == group.CompanyId
                        ? group.Department.Name : null).ThenBy(group => group.Id)
                : query.OrderBy(group => group.Department != null &&
                    group.Department.TenantId == tenantId && group.Department.CompanyId == group.CompanyId
                        ? group.Department.Name : null).ThenBy(group => group.Id),
            "rolecount" or "assignedrolecount" => descending
                ? query.OrderByDescending(group => group.RoleMappings.Count(mapping =>
                    mapping.TenantId == tenantId && mapping.IsActive && !mapping.IsDeleted)).ThenBy(group => group.Id)
                : query.OrderBy(group => group.RoleMappings.Count(mapping =>
                    mapping.TenantId == tenantId && mapping.IsActive && !mapping.IsDeleted)).ThenBy(group => group.Id),
            "createdat" => descending
                ? query.OrderByDescending(group => group.CreatedAt).ThenBy(group => group.Id)
                : query.OrderBy(group => group.CreatedAt).ThenBy(group => group.Id),
            _ => descending
                ? query.OrderByDescending(group => group.GroupName).ThenBy(group => group.Id)
                : query.OrderBy(group => group.GroupName).ThenBy(group => group.Id)
        };
    }

    private static RoleGroupDto ToDto(RoleGroup group)
    {
        var roles = group.RoleMappings
            .Where(mapping => mapping.IsActive && !mapping.IsDeleted && mapping.Role is not null &&
                mapping.TenantId == group.TenantId && mapping.Role.TenantId == group.TenantId)
            .Select(mapping => new RoleGroupRoleDto
            {
                RoleId = mapping.RoleId,
                Name = mapping.Role.Name,
                Code = mapping.Role.Code,
                Description = mapping.Role.Description,
                IsActive = mapping.Role.IsActive && !mapping.Role.IsDeleted,
                IsDeleted = mapping.Role.IsDeleted
            })
            .OrderBy(role => role.Name)
            .ToList();

        var departmentName = group.Department?.TenantId == group.TenantId &&
            group.Department.CompanyId == group.CompanyId
            ? group.Department.Name
            : null;

        return new RoleGroupDto
        {
            Id = group.Id,
            CompanyId = group.CompanyId,
            GroupName = group.GroupName,
            GroupCode = group.GroupCode,
            Description = group.Description,
            DepartmentId = group.DepartmentId,
            DepartmentName = departmentName,
            Remarks = group.Remarks,
            IsActive = group.IsActive,
            IsDeleted = group.IsDeleted,
            CreatedAt = ToUtcOffset(group.CreatedAt),
            CreatedBy = group.CreatedBy,
            UpdatedAt = group.UpdatedAt.HasValue ? ToUtcOffset(group.UpdatedAt.Value) : null,
            UpdatedBy = group.UpdatedBy,
            AssignedRoleCount = roles.Count,
            ActiveAssignedRoleCount = roles.Count(role => role.IsActive),
            InactiveAssignedRoleCount = roles.Count(role => !role.IsActive),
            AssignedRoles = roles
        };
    }
}
