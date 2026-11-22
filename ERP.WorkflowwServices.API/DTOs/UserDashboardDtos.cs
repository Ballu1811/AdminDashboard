namespace ERP.WorkflowwServices.API.DTOs;

/// <summary>Optional filters for an authorization-scoped user dashboard.</summary>
public sealed record UserDashboardFilter
{
    /// <summary>Inclusive start of the selected date range.</summary>
    public DateTimeOffset? FromDate { get; init; }
    /// <summary>Exclusive end of the selected date range.</summary>
    public DateTimeOffset? ToDate { get; init; }
    /// <summary>Tenant filter, available only to platform administrators.</summary>
    public Guid? TenantId { get; init; }
    /// <summary>Company filter within the caller's authorized scope.</summary>
    public Guid? CompanyId { get; init; }
    /// <summary>Branch filter within the caller's authorized scope.</summary>
    public Guid? BranchId { get; init; }
    /// <summary>User type filter.</summary>
    public string? UserType { get; init; }
    /// <summary>Account status filter.</summary>
    public string? Status { get; init; }
}

/// <summary>Summary counts for users in the selected scope.</summary>
public sealed record UserDashboardSummary(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    int PendingApprovals,
    int PendingInvitations,
    int LockedAccounts,
    int NewUsers,
    int NeverLoggedIn);

/// <summary>A labeled count for a dashboard breakdown.</summary>
public sealed record UserDashboardCount(string Name, int Count);

/// <summary>A point in the user registration trend.</summary>
public sealed record UserDashboardTrendPoint(DateTimeOffset Date, int Count);

/// <summary>A tenant, company, branch, department, or position count.</summary>
public sealed record UserDashboardOrganizationItem(
    string Id,
    string Name,
    int Count,
    bool IsChild = false);

/// <summary>Organization breakdowns within the caller's authorized scope.</summary>
public sealed record UserDashboardOrganizations(
    IReadOnlyList<UserDashboardOrganizationItem> Tenants,
    IReadOnlyList<UserDashboardOrganizationItem> Companies,
    IReadOnlyList<UserDashboardOrganizationItem> Branches,
    IReadOnlyList<UserDashboardOrganizationItem> Departments,
    IReadOnlyList<UserDashboardOrganizationItem> Positions);

/// <summary>Counts of user records requiring administrator attention.</summary>
public sealed record UserDashboardAttention(
    int PendingApprovals,
    int PendingInvitations,
    int ExpiredInvitations,
    int LockedAccounts,
    int UsersWithoutRole,
    int UsersWithoutCompanyAccess,
    int UsersWithoutPosition,
    int AccountsExpiringSoon);

/// <summary>Available account-security indicators.</summary>
public sealed record UserDashboardSecurity(
    int FailedLoginAttempts,
    int LockedAccounts,
    int NeverLoggedIn,
    int InactiveAccounts,
    int TwoFactorEnabled,
    int TotalUsers);

/// <summary>A recent user creation or update event.</summary>
public sealed record UserDashboardActivity(
    Guid UserId,
    string Activity,
    string User,
    string PerformedBy,
    string Organization,
    DateTimeOffset DateTime,
    string Status);

/// <summary>Aggregated, authorization-scoped user dashboard data.</summary>
public sealed record UserDashboardResponse(
    UserDashboardSummary Summary,
    IReadOnlyList<UserDashboardTrendPoint> RegistrationTrend,
    IReadOnlyList<UserDashboardCount> StatusDistribution,
    IReadOnlyList<UserDashboardCount> UserTypeDistribution,
    UserDashboardOrganizations OrganizationDistribution,
    UserDashboardAttention AttentionRequired,
    UserDashboardSecurity SecurityOverview,
    IReadOnlyList<UserDashboardActivity> RecentActivities);
