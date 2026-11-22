using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class DepartmentService(WorkflowDbContext db, IUserContext user) : IDepartmentService
{
    public async Task<PagedResult<DepartmentDto>> GetAllAsync(DepartmentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Departments.IgnoreQueryFilters().Where(x => x.TenantId == user.TenantId);
        if (filter.Status == 0) query = query.Where(x => !x.IsDeleted && x.IsActive);
        else if (filter.Status == 1) query = query.Where(x => x.IsDeleted);
        else if (filter.Status == 2) query = query.Where(x => !x.IsDeleted && !x.IsActive);
        if (filter.ParentDepartmentId.HasValue) query = query.Where(x => x.ParentDepartmentId == filter.ParentDepartmentId);
        if (!string.IsNullOrWhiteSpace(filter.KeyWord))
        {
            var key = filter.KeyWord.Trim();
            query = query.Where(x => x.Name.Contains(key) || x.Code.Contains(key) || (x.Description != null && x.Description.Contains(key)));
        }
        var descending = string.Equals(filter.SortOrder ?? filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (filter.SortField ?? filter.SortBy)?.ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(x => x.Code).ThenBy(x => x.Id) : query.OrderBy(x => x.Code).ThenBy(x => x.Id),
            "createdat" => descending ? query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id) : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            "isactive" => descending ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Id) : query.OrderBy(x => x.IsActive).ThenBy(x => x.Id),
            _ => descending ? query.OrderByDescending(x => x.Name).ThenBy(x => x.Id) : query.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };
        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.PageNumber);
        var size = Math.Clamp(filter.PageSize, 1, 250);
        var records = await query.Skip((page - 1) * size).Take(size).Select(x => new DepartmentDto
        {
            Id=x.Id, CompanyId=x.CompanyId, Name=x.Name, Code=x.Code, Description=x.Description,
            ParentDepartmentId=x.ParentDepartmentId,
            ParentDepartmentName=x.ParentDepartment != null && x.ParentDepartment.TenantId == x.TenantId ? x.ParentDepartment.Name : null,
            IsActive=x.IsActive, IsDeleted=x.IsDeleted, CreatedAt=x.CreatedAt,
            ActiveChildCount=db.Departments.Count(child => child.ParentDepartmentId == x.Id && child.IsActive && !child.IsDeleted),
            ActivePositionCount=db.Positions.Count(position => position.DepartmentId == x.Id && position.IsActive && !position.IsDeleted),
            ActiveUserCount=db.Users.Count(person => person.DepartmentId == x.Id && person.IsActive && !person.IsDeleted)
        }).ToListAsync(cancellationToken);
        return new PagedResult<DepartmentDto> { Total = total, Data = records };
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Departments.AsNoTracking().Where(x => x.Id == id).Select(x => new DepartmentDto
        {
            Id=x.Id, CompanyId=x.CompanyId, Name=x.Name, Code=x.Code, Description=x.Description,
            ParentDepartmentId=x.ParentDepartmentId,
            ParentDepartmentName=x.ParentDepartment != null && x.ParentDepartment.TenantId == x.TenantId ? x.ParentDepartment.Name : null,
            IsActive=x.IsActive, IsDeleted=x.IsDeleted, CreatedAt=x.CreatedAt,
            ActiveChildCount=db.Departments.Count(child => child.ParentDepartmentId == x.Id && child.IsActive && !child.IsDeleted),
            ActivePositionCount=db.Positions.Count(position => position.DepartmentId == x.Id && position.IsActive && !position.IsDeleted),
            ActiveUserCount=db.Users.Count(person => person.DepartmentId == x.Id && person.IsActive && !person.IsDeleted)
        }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentSaveRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, null, cancellationToken);
        var entity = new Department { Id = Guid.NewGuid(), TenantId = user.TenantId };
        Apply(entity, request);
        db.Departments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<DepartmentDto?> UpdateAsync(Guid id, DepartmentSaveRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return null;
        await ValidateAsync(request, id, cancellationToken);
        Apply(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Department was not found.");
        var hasChildren = await db.Departments.AnyAsync(x => x.ParentDepartmentId == id && x.IsActive && !x.IsDeleted, cancellationToken);
        var hasPositions = await db.Positions.AnyAsync(x => x.DepartmentId == id && x.IsActive && !x.IsDeleted, cancellationToken);
        var hasUsers = await db.Users.AnyAsync(x => x.DepartmentId == id && x.IsActive && !x.IsDeleted, cancellationToken);
        if (hasChildren || hasPositions || hasUsers)
            throw new InvalidOperationException("Move active child departments, positions, and users before archiving this department.");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedBy = user.UserId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == user.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Department was not found.");
        if (!entity.IsDeleted) throw new InvalidOperationException("Department is already active.");
        if (await db.Departments.IgnoreQueryFilters().AnyAsync(x => x.TenantId == entity.TenantId && x.CompanyId == entity.CompanyId && x.Code == entity.Code && !x.IsDeleted && x.Id != entity.Id, cancellationToken))
            throw new InvalidOperationException("An active department already uses this code in the company.");
        if (entity.ParentDepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == entity.ParentDepartmentId && x.IsActive && !x.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Restore the parent department before restoring this department.");
        entity.IsDeleted = false; entity.DeletedAt = null; entity.DeletedBy = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<DepartmentDropdownDto>> GetDropdownAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Departments.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.TenantId == user.TenantId && x.IsActive && !x.IsDeleted);
        if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId);
        return query.OrderBy(x => x.Name)
            .Select(x => new DepartmentDropdownDto(x.Id, x.Name, x.Code, x.ParentDepartmentId, x.CompanyId))
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateAsync(DepartmentSaveRequest request, Guid? id, CancellationToken ct)
    {
        var name = request.Name.Trim(); var code = request.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Department name and code are required.");
        if (await db.Departments.IgnoreQueryFilters().AnyAsync(x => x.TenantId == user.TenantId && x.CompanyId == request.CompanyId && x.Code == code && !x.IsDeleted && x.Id != id, ct))
            throw new InvalidOperationException("A department with this code already exists in the company.");
        if (request.CompanyId.HasValue && !await db.Companies.AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct)) throw new InvalidOperationException("Select an active company.");
        if (!request.ParentDepartmentId.HasValue) return;
        if (request.ParentDepartmentId == id) throw new InvalidOperationException("A department cannot be its own parent.");
        var parent = await db.Departments.FirstOrDefaultAsync(x => x.Id == request.ParentDepartmentId && x.IsActive && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Parent department must exist and be active.");
        if (parent.CompanyId != request.CompanyId) throw new InvalidOperationException("Parent and child departments must belong to the same company.");
        var visited = new HashSet<Guid>(); var current = parent.ParentDepartmentId;
        while (current.HasValue)
        {
            if (current == id) throw new InvalidOperationException("Circular department hierarchy is not allowed.");
            if (!visited.Add(current.Value)) throw new InvalidOperationException("Existing circular department hierarchy must be resolved first.");
            current = await db.Departments.Where(x => x.Id == current.Value).Select(x => x.ParentDepartmentId).FirstOrDefaultAsync(ct);
        }
    }

    private static void Apply(Department entity, DepartmentSaveRequest request)
    {
        entity.CompanyId=request.CompanyId; entity.Name=request.Name.Trim(); entity.Code=request.Code.Trim().ToUpperInvariant();
        entity.Description=request.Description?.Trim(); entity.ParentDepartmentId=request.ParentDepartmentId; entity.IsActive=request.IsActive;
    }

}
