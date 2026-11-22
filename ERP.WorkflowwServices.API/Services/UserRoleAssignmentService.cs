using System.Data;
using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class UserRoleAssignmentService(WorkflowDbContext db, IUserContext user) : IUserRoleAssignmentService
{
    public async Task<PagedResult<UserRoleAssignmentDto>> GetAllAsync(UserRoleAssignmentFilter filter)
    {
        var today = DateTime.UtcNow.Date;
        var query = ActiveTenantAssignments();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(assignment =>
                assignment.User!.FullName.Contains(search)
                || assignment.User.Username.Contains(search)
                || (assignment.User.Email != null && assignment.User.Email.Contains(search))
                || (assignment.User.EmployeeCode != null && assignment.User.EmployeeCode.Contains(search))
                || (assignment.User.Position != null && assignment.User.Position.PositionName.Contains(search)));
        }

        if (filter.DepartmentId.HasValue)
            query = query.Where(assignment => assignment.User!.DepartmentId == filter.DepartmentId);
        if (filter.RoleId.HasValue)
            query = query.Where(assignment => assignment.RoleId == filter.RoleId);
        if (filter.EffectiveFrom.HasValue)
        {
            var from = filter.EffectiveFrom.Value.Date;
            query = query.Where(assignment => !assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= from);
        }
        if (filter.EffectiveTo.HasValue)
        {
            var to = filter.EffectiveTo.Value.Date;
            query = query.Where(assignment => assignment.EffectiveFrom <= to);
        }

        query = filter.Status?.Trim().ToLowerInvariant() switch
        {
            "active" => query.Where(assignment => assignment.IsActive
                && assignment.User!.IsActive && assignment.Role!.IsActive
                && assignment.EffectiveFrom <= today
                && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= today)),
            "upcoming" => query.Where(assignment => assignment.IsActive
                && assignment.User!.IsActive && assignment.Role!.IsActive && assignment.EffectiveFrom > today),
            "expired" => query.Where(assignment => assignment.IsActive
                && assignment.User!.IsActive && assignment.Role!.IsActive
                && assignment.EffectiveTo.HasValue && assignment.EffectiveTo.Value < today),
            "inactive" => query.Where(assignment => !assignment.IsActive
                || !assignment.User!.IsActive || !assignment.Role!.IsActive),
            _ => query,
        };

        var total = await query.CountAsync();
        query = ApplySort(query, filter.SortColumn, filter.SortDirection);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var records = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<UserRoleAssignmentDto>
        {
            Total = total,
            Data = await MapAsync(records),
        };
    }

    public async Task<UserRoleAssignmentSummaryDto> GetSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var query = ActiveTenantAssignments();
        var current = query.Where(assignment => assignment.IsActive
            && assignment.User!.IsActive && assignment.Role!.IsActive
            && assignment.EffectiveFrom <= today
            && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= today));

        return new UserRoleAssignmentSummaryDto
        {
            UsersWithAssignedRoles = await current.Select(assignment => assignment.UserId).Distinct().CountAsync(),
            ActiveAssignments = await current.CountAsync(),
            UpcomingAssignments = await query.CountAsync(assignment => assignment.IsActive
                && assignment.User!.IsActive && assignment.Role!.IsActive && assignment.EffectiveFrom > today),
            ExpiredAssignments = await query.CountAsync(assignment => assignment.IsActive
                && assignment.User!.IsActive && assignment.Role!.IsActive
                && assignment.EffectiveTo.HasValue && assignment.EffectiveTo.Value < today),
            InactiveAssignments = await query.CountAsync(assignment => !assignment.IsActive
                || !assignment.User!.IsActive || !assignment.Role!.IsActive),
        };
    }

    public async Task<IReadOnlyList<UserRoleAssignmentUserOptionDto>> GetUsersAsync(
        string? search, int pageNumber, int pageSize)
    {
        var query = db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.TenantId == user.TenantId && !item.IsDeleted && item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item => item.FullName.Contains(value)
                || item.Username.Contains(value)
                || (item.Email != null && item.Email.Contains(value))
                || (item.EmployeeCode != null && item.EmployeeCode.Contains(value)));
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);
        return await query.OrderBy(item => item.FullName).ThenBy(item => item.Username)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(item => new UserRoleAssignmentUserOptionDto
            {
                Id = item.Id,
                FullName = item.FullName,
                Username = item.Username,
                Email = item.Email,
                EmployeeCode = item.EmployeeCode,
                DepartmentId = item.DepartmentId,
                DepartmentName = db.Departments.IgnoreQueryFilters()
                    .Where(department => department.Id == item.DepartmentId
                        && department.TenantId == user.TenantId && !department.IsDeleted)
                    .Select(department => department.Name)
                    .FirstOrDefault(),
                PositionId = item.PositionId,
                PositionName = db.Positions.IgnoreQueryFilters()
                    .Where(position => position.Id == item.PositionId
                        && position.TenantId == user.TenantId && !position.IsDeleted)
                    .Select(position => position.PositionName)
                    .FirstOrDefault(),
                IsActive = item.IsActive,
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<UserRoleAssignmentRoleOptionDto>> GetRolesAsync()
    {
        return await db.Roles.IgnoreQueryFilters().AsNoTracking()
            .Where(role => role.TenantId == user.TenantId && !role.IsDeleted && role.IsActive
                && (user.IsSuperAdmin || (!role.IsSystem
                    && !string.Equals(role.ScopeLevel, "Platform")
                    && !string.Equals(role.Code, "SUPER_ADMIN")
                    && (!user.IsCompanyAdmin || !string.Equals(role.Code, "COMPANY_ADMIN")))))
            .OrderBy(role => role.Priority).ThenBy(role => role.Name)
            .Select(role => new UserRoleAssignmentRoleOptionDto
            {
                Id = role.Id,
                Name = role.Name,
                Code = role.Code,
                Description = role.Description,
                TenantId = role.TenantId,
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<UserRoleAssignmentDepartmentOptionDto>> GetDepartmentsAsync()
    {
        return await db.Departments.IgnoreQueryFilters().AsNoTracking()
            .Where(department => department.TenantId == user.TenantId
                && !department.IsDeleted && department.IsActive)
            .OrderBy(department => department.Name)
            .Select(department => new UserRoleAssignmentDepartmentOptionDto
            {
                Id = department.Id,
                Name = department.Name,
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<UserRoleAssignmentDto>> GetByUserAsync(Guid userId)
    {
        await EnsureUserExistsAsync(userId);
        var records = await db.UserRoleAssignments.IgnoreQueryFilters().AsNoTracking()
            .Where(assignment => assignment.TenantId == user.TenantId && assignment.UserId == userId)
            .Include(assignment => assignment.User)
            .Include(assignment => assignment.Role)
            .OrderByDescending(assignment => assignment.EffectiveFrom)
            .ThenBy(assignment => assignment.Role!.Name)
            .ToListAsync();
        return await MapAsync(records);
    }

    public async Task<UserRoleAssignmentDto?> GetByIdAsync(Guid id)
    {
        var record = await db.UserRoleAssignments.IgnoreQueryFilters().AsNoTracking()
            .Where(assignment => assignment.TenantId == user.TenantId && assignment.Id == id)
            .Include(assignment => assignment.User)
            .Include(assignment => assignment.Role)
            .FirstOrDefaultAsync();
        return record is null ? null : (await MapAsync([record])).Single();
    }

    public async Task<IReadOnlyList<UserRoleAssignmentHistoryDto>> GetHistoryAsync(Guid userId)
    {
        await EnsureUserExistsAsync(userId);
        var records = await db.UserRoleAssignments.IgnoreQueryFilters().AsNoTracking()
            .Where(assignment => assignment.TenantId == user.TenantId && assignment.UserId == userId)
            .Include(assignment => assignment.Role)
            .OrderByDescending(assignment => assignment.DeactivatedAt ?? assignment.CreatedAt)
            .ToListAsync();
        var actorIds = records.Select(assignment => assignment.DeactivatedBy ?? assignment.CreatedBy)
            .Where(id => id != Guid.Empty).Distinct().ToList();
        var actorNames = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(actor => actorIds.Contains(actor.Id) && actor.TenantId == user.TenantId && !actor.IsDeleted)
            .ToDictionaryAsync(actor => actor.Id, actor => actor.FullName);

        return records.Select(assignment =>
        {
            var changedBy = assignment.DeactivatedBy ?? assignment.CreatedBy;
            return new UserRoleAssignmentHistoryDto
            {
                Id = assignment.Id,
                RoleId = assignment.RoleId,
                RoleName = assignment.Role?.Name ?? string.Empty,
                Action = assignment.IsActive ? "Assigned" : "Deactivated",
                EffectiveFrom = assignment.EffectiveFrom,
                EffectiveTo = assignment.EffectiveTo,
                Remarks = assignment.Remarks,
                ChangedAt = assignment.DeactivatedAt ?? assignment.CreatedAt,
                ChangedBy = changedBy,
                ChangedByName = actorNames.GetValueOrDefault(changedBy) ?? "System / legacy import",
                DeactivatedAt = assignment.DeactivatedAt,
                DeactivatedBy = assignment.DeactivatedBy,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<UserRoleAssignmentDto>> SaveForUserAsync(
        Guid userId, IReadOnlyCollection<UserRoleAssignmentInput> assignments)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var targetUser = await FindEligibleUserAsync(userId)
            ?? throw new KeyNotFoundException("Active user was not found in this tenant.");
        var requested = assignments.ToList();
        if (requested.Count == 0)
            throw new InvalidOperationException("Assign at least one role.");
        if (requested.Select(item => item.RoleId).Distinct().Count() != requested.Count)
            throw new InvalidOperationException("A role can only be assigned once in this request.");

        foreach (var item in requested)
        {
            if (item.RoleId == Guid.Empty)
                throw new InvalidOperationException("Each assignment must include a role.");
            var from = item.EffectiveFrom.Date;
            if (from == DateTime.MinValue.Date)
                throw new InvalidOperationException("Effective From is required.");
            if (item.EffectiveTo.HasValue && item.EffectiveTo.Value.Date < from)
                throw new InvalidOperationException("Effective To cannot be earlier than Effective From.");
        }

        var roleIds = requested.Select(item => item.RoleId).Distinct().ToHashSet();
        var activeRoles = await db.Roles.IgnoreQueryFilters()
            .Where(role => roleIds.Contains(role.Id) && role.TenantId == user.TenantId
                && !role.IsDeleted && role.IsActive)
            .ToListAsync();
        if (activeRoles.Count != roleIds.Count)
            throw new InvalidOperationException("One or more selected roles are inactive or unavailable.");
        if (!user.IsSuperAdmin && activeRoles.Any(role => role.IsSystem
                || string.Equals(role.ScopeLevel, "Platform", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role.Code, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Platform roles cannot be assigned by tenant or company administrators.");
        if (user.IsCompanyAdmin && activeRoles.Any(role =>
                string.Equals(role.Code, "COMPANY_ADMIN", StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Company administrators cannot assign the Company Admin role.");
        if (activeRoles.Any(role => string.Equals(role.ScopeLevel, "Company", StringComparison.OrdinalIgnoreCase)
                && !targetUser.CompanyId.HasValue))
            throw new InvalidOperationException("Company-scoped roles require a company assignment.");

        var existing = await db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(assignment => assignment.TenantId == user.TenantId && assignment.UserId == userId)
            .ToListAsync();
        if (requested.Where(item => item.Id.HasValue).Any(item =>
                existing.All(assignment => assignment.Id != item.Id || assignment.RoleId != item.RoleId)))
            throw new InvalidOperationException("An assignment identifier does not belong to this user and role.");
        var today = DateTime.UtcNow.Date;
        var currentAssignments = existing.Where(assignment => assignment.IsActive
            && !assignment.IsDeleted
            && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value.Date >= today)).ToList();
        var keepIds = new HashSet<Guid>();
        var replacements = new List<UserRoleAssignment>();
        var now = DateTime.UtcNow;

        foreach (var item in requested)
        {
            var from = item.EffectiveFrom.Date;
            var to = item.EffectiveTo?.Date;
            var matching = currentAssignments.FirstOrDefault(assignment => assignment.RoleId == item.RoleId
                && assignment.EffectiveFrom.Date == from
                && assignment.EffectiveTo?.Date == to
                && assignment.IsActive == item.IsActive
                && string.Equals(assignment.Remarks, Normalize(item.Remarks), StringComparison.Ordinal));
            if (matching is not null)
            {
                keepIds.Add(matching.Id);
                continue;
            }

            var overlap = existing.Any(assignment =>
                assignment.RoleId == item.RoleId
                && assignment.IsActive
                && !assignment.IsDeleted
                && !currentAssignments.Any(current => current.Id == assignment.Id)
                && RangesOverlap(from, to, assignment.EffectiveFrom.Date, assignment.EffectiveTo?.Date));
            if (item.IsActive && overlap)
                throw new InvalidOperationException("This role already has an active assignment with overlapping effective dates.");

            replacements.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = userId,
                RoleId = item.RoleId,
                EffectiveFrom = from,
                EffectiveTo = to,
                IsActive = item.IsActive,
                Remarks = Normalize(item.Remarks),
                CreatedAt = now,
                CreatedBy = user.UserId,
                DeactivatedAt = item.IsActive ? null : now,
                DeactivatedBy = item.IsActive ? null : user.UserId,
            });
        }

        foreach (var assignment in currentAssignments.Where(assignment => !keepIds.Contains(assignment.Id)))
            Deactivate(assignment, now);
        db.UserRoleAssignments.AddRange(replacements);
        await db.SaveChangesAsync();
        await SyncLegacyProjectionAsync(targetUser);
        await db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        return await GetByUserAsync(userId);
    }

    public async Task DeactivateAsync(Guid id) => await BulkDeactivateAsync([id]);

    public async Task<int> BulkDeactivateAsync(IReadOnlyCollection<Guid> ids)
    {
        var selectedIds = ids.Distinct().ToHashSet();
        if (selectedIds.Count == 0) return 0;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var records = await db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(assignment => selectedIds.Contains(assignment.Id)
                && assignment.TenantId == user.TenantId)
            .ToListAsync();
        if (records.Count != selectedIds.Count)
            throw new KeyNotFoundException("One or more assignments were not found.");
        if (records.Any(assignment => !assignment.IsActive || assignment.IsDeleted))
            throw new InvalidOperationException("One or more selected assignments are already inactive.");

        var now = DateTime.UtcNow;
        foreach (var assignment in records)
            Deactivate(assignment, now);

        await db.SaveChangesAsync();
        var users = await db.Users.IgnoreQueryFilters()
            .Where(item => records.Select(record => record.UserId).Contains(item.Id)
                && item.TenantId == user.TenantId && !item.IsDeleted)
            .ToListAsync();
        foreach (var targetUser in users)
            await SyncLegacyProjectionAsync(targetUser);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return records.Count;
    }

    public async Task SyncLegacyRolesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        bool allowInactiveUser = false)
    {
        var requestedIds = roleIds.Distinct().ToHashSet();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Select at least one role.");

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var targetUser = await FindEligibleUserAsync(userId, allowInactiveUser)
            ?? throw new KeyNotFoundException("Active user was not found in this tenant.");
        var roles = await db.Roles.IgnoreQueryFilters()
            .Where(role => requestedIds.Contains(role.Id) && role.TenantId == user.TenantId
                && !role.IsDeleted && role.IsActive)
            .ToListAsync();
        if (roles.Count != requestedIds.Count)
            throw new InvalidOperationException("One or more selected roles are inactive or unavailable.");

        var existing = await db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(assignment => assignment.TenantId == user.TenantId && assignment.UserId == userId)
            .ToListAsync();
        var today = DateTime.UtcNow.Date;
        var current = existing.Where(assignment => assignment.IsActive && !assignment.IsDeleted
            && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value.Date >= today)).ToList();
        var now = DateTime.UtcNow;
        foreach (var assignment in current.Where(assignment => !requestedIds.Contains(assignment.RoleId)))
            Deactivate(assignment, now);

        var currentRoleIds = current.Select(assignment => assignment.RoleId).ToHashSet();
        foreach (var role in roles.Where(role => !currentRoleIds.Contains(role.Id)))
        {
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = userId,
                RoleId = role.Id,
                CompanyId = string.Equals(role.ScopeLevel, "Company", StringComparison.OrdinalIgnoreCase)
                    ? targetUser.CompanyId
                    : null,
                IsDefault = role.Id == targetUser.RoleId,
                EffectiveFrom = today,
                IsActive = true,
                Remarks = "Assigned through user role mapping",
                CreatedAt = now,
                CreatedBy = user.UserId,
            });
        }

        await db.SaveChangesAsync();
        await SyncLegacyProjectionAsync(targetUser);
        await db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    private IQueryable<UserRoleAssignment> ActiveTenantAssignments()
    {
        return db.UserRoleAssignments.IgnoreQueryFilters().AsNoTracking()
            .Include(assignment => assignment.User)
            .Include(assignment => assignment.Role)
            .Where(assignment => assignment.TenantId == user.TenantId
                && !assignment.IsDeleted
                && assignment.User != null && assignment.User.TenantId == user.TenantId
                    && !assignment.User.IsDeleted
                && assignment.Role != null && assignment.Role.TenantId == user.TenantId
                    && !assignment.Role.IsDeleted);
    }

    private async Task<List<UserRoleAssignmentDto>> MapAsync(List<UserRoleAssignment> records)
    {
        var departmentIds = records.Select(record => record.User?.DepartmentId)
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var departmentNames = await db.Departments.IgnoreQueryFilters().AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id)
                && department.TenantId == user.TenantId && !department.IsDeleted)
            .ToDictionaryAsync(department => department.Id, department => department.Name);
        var positionIds = records.Select(record => record.User?.PositionId)
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var positionNames = await db.Positions.IgnoreQueryFilters().AsNoTracking()
            .Where(position => positionIds.Contains(position.Id)
                && position.TenantId == user.TenantId && !position.IsDeleted)
            .ToDictionaryAsync(position => position.Id, position => position.PositionName);
        var today = DateTime.UtcNow.Date;

        return records.Select(record =>
        {
            var assignmentStatus = GetStatus(record, today);
            return new UserRoleAssignmentDto
            {
                Id = record.Id,
                UserId = record.UserId,
                FullName = record.User?.FullName ?? string.Empty,
                Username = record.User?.Username ?? string.Empty,
                Email = record.User?.Email,
                UserIsActive = record.User?.IsActive ?? false,
                DepartmentId = record.User?.DepartmentId,
                DepartmentName = record.User?.DepartmentId is Guid departmentId
                    && departmentNames.TryGetValue(departmentId, out var departmentName) ? departmentName : null,
                EmployeeCode = record.User?.EmployeeCode,
                PositionId = record.User?.PositionId,
                PositionName = record.User?.PositionId is long positionId
                    && positionNames.TryGetValue(positionId, out var positionName) ? positionName : null,
                RoleId = record.RoleId,
                RoleName = record.Role?.Name ?? string.Empty,
                RoleCode = record.Role?.Code,
                RoleDescription = record.Role?.Description,
                EffectiveFrom = record.EffectiveFrom,
                EffectiveTo = record.EffectiveTo,
                IsActive = record.IsActive,
                AssignmentStatus = assignmentStatus,
                Remarks = record.Remarks,
                CreatedAt = record.CreatedAt,
                CreatedBy = record.CreatedBy,
                DeactivatedAt = record.DeactivatedAt,
                DeactivatedBy = record.DeactivatedBy,
            };
        }).ToList();
    }

    private static IQueryable<UserRoleAssignment> ApplySort(
        IQueryable<UserRoleAssignment> query, string? sortColumn, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (sortColumn ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "fullname" => descending ? query.OrderByDescending(item => item.User!.FullName) : query.OrderBy(item => item.User!.FullName),
            "email" => descending ? query.OrderByDescending(item => item.User!.Email) : query.OrderBy(item => item.User!.Email),
            "department" => descending ? query.OrderByDescending(item => item.User!.DepartmentId) : query.OrderBy(item => item.User!.DepartmentId),
            "position" => descending ? query.OrderByDescending(item => item.User!.Position!.PositionName) : query.OrderBy(item => item.User!.Position!.PositionName),
            "role" => descending ? query.OrderByDescending(item => item.Role!.Name) : query.OrderBy(item => item.Role!.Name),
            "effectiveto" => descending ? query.OrderByDescending(item => item.EffectiveTo) : query.OrderBy(item => item.EffectiveTo),
            "status" => descending ? query.OrderByDescending(item => item.IsActive) : query.OrderBy(item => item.IsActive),
            _ => descending ? query.OrderByDescending(item => item.EffectiveFrom) : query.OrderBy(item => item.EffectiveFrom),
        };
        return ordered.ThenBy(item => item.Id);
    }

    private static string GetStatus(UserRoleAssignment assignment, DateTime today)
    {
        if (!assignment.IsActive || assignment.User?.IsActive != true || assignment.Role?.IsActive != true)
            return "Inactive";
        if (assignment.EffectiveFrom.Date > today) return "Upcoming";
        if (assignment.EffectiveTo.HasValue && assignment.EffectiveTo.Value.Date < today) return "Expired";
        return "Active";
    }

    private async Task<Users?> FindEligibleUserAsync(Guid userId, bool allowInactiveUser = false)
    {
        return await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == userId && item.TenantId == user.TenantId
                && !item.IsDeleted && (allowInactiveUser || item.IsActive));
    }

    private async Task EnsureUserExistsAsync(Guid userId)
    {
        var exists = await db.Users.IgnoreQueryFilters().AnyAsync(item =>
            item.Id == userId && item.TenantId == user.TenantId && !item.IsDeleted);
        if (!exists) throw new KeyNotFoundException("User was not found in this tenant.");
    }

    private async Task SyncLegacyProjectionAsync(Users targetUser)
    {
        var today = DateTime.UtcNow.Date;
        var roleIds = await db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(assignment => assignment.TenantId == user.TenantId
                && assignment.UserId == targetUser.Id
                && !assignment.IsDeleted
                && assignment.IsActive
                && assignment.EffectiveFrom <= today
                && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= today))
            .Join(db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == user.TenantId
                    && !role.IsDeleted && role.IsActive),
                assignment => assignment.RoleId,
                role => role.Id,
                (_, role) => role.Id)
            .Distinct()
            .ToListAsync();

        var previousLinks = await db.UserRoles.IgnoreQueryFilters()
            .Where(link => link.UserId == targetUser.Id && link.TenantId == user.TenantId)
            .ToListAsync();
        var previousRoleIds = previousLinks.Select(link => link.RoleId).ToHashSet();
        if (!previousRoleIds.SetEquals(roleIds))
        {
            var refreshTokens = await db.RefreshTokens.IgnoreQueryFilters()
                .Where(token => token.UserId == targetUser.Id && token.TenantId == user.TenantId && !token.IsRevoked)
                .ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = now;
                token.ReasonRevoked = "User role assignments changed";
            }
        }
        db.UserRoles.RemoveRange(previousLinks);
        foreach (var roleId in roleIds)
            db.UserRoles.Add(new UserRole { UserId = targetUser.Id, RoleId = roleId, TenantId = user.TenantId });
        if (roleIds.Contains(targetUser.RoleId) == false && roleIds.Count > 0)
            targetUser.RoleId = roleIds[0];
    }

    private void Deactivate(UserRoleAssignment assignment, DateTime at)
    {
        assignment.IsActive = false;
        assignment.DeactivatedAt = at;
        assignment.DeactivatedBy = user.UserId;
        assignment.UpdatedAt = at;
        assignment.UpdatedBy = user.UserId;
    }

    private static bool RangesOverlap(DateTime from, DateTime? to, DateTime otherFrom, DateTime? otherTo)
    {
        return (!to.HasValue || otherFrom <= to.Value)
            && (!otherTo.HasValue || from <= otherTo.Value);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
