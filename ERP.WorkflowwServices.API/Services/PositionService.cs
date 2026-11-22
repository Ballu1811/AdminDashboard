using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ERP.WorkflowwServices.API.Services;

public sealed class PositionService(WorkflowDbContext db, IUserContext user) : IPositionService
{
    public async Task<PagedResult<PositionDto>> GetAllAsync(PositionFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = user.TenantId;
        var query = db.Positions.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId);
        if (filter.Status == 0) query = query.Where(x => !x.IsDeleted && x.IsActive);
        else if (filter.Status == 1) query = query.Where(x => x.IsDeleted);
        else if (filter.Status == 2) query = query.Where(x => !x.IsDeleted && !x.IsActive);
        if (filter.CompanyId.HasValue) query = query.Where(x => x.CompanyId == filter.CompanyId);
        if (filter.DepartmentId.HasValue) query = query.Where(x => x.DepartmentId == filter.DepartmentId);
        if (filter.ParentPositionId.HasValue) query = query.Where(x => x.ParentPositionId == filter.ParentPositionId);
        if (!string.IsNullOrWhiteSpace(filter.KeyWord))
        {
            var keyword = filter.KeyWord.Trim();
            query = query.Where(x => x.PositionName.Contains(keyword) || x.PositionCode.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)) || (x.Remarks != null && x.Remarks.Contains(keyword)));
        }
        query = ApplySort(query, filter.SortField ?? filter.SortBy, filter.SortOrder ?? filter.SortDirection);
        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(filter.PageNumber, 1);
        var size = Math.Clamp(filter.PageSize, 1, 250);
        var entities = await query.Skip((page - 1) * size).Take(size)
            .Include(x => x.ParentPosition)
            .Include(x => x.Department)
            .Include(x => x.PositionRoles.Where(mapping => mapping.TenantId == tenantId && mapping.IsActive && !mapping.IsDeleted))
                .ThenInclude(mapping => mapping.Role)
            .ToListAsync(cancellationToken);
        return new PagedResult<PositionDto> { Total = total, Data = entities.Select(ToDto).ToList() };
    }

    public async Task<PositionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Positions.AsNoTracking().Where(x => x.TenantId == user.TenantId)
            .Include(x => x.ParentPosition).Include(x => x.Department)
            .Include(x => x.PositionRoles.Where(mapping => mapping.IsActive && !mapping.IsDeleted)).ThenInclude(mapping => mapping.Role)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<PositionDto> SaveAsync(PositionSaveRequest request, long? id = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var name = request.PositionName?.Trim() ?? string.Empty;
        var code = request.PositionCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Position name and code are required.");
        if (request.DepartmentId == Guid.Empty) throw new InvalidOperationException("Select a department.");
        var department = await db.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(x =>
            x.Id == request.DepartmentId && x.TenantId == user.TenantId && x.IsActive && !x.IsDeleted, cancellationToken);
        if (department == null) throw new InvalidOperationException("Select an active department.");
        var companyId = department.CompanyId;
        if (request.CompanyId.HasValue && request.CompanyId != companyId)
            throw new InvalidOperationException("The selected department does not belong to the selected company.");
        if (companyId.HasValue && !await db.Companies.AnyAsync(x => x.Id == companyId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Select an active company.");
        var duplicate = await db.Positions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == user.TenantId && x.CompanyId == companyId &&
            x.PositionCode == code && !x.IsDeleted && x.Id != id, cancellationToken);
        if (duplicate) throw new InvalidOperationException("A position with this code already exists in this company.");
        Position? entity = null;
        if (id.HasValue)
        {
            entity = await db.Positions.Include(x => x.PositionRoles.Where(mapping => !mapping.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id.Value && x.TenantId == user.TenantId, cancellationToken)
                ?? throw new KeyNotFoundException("Position was not found.");
            if (entity.CompanyId != companyId && await db.Positions.IgnoreQueryFilters().AnyAsync(x =>
                x.TenantId == user.TenantId && x.ParentPositionId == id && !x.IsDeleted, cancellationToken))
                throw new InvalidOperationException("Reassign child positions before changing this position's company.");
        }
        await ValidateParentAsync(request.ParentPositionId, id, companyId, cancellationToken);
        var roleIds = (request.RoleIds ?? []).Distinct().ToList();
        var validRoles = await db.Roles.Where(x => roleIds.Contains(x.Id) && x.IsActive && !x.IsDeleted && x.TenantId == user.TenantId)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (validRoles.Count != roleIds.Count) throw new InvalidOperationException("One or more selected roles are missing or inactive.");

        entity ??= new Position { TenantId = user.TenantId };

        entity.CompanyId = companyId;
        entity.PositionName = name;
        entity.PositionCode = code;
        entity.Description = request.Description?.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.ParentPositionId = request.ParentPositionId;
        entity.PositionCategory = request.PositionCategory;
        entity.IsManagerial = request.IsManagerial;
        entity.IsActive = request.IsActive;
        entity.Remarks = request.Remarks?.Trim();
        entity.PositionLevel = await CalculateLevelAsync(request.ParentPositionId, cancellationToken);
        if (!id.HasValue) db.Positions.Add(entity);

        var selected = roleIds.ToHashSet();
        foreach (var mapping in entity.PositionRoles)
        {
            mapping.IsActive = selected.Remove(mapping.RoleId);
            mapping.IsDeleted = !mapping.IsActive;
            if (mapping.IsDeleted)
            {
                mapping.DeletedAt = DateTime.UtcNow;
                mapping.DeletedBy = user.UserId;
            }
        }
        foreach (var roleId in selected) entity.PositionRoles.Add(new PositionRole { TenantId = user.TenantId, RoleId = roleId, IsActive = true });

        if (id.HasValue) await RecalculateDescendantLevelsAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<bool> IsCodeAvailableAsync(Guid? companyId, string code, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode)) return false;
        if (companyId.HasValue && !await db.Companies.AnyAsync(x => x.Id == companyId && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Select an active company.");
        return !await db.Positions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == user.TenantId &&
            x.CompanyId == companyId && x.PositionCode == normalizedCode && !x.IsDeleted &&
            (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }

    public async Task ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Position was not found.");
        var hasChildren = await db.Positions.AnyAsync(x => x.ParentPositionId == id && !x.IsDeleted, cancellationToken);
        if (hasChildren) throw new InvalidOperationException("Reassign or archive child positions before archiving this position.");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedBy = user.UserId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var entity = await db.Positions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == user.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Position was not found.");
        if (!entity.IsDeleted) throw new InvalidOperationException("Position is already active.");
        if (await db.Positions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == entity.TenantId && x.CompanyId == entity.CompanyId && x.PositionCode == entity.PositionCode && !x.IsDeleted && x.Id != entity.Id, cancellationToken))
            throw new InvalidOperationException("Another active position is already using this code in the company.");
        if (entity.ParentPositionId.HasValue && !await db.Positions.AnyAsync(x => x.Id == entity.ParentPositionId &&
            x.TenantId == user.TenantId && x.CompanyId == entity.CompanyId && x.IsActive && !x.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Restore the parent position before restoring this position.");
        entity.IsDeleted = false; entity.DeletedAt = null; entity.DeletedBy = null;
        entity.PositionLevel = await CalculateLevelAsync(entity.ParentPositionId, cancellationToken);
        await RecalculateDescendantLevelsAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> BulkArchiveAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        if (selectedIds.Count == 0) return 0;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var blocked = await db.Positions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == user.TenantId &&
            x.ParentPositionId.HasValue && selectedIds.Contains(x.ParentPositionId.Value) &&
            !x.IsDeleted && !selectedIds.Contains(x.Id), cancellationToken);
        if (blocked) throw new InvalidOperationException("Selected positions have child positions outside the selection.");
        var records = await db.Positions.Where(x => selectedIds.Contains(x.Id) && x.TenantId == user.TenantId).ToListAsync(cancellationToken);
        if (records.Count != selectedIds.Count) throw new KeyNotFoundException("One or more positions were not found.");
        foreach (var item in records) { item.IsDeleted = true; item.DeletedAt = DateTime.UtcNow; item.DeletedBy = user.UserId; }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return records.Count;
    }

    public async Task<int> BulkRestoreAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        if (selectedIds.Count == 0) return 0;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var records = await db.Positions.IgnoreQueryFilters()
            .Where(x => selectedIds.Contains(x.Id) && x.TenantId == user.TenantId)
            .ToListAsync(cancellationToken);
        if (records.Count != selectedIds.Count) throw new KeyNotFoundException("One or more positions were not found.");
        if (records.Any(x => !x.IsDeleted)) throw new InvalidOperationException("One or more selected positions are already active.");
        if (records.GroupBy(x => new { x.CompanyId, x.PositionCode }).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Selected positions contain duplicate codes within the same company.");

        foreach (var record in records)
        {
            if (await db.Positions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == user.TenantId &&
                x.CompanyId == record.CompanyId && x.PositionCode == record.PositionCode && !x.IsDeleted &&
                !selectedIds.Contains(x.Id), cancellationToken))
                throw new InvalidOperationException("Another active position is already using this code in the company.");
        }

        var hierarchy = await db.Positions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.TenantId == user.TenantId && (!x.IsDeleted || selectedIds.Contains(x.Id)))
            .Select(x => new { x.Id, x.CompanyId, x.ParentPositionId, x.PositionLevel, x.IsActive, x.IsDeleted })
            .ToListAsync(cancellationToken);
        var hierarchyById = hierarchy.ToDictionary(x => x.Id);
        foreach (var record in records)
        {
            if (!record.ParentPositionId.HasValue) continue;
            if (!hierarchyById.TryGetValue(record.ParentPositionId.Value, out var parent) ||
                parent.CompanyId != record.CompanyId || !parent.IsActive ||
                (parent.IsDeleted && !selectedIds.Contains(parent.Id)))
                throw new InvalidOperationException("Restore the active parent position before restoring its child.");
        }

        var calculatedLevels = new Dictionary<long, int>();
        var visiting = new HashSet<long>();
        int CalculateRestoredLevel(long positionId)
        {
            if (calculatedLevels.TryGetValue(positionId, out var knownLevel)) return knownLevel;
            if (!visiting.Add(positionId)) throw new InvalidOperationException("Circular position hierarchy is not allowed.");
            var position = hierarchyById[positionId];
            var level = 1;
            if (position.ParentPositionId.HasValue)
            {
                var parent = hierarchyById[position.ParentPositionId.Value];
                if (parent.CompanyId != position.CompanyId || !parent.IsActive ||
                    (parent.IsDeleted && !selectedIds.Contains(parent.Id)))
                    throw new InvalidOperationException("Restore the active parent position before restoring its child.");
                level = CalculateRestoredLevel(parent.Id) + 1;
            }
            visiting.Remove(positionId);
            calculatedLevels[positionId] = level;
            return level;
        }

        foreach (var record in records)
        {
            record.PositionLevel = CalculateRestoredLevel(record.Id);
            record.IsDeleted = false;
            record.DeletedAt = null;
            record.DeletedBy = null;
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var record in records) await RecalculateDescendantLevelsAsync(record, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return records.Count;
    }

    public async Task<List<PositionHierarchyDto>> GetHierarchyAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Positions.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted);
        if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
        var records = await query
            .Include(x => x.Department)
            .Include(x => x.PositionRoles.Where(mapping => mapping.IsActive && !mapping.IsDeleted)).ThenInclude(mapping => mapping.Role)
            .OrderBy(x => x.PositionLevel).ThenBy(x => x.PositionName).ToListAsync(cancellationToken);
        var byParent = records.GroupBy(x => x.ParentPositionId ?? 0L).ToDictionary(x => x.Key, x => x.Select(ToHierarchyDto).ToList());
        List<PositionHierarchyDto> Build(long? parentId, HashSet<long> visited)
        {
            if (!byParent.TryGetValue(parentId ?? 0L, out var children)) return [];
            var result = new List<PositionHierarchyDto>();
            foreach (var child in children)
            {
                if (!visited.Add(child.Id)) continue;
                child.Children = Build(child.Id, visited);
                result.Add(child);
            }
            return result;
        }
        return Build(null, []);
    }

    public Task<List<PositionDropdownDto>> GetDropdownAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Positions.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted);
        if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
        return query.OrderBy(x => x.PositionName)
            .Select(x => new PositionDropdownDto(x.Id, x.PositionName, x.PositionCode, x.DepartmentId, x.ParentPositionId, x.PositionLevel, x.CompanyId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PositionRoleDto>?> GetRolesAsync(long id, CancellationToken cancellationToken = default)
    {
        if (!await db.Positions.AnyAsync(x => x.Id == id && x.TenantId == user.TenantId, cancellationToken)) return null;
        return await db.PositionRoles.AsNoTracking().Where(x => x.PositionId == id && x.IsActive && !x.IsDeleted)
            .Select(x => new PositionRoleDto { RoleId = x.RoleId, Name = x.Role.Name, Code = x.Role.Code, Description = x.Role.Description, IsActive = x.Role.IsActive })
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateParentAsync(long? parentId, long? selfId, Guid? companyId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue) return;
        var parent = await db.Positions.IgnoreQueryFilters().FirstOrDefaultAsync(x =>
            x.Id == parentId && x.TenantId == user.TenantId && x.CompanyId == companyId && x.IsActive && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Parent position must be active and belong to the same company.");
        if (parent.Id == selfId) throw new InvalidOperationException("A position cannot be its own parent.");
        var visited = new HashSet<long>();
        var currentId = parent.ParentPositionId;
        while (currentId.HasValue)
        {
            if (currentId == selfId) throw new InvalidOperationException("Circular position hierarchy is not allowed.");
            if (!visited.Add(currentId.Value)) throw new InvalidOperationException("Existing circular hierarchy must be resolved first.");
            currentId = await db.Positions.IgnoreQueryFilters().Where(x =>
                x.Id == currentId.Value && x.TenantId == user.TenantId && x.CompanyId == companyId && !x.IsDeleted)
                .Select(x => x.ParentPositionId).FirstOrDefaultAsync(cancellationToken);
        }
    }

    private async Task<int> CalculateLevelAsync(long? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue) return 1;
        var parentLevel = await db.Positions.Where(x => x.Id == parentId && x.TenantId == user.TenantId)
            .Select(x => x.PositionLevel).FirstOrDefaultAsync(cancellationToken);
        return (parentLevel ?? 0) + 1;
    }

    private async Task RecalculateDescendantLevelsAsync(Position root, CancellationToken cancellationToken)
    {
        var positions = await db.Positions.IgnoreQueryFilters().Where(x =>
            x.TenantId == user.TenantId && x.CompanyId == root.CompanyId && !x.IsDeleted).ToListAsync(cancellationToken);
        var childrenByParent = positions.Where(x => x.ParentPositionId.HasValue)
            .GroupBy(x => x.ParentPositionId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
        var visited = new HashSet<long> { root.Id };
        var pending = new Queue<(long Id, int Level)>();
        pending.Enqueue((root.Id, root.PositionLevel ?? 1));

        while (pending.TryDequeue(out var parent))
        {
            if (!childrenByParent.TryGetValue(parent.Id, out var children)) continue;
            foreach (var child in children)
            {
                if (!visited.Add(child.Id)) continue;
                child.PositionLevel = parent.Level + 1;
                pending.Enqueue((child.Id, child.PositionLevel.Value));
            }
        }
    }

    private static IQueryable<Position> ApplySort(IQueryable<Position> query, string? field, string? direction)
    {
        var desc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (field ?? "positionName").ToLowerInvariant() switch
        {
            "positioncode" or "code" => desc ? query.OrderByDescending(x => x.PositionCode).ThenBy(x => x.Id) : query.OrderBy(x => x.PositionCode).ThenBy(x => x.Id),
            "departmentid" => desc ? query.OrderByDescending(x => x.DepartmentId).ThenBy(x => x.Id) : query.OrderBy(x => x.DepartmentId).ThenBy(x => x.Id),
            "positionlevel" => desc ? query.OrderByDescending(x => x.PositionLevel).ThenBy(x => x.Id) : query.OrderBy(x => x.PositionLevel).ThenBy(x => x.Id),
            "createdat" => desc ? query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id) : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => desc ? query.OrderByDescending(x => x.PositionName).ThenBy(x => x.Id) : query.OrderBy(x => x.PositionName).ThenBy(x => x.Id)
        };
    }

    private static PositionDto ToDto(Position x) => new()
    {
        Id=x.Id, CompanyId=x.CompanyId, PositionName=x.PositionName, PositionCode=x.PositionCode, Description=x.Description,
        DepartmentId=x.DepartmentId, DepartmentName=x.Department?.TenantId == x.TenantId && x.Department.CompanyId == x.CompanyId ? x.Department.Name : null,
        ParentPositionId=x.ParentPositionId, ParentPositionName=x.ParentPosition?.TenantId == x.TenantId &&
            x.ParentPosition.CompanyId == x.CompanyId ? x.ParentPosition.PositionName : null,
        PositionLevel=x.PositionLevel, PositionCategory=x.PositionCategory, IsManagerial=x.IsManagerial, IsActive=x.IsActive,
        Remarks=x.Remarks, IsDeleted=x.IsDeleted, CreatedAt=x.CreatedAt,
        AssignedRoles=x.PositionRoles.Where(mapping => mapping.IsActive && !mapping.IsDeleted && mapping.Role != null && mapping.Role.TenantId == x.TenantId)
            .Select(mapping => new PositionRoleDto { RoleId=mapping.RoleId, Name=mapping.Role.Name, Code=mapping.Role.Code, Description=mapping.Role.Description, IsActive=mapping.Role.IsActive }).ToList(),
        AssignedRoleCount=x.PositionRoles.Count(mapping => mapping.TenantId == x.TenantId && mapping.IsActive && !mapping.IsDeleted)
    };
    private static PositionHierarchyDto ToHierarchyDto(Position x)
    {
        var dto = ToDto(x);
        return new PositionHierarchyDto { Id=dto.Id, CompanyId=dto.CompanyId, PositionName=dto.PositionName, PositionCode=dto.PositionCode,
            DepartmentId=dto.DepartmentId, ParentPositionId=dto.ParentPositionId, ParentPositionName=dto.ParentPositionName, PositionLevel=dto.PositionLevel,
            PositionCategory=dto.PositionCategory, IsManagerial=dto.IsManagerial, IsActive=dto.IsActive, IsDeleted=dto.IsDeleted,
            AssignedRoles=dto.AssignedRoles, AssignedRoleCount=dto.AssignedRoleCount };
    }
}
