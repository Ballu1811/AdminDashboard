using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class UserPositionAssignmentService(WorkflowDbContext db, IUserContext user) : IUserPositionAssignmentService
{
    public async Task<IReadOnlyList<UserPositionAssignmentDto>> GetByUserAsync(Guid userId)
    {
        await EnsureUserScopeAsync(userId, "View");
        var records = await db.UserPositionAssignments.AsNoTracking()
            .Where(item => item.UserId == userId && item.TenantId == user.TenantId)
            .Include(item => item.Position).ThenInclude(position => position.Department)
            .OrderByDescending(item => item.EffectiveFrom)
            .ToListAsync();
        return records.Select(Map).ToList();
    }

    public async Task<UserPositionAssignmentDto> AssignAsync(
        Guid userId,
        SaveUserPositionAssignmentRequest request)
    {
        await EnsureUserScopeAsync(userId, "Update");
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
            throw new InvalidOperationException("The assignment end date must be on or after the start date.");

        var target = await db.Users.FirstOrDefaultAsync(item =>
            item.Id == userId && item.TenantId == user.TenantId);
        if (target == null)
            throw new KeyNotFoundException("User was not found in your authorized company.");

        var position = await db.Positions.Include(item => item.Department)
            .FirstOrDefaultAsync(item => item.Id == request.PositionId
                && item.TenantId == user.TenantId
                && item.CompanyId == target.CompanyId
                && item.IsActive && !item.IsDeleted);
        if (position == null)
            throw new InvalidOperationException("Select an active position in the user's company.");

        if (target.DepartmentId.HasValue && target.DepartmentId != position.DepartmentId)
            throw new InvalidOperationException("The selected position belongs to a different department.");

        var start = request.EffectiveFrom.UtcDateTime;
        var end = request.EffectiveTo?.UtcDateTime;
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync()
            : null;

        var overlapping = await db.UserPositionAssignments
            .Where(item => item.TenantId == user.TenantId && item.UserId == userId
                && item.IsActive
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= start)
                && (!end.HasValue || item.EffectiveFrom <= end.Value))
            .ToListAsync();

        foreach (var existing in overlapping)
        {
            existing.IsActive = false;
            if (existing.EffectiveFrom < start)
                existing.EffectiveTo = start.AddTicks(-1);
        }

        var assignment = new UserPositionAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = userId,
            PositionId = position.Id,
            Position = position,
            EffectiveFrom = start,
            EffectiveTo = end,
            IsActive = true,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()
        };
        db.UserPositionAssignments.Add(assignment);
        await db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        return Map(assignment);
    }

    private async Task EnsureUserScopeAsync(Guid userId, string action)
    {
        if (!user.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required.");
        var authorized = user.IsSuperAdmin
            || user.IsCompanyAdmin && user.HasPermission($"UserManagement.Company.{action}")
            || user.IsTenantAdmin && user.HasPermission($"UserManagement.Tenant.{action}");
        if (!authorized)
            throw new UnauthorizedAccessException("You are not authorized to manage user position assignments.");

        var targetCompany = await db.Users.AsNoTracking()
            .Where(item => item.Id == userId && item.TenantId == user.TenantId)
            .Select(item => item.CompanyId)
            .FirstOrDefaultAsync();
        if (targetCompany == null)
            throw new KeyNotFoundException("User was not found in your tenant.");

        if (user.IsCompanyAdmin)
        {
            var actorCompany = await db.Users.AsNoTracking()
                .Where(item => item.Id == user.UserId && item.TenantId == user.TenantId)
                .Select(item => item.CompanyId)
                .FirstOrDefaultAsync();
            if (user.CompanyId.HasValue && actorCompany != user.CompanyId)
                throw new UnauthorizedAccessException("You cannot access users from another company.");
            var accessibleCompanyIds = await db.UserCompanyAccesses.AsNoTracking()
                .Where(access => access.TenantId == user.TenantId
                    && access.UserId == user.UserId
                    && access.IsActive
                    && access.EffectiveFrom <= DateTime.UtcNow
                    && (!access.EffectiveTo.HasValue || access.EffectiveTo.Value >= DateTime.UtcNow))
                .Select(access => access.CompanyId)
                .ToListAsync();
            if (actorCompany.HasValue)
                accessibleCompanyIds.Add(actorCompany.Value);
            if (!targetCompany.HasValue || !accessibleCompanyIds.Contains(targetCompany.Value))
                throw new UnauthorizedAccessException("You cannot access users from another company.");
        }
    }

    private static UserPositionAssignmentDto Map(UserPositionAssignment item) =>
        new(
            item.Id,
            item.UserId,
            item.PositionId,
            item.Position.PositionName,
            item.Position.DepartmentId,
            item.Position.Department.Name,
            new DateTimeOffset(DateTime.SpecifyKind(item.EffectiveFrom, DateTimeKind.Utc)),
            item.EffectiveTo.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(item.EffectiveTo.Value, DateTimeKind.Utc))
                : null,
            item.IsActive,
            item.Remarks);
}
