using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class UserDashboardService(WorkflowDbContext db, IUserContext user) : IUserDashboardService
{
    public async Task<UserDashboardResponse> GetDashboardAsync(
        UserDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
            throw new ArgumentException("FromDate cannot be later than ToDate.", nameof(filter));

        var users = await BuildScopedQueryAsync(filter, cancellationToken);
        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(filter.UserType))
            users = users.Where(item => item.UserType == filter.UserType);

        users = filter.Status?.Trim().ToLowerInvariant() switch
        {
            "active" => users.Where(item => item.IsActive && !item.IsLocked
                && (!item.LockoutEnd.HasValue || item.LockoutEnd <= now)),
            "inactive" => users.Where(item => !item.IsActive),
            "pending" => users.Where(item => item.RequiresApproval && !item.IsActive),
            "locked" => users.Where(item => item.IsLocked || (item.LockoutEnd.HasValue && item.LockoutEnd > now)),
            "neverloggedin" => users.Where(item => item.LastLoginAt == null),
            _ => users,
        };

        var userIds = users.Select(item => item.Id);
        var summary = await users.GroupBy(_ => 1)
            .Select(group => new UserDashboardSummary(
                group.Count(),
                group.Count(item => item.IsActive && !item.IsLocked
                    && (!item.LockoutEnd.HasValue || item.LockoutEnd <= now)),
                group.Count(item => !item.IsActive),
                group.Count(item => item.RequiresApproval && !item.IsActive),
                0,
                group.Count(item => item.IsLocked || (item.LockoutEnd.HasValue && item.LockoutEnd > now)),
                0,
                group.Count(item => item.LastLoginAt == null)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new UserDashboardSummary(0, 0, 0, 0, 0, 0, 0, 0);

        var newUsersFrom = filter.FromDate?.UtcDateTime ?? now.AddDays(-30);
        var newUsersTo = filter.ToDate?.UtcDateTime ?? now.AddTicks(1);
        summary = summary with
        {
            NewUsers = await users.CountAsync(
                item => item.CreatedAt >= newUsersFrom && item.CreatedAt < newUsersTo,
                cancellationToken)
        };

        var invitationQuery = db.UserInvitations.IgnoreQueryFilters().AsNoTracking()
            .Where(item => !item.IsDeleted && userIds.Contains(item.UserId));
        var pendingInvitations = await invitationQuery.CountAsync(
            item => item.AcceptedAt == null && item.RevokedAt == null && item.ExpiresAt > now,
            cancellationToken);
        var expiredInvitations = await invitationQuery.CountAsync(
            item => item.AcceptedAt == null && item.RevokedAt == null && item.ExpiresAt <= now,
            cancellationToken);
        summary = summary with { PendingInvitations = pendingInvitations };

        var trend = await GetTrendAsync(users, filter, cancellationToken);
        var status = await GetStatusDistributionAsync(users, now, cancellationToken);
        var userTypeRows = await users.Where(item => item.UserType != null && item.UserType != "")
            .GroupBy(item => item.UserType)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);
        var userTypes = userTypeRows
            .Select(item => new UserDashboardCount(item.Name, item.Count))
            .ToList();
        var organizations = await GetOrganizationsAsync(users, cancellationToken);

        var attention = new UserDashboardAttention(
            summary.PendingApprovals,
            pendingInvitations,
            expiredInvitations,
            summary.LockedAccounts,
            await users.CountAsync(item => item.RoleId == Guid.Empty
                && !item.UserRoles.Any() && !item.UserRoleAssignments.Any(assignment => assignment.IsActive && !assignment.IsDeleted),
                cancellationToken),
            await users.CountAsync(item => item.CompanyId == null
                && !item.CompanyAccesses.Any(access => access.IsActive && !access.IsDeleted),
                cancellationToken),
            await users.CountAsync(item => item.PositionId == null
                && !item.PositionAssignments.Any(assignment => assignment.IsActive && !assignment.IsDeleted),
                cancellationToken),
            await users.CountAsync(item => item.AccountExpiresAt > now && item.AccountExpiresAt <= now.AddDays(30),
                cancellationToken));

        var security = new UserDashboardSecurity(
            await users.SumAsync(item => (int?)item.AccessFailedCount, cancellationToken) ?? 0,
            summary.LockedAccounts,
            summary.NeverLoggedIn,
            summary.InactiveUsers,
            await users.CountAsync(item => item.TwoFactorEnabled, cancellationToken),
            summary.TotalUsers);

        var activities = await GetRecentActivitiesAsync(users, filter, cancellationToken);
        return new UserDashboardResponse(
            summary, trend, status, userTypes, organizations, attention, security, activities);
    }

    private async Task<IQueryable<Users>> BuildScopedQueryAsync(
        UserDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(item => !item.IsDeleted && item.Id != user.UserId);
        List<Guid>? allowedCompanyIds = null;

        if (!user.IsSuperAdmin)
            query = query.Where(item => item.TenantId == user.TenantId);
        else if (filter.TenantId.HasValue)
        {
            if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(
                    tenant => tenant.Id == filter.TenantId.Value && tenant.IsActive && !tenant.IsDeleted,
                    cancellationToken))
                throw new ArgumentException("The selected tenant is unavailable.", nameof(filter));
            query = query.Where(item => item.TenantId == filter.TenantId.Value);
        }

        if (user.IsCompanyAdmin)
        {
            var actorCompanyId = await db.Users.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.Id == user.UserId && item.TenantId == user.TenantId && !item.IsDeleted)
                .Select(item => item.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (user.CompanyId.HasValue && actorCompanyId != user.CompanyId)
                throw new UnauthorizedAccessException("The signed-in company assignment is no longer valid.");

            allowedCompanyIds = await db.UserCompanyAccesses.IgnoreQueryFilters().AsNoTracking()
                .Where(access => access.TenantId == user.TenantId && access.UserId == user.UserId
                    && access.IsActive && !access.IsDeleted && access.EffectiveFrom <= DateTime.UtcNow
                    && (!access.EffectiveTo.HasValue || access.EffectiveTo.Value >= DateTime.UtcNow))
                .Select(access => access.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);
            if (actorCompanyId.HasValue)
                allowedCompanyIds.Add(actorCompanyId.Value);
            allowedCompanyIds = allowedCompanyIds.Distinct().ToList();

            if (filter.CompanyId.HasValue && !allowedCompanyIds.Contains(filter.CompanyId.Value))
                throw new UnauthorizedAccessException("You cannot access dashboard data for that company.");
            query = query.Where(item => item.CompanyId.HasValue && allowedCompanyIds.Contains(item.CompanyId.Value));
        }
        else if (filter.CompanyId.HasValue)
        {
            var companyQuery = db.Companies.IgnoreQueryFilters().AsNoTracking()
                .Where(company => company.Id == filter.CompanyId.Value && company.IsActive && !company.IsDeleted);
            if (!user.IsSuperAdmin)
                companyQuery = companyQuery.Where(company => company.TenantId == user.TenantId);
            if (filter.TenantId.HasValue)
                companyQuery = companyQuery.Where(company => company.TenantId == filter.TenantId.Value);
            if (!await companyQuery.AnyAsync(cancellationToken))
                throw new ArgumentException("The selected company is unavailable in your scope.", nameof(filter));
            query = query.Where(item => item.CompanyId == filter.CompanyId.Value);
        }

        if (filter.BranchId.HasValue)
        {
            var branchQuery = db.Branches.IgnoreQueryFilters().AsNoTracking()
                .Where(branch => branch.Id == filter.BranchId.Value && branch.IsActive && !branch.IsDeleted);
            if (!user.IsSuperAdmin)
                branchQuery = branchQuery.Where(branch => branch.TenantId == user.TenantId);
            if (filter.TenantId.HasValue)
                branchQuery = branchQuery.Where(branch => branch.TenantId == filter.TenantId.Value);
            if (allowedCompanyIds is not null)
                branchQuery = branchQuery.Where(branch => allowedCompanyIds.Contains(branch.CompanyId));
            if (filter.CompanyId.HasValue)
                branchQuery = branchQuery.Where(branch => branch.CompanyId == filter.CompanyId.Value);
            if (!await branchQuery.AnyAsync(cancellationToken))
                throw new UnauthorizedAccessException("The selected branch is outside your authorized scope.");
            query = query.Where(item => item.BranchId == filter.BranchId.Value);
        }

        return query;
    }

    private static async Task<IReadOnlyList<UserDashboardTrendPoint>> GetTrendAsync(
        IQueryable<Users> users,
        UserDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        var from = filter.FromDate?.UtcDateTime ?? DateTime.UtcNow.AddMonths(-11);
        var to = filter.ToDate?.UtcDateTime ?? DateTime.UtcNow.AddTicks(1);
        users = users.Where(item => item.CreatedAt >= from && item.CreatedAt < to);
        var daily = (to - from).TotalDays <= 45;
        var points = await users.GroupBy(item => new
            {
                item.CreatedAt.Year,
                item.CreatedAt.Month,
                Day = daily ? item.CreatedAt.Day : 1
            })
            .Select(group => new { group.Key.Year, group.Key.Month, group.Key.Day, Count = group.Count() })
            .OrderBy(item => item.Year).ThenBy(item => item.Month).ThenBy(item => item.Day)
            .ToListAsync(cancellationToken);
        return points.Select(point => new UserDashboardTrendPoint(
            new DateTimeOffset(point.Year, point.Month, point.Day, 0, 0, 0, TimeSpan.Zero),
            point.Count)).ToList();
    }

    private static async Task<IReadOnlyList<UserDashboardCount>> GetStatusDistributionAsync(
        IQueryable<Users> users,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var counts = await users.GroupBy(item =>
                item.IsLocked || (item.LockoutEnd.HasValue && item.LockoutEnd > now)
                    ? "Locked"
                    : item.RequiresApproval && !item.IsActive
                        ? "Pending approval"
                        : item.IsActive ? "Active" : "Inactive")
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Name, item => item.Count, cancellationToken);
        return
        [
            new UserDashboardCount("Active", counts.GetValueOrDefault("Active")),
            new UserDashboardCount("Inactive", counts.GetValueOrDefault("Inactive")),
            new UserDashboardCount("Pending approval", counts.GetValueOrDefault("Pending approval")),
            new UserDashboardCount("Locked", counts.GetValueOrDefault("Locked"))
        ];
    }

    private async Task<UserDashboardOrganizations> GetOrganizationsAsync(
        IQueryable<Users> users,
        CancellationToken cancellationToken)
    {
        var tenantItems = new List<UserDashboardOrganizationItem>();
        var companyItems = new List<UserDashboardOrganizationItem>();
        var branchItems = new List<UserDashboardOrganizationItem>();
        var departmentItems = new List<UserDashboardOrganizationItem>();
        var positionItems = new List<UserDashboardOrganizationItem>();

        if (user.IsSuperAdmin)
        {
            var tenantCounts = await users.GroupBy(item => item.TenantId)
                .Select(group => new { Id = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
            var tenantIds = tenantCounts.Select(item => item.Id).ToList();
            var tenantNames = await db.Tenants.IgnoreQueryFilters().AsNoTracking()
                .Where(item => tenantIds.Contains(item.Id) && !item.IsDeleted)
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
            tenantItems.AddRange(tenantCounts.Select(item => new UserDashboardOrganizationItem(
                item.Id.ToString(), tenantNames.GetValueOrDefault(item.Id) ?? item.Id.ToString(), item.Count)));
        }

        {
            var companyCounts = await users.Where(item => item.CompanyId.HasValue)
                .GroupBy(item => item.CompanyId!.Value)
                .Select(group => new { Id = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
            var companyIds = companyCounts.Select(item => item.Id).ToList();
            var companies = await db.Companies.IgnoreQueryFilters().AsNoTracking()
                .Where(item => companyIds.Contains(item.Id) && !item.IsDeleted)
                .Select(item => new { item.Id, item.Name, item.ParentCompanyId })
                .ToDictionaryAsync(item => item.Id, cancellationToken);
            companyItems.AddRange(companyCounts.Select(item =>
            {
                var company = companies.GetValueOrDefault(item.Id);
                return new UserDashboardOrganizationItem(item.Id.ToString(), company?.Name ?? item.Id.ToString(),
                    item.Count, company?.ParentCompanyId.HasValue ?? false);
            }));

            var departmentCounts = await users.Where(item => item.DepartmentId.HasValue)
                .GroupBy(item => item.DepartmentId!.Value)
                .Select(group => new { Id = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
            var departmentIds = departmentCounts.Select(item => item.Id).ToList();
            var departments = await db.Departments.IgnoreQueryFilters().AsNoTracking()
                .Where(item => departmentIds.Contains(item.Id) && !item.IsDeleted)
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
            departmentItems.AddRange(departmentCounts.Select(item => new UserDashboardOrganizationItem(
                item.Id.ToString(), departments.GetValueOrDefault(item.Id) ?? item.Id.ToString(), item.Count)));

            var positionCounts = await users.Where(item => item.PositionId.HasValue)
                .GroupBy(item => item.PositionId!.Value)
                .Select(group => new { Id = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
            var positionIds = positionCounts.Select(item => item.Id).ToList();
            var positions = await db.Positions.IgnoreQueryFilters().AsNoTracking()
                .Where(item => positionIds.Contains(item.Id) && !item.IsDeleted)
                .ToDictionaryAsync(item => item.Id, item => item.PositionName, cancellationToken);
            positionItems.AddRange(positionCounts.Select(item => new UserDashboardOrganizationItem(
                item.Id.ToString(), positions.GetValueOrDefault(item.Id) ?? item.Id.ToString(), item.Count)));
        }

        var branchCounts = await users.Where(item => item.BranchId.HasValue)
            .GroupBy(item => item.BranchId!.Value)
            .Select(group => new { Id = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
        var branchIds = branchCounts.Select(item => item.Id).ToList();
        var branches = await db.Branches.IgnoreQueryFilters().AsNoTracking()
            .Where(item => branchIds.Contains(item.Id) && !item.IsDeleted)
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        branchItems.AddRange(branchCounts.Select(item => new UserDashboardOrganizationItem(
            item.Id.ToString(), branches.GetValueOrDefault(item.Id) ?? item.Id.ToString(), item.Count)));

        return new UserDashboardOrganizations(tenantItems, companyItems, branchItems, departmentItems, positionItems);
    }

    private async Task<IReadOnlyList<UserDashboardActivity>> GetRecentActivitiesAsync(
        IQueryable<Users> users,
        UserDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        var created = users.Select(item => new
        {
            item.Id,
            item.FullName,
            Activity = "User created",
            ActorId = item.CreatedBy,
            At = item.CreatedAt,
            Organization = item.Company != null ? item.Company.Name : item.Tenant.Name,
            Status = item.IsActive ? "Active" : "Inactive"
        });
        if (filter.FromDate.HasValue)
        created = created.Where(item => item.At >= filter.FromDate.Value.UtcDateTime);
        if (filter.ToDate.HasValue)
        created = created.Where(item => item.At < filter.ToDate.Value.UtcDateTime);
        var updated = users.Where(item => item.UpdatedAt.HasValue)
            .Select(item => new
            {
                item.Id,
                item.FullName,
                Activity = "User updated",
                ActorId = item.UpdatedBy ?? Guid.Empty,
                At = item.UpdatedAt!.Value,
                Organization = item.Company != null ? item.Company.Name : item.Tenant.Name,
                Status = item.IsActive ? "Active" : "Inactive"
            });
        if (filter.FromDate.HasValue)
            updated = updated.Where(item => item.At >= filter.FromDate.Value.UtcDateTime);
        if (filter.ToDate.HasValue)
            updated = updated.Where(item => item.At < filter.ToDate.Value.UtcDateTime);
        var events = await created.Concat(updated).OrderByDescending(item => item.At)
            .Take(20).ToListAsync(cancellationToken);
        var actorIds = events.Select(item => item.ActorId).Where(id => id != Guid.Empty).Distinct().ToList();
        var actorNames = await users
            .Where(item => actorIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.FullName, cancellationToken);

        return events.Select(item => new UserDashboardActivity(
            item.Id, item.Activity, item.FullName,
            item.ActorId == user.UserId
                ? "You"
                : actorNames.GetValueOrDefault(item.ActorId)
                    ?? (item.ActorId == Guid.Empty ? "System" : "Administrator"),
            item.Organization,
            new DateTimeOffset(DateTime.SpecifyKind(item.At, DateTimeKind.Utc)),
            item.Status)).ToList();
    }
}
