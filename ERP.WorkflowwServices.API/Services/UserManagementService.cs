using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;

namespace ERP.WorkflowwServices.API.Services
{
    public sealed class UserManagementService : IUserManagementService
    {
        private readonly WorkflowDbContext _context;
        private readonly IUserContext _userContext;
        private readonly IUserRoleAssignmentService _roleAssignments;
        private readonly IUserPositionAssignmentService _positionAssignments;
        private readonly IUserInvitationService _invitations;

        public UserManagementService(
            WorkflowDbContext context,
            IUserContext userContext,
            IUserRoleAssignmentService roleAssignments,
            IUserPositionAssignmentService positionAssignments,
            IUserInvitationService invitations)
        {
            _context = context;
            _userContext = userContext;
            _roleAssignments = roleAssignments;
            _positionAssignments = positionAssignments;
            _invitations = invitations;
        }

        public async Task<PagedResult<UserListDto>> GetAllAsync(FilterModel filter)
        {
            var companyId = await ResolveCompanyScopeAsync(filter.UserCompanyId, "View");
            var accessibleCompanyIds = _userContext.IsCompanyAdmin
                ? filter.UserCompanyId.HasValue
                    ? companyId.HasValue ? [companyId.Value] : []
                    : await GetAccessibleCompanyIdsAsync()
                : null;
            var query = _context.Users.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                query = query.IgnoreQueryFilters()
                    .Where(user => !user.IsDeleted && user.Id != _userContext.UserId);
            else
                query = query.Where(user => user.TenantId == _userContext.TenantId);

            query = query
                .Include(user => user.Role)
                .Include(user => user.Position)
                .Include(user => user.PositionAssignments.Where(assignment =>
                    assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                    && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow)))
                    .ThenInclude(assignment => assignment.Position)
                .Include(user => user.Company)
                .Include(user => user.CompanyAccesses.Where(access =>
                    access.IsActive && access.EffectiveFrom <= DateTime.UtcNow
                    && (!access.EffectiveTo.HasValue || access.EffectiveTo.Value >= DateTime.UtcNow)))
                    .ThenInclude(access => access.Company)
                .Include(user => user.Branch)
                .Include(user => user.Tenant)
                .Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role);

            if (accessibleCompanyIds is not null)
                query = query.Where(user => user.CompanyId.HasValue && accessibleCompanyIds.Contains(user.CompanyId.Value));
            else if (companyId.HasValue)
                query = query.Where(user => user.CompanyId == companyId.Value);

            if (!string.IsNullOrWhiteSpace(filter.KeyWord))
            {
                var keyword = filter.KeyWord.Trim();
                query = query.Where(user => user.Username.Contains(keyword)
                    || user.FullName.Contains(keyword)
                    || (user.Email != null && user.Email.Contains(keyword))
                    || (user.MobileNo != null && user.MobileNo.Contains(keyword)));
            }

            if (filter.Status is <= 1)
                query = query.Where(user => user.IsActive == (filter.Status == 0));

            var total = await query.CountAsync();
            var pageNumber = Math.Max(filter.PageNumber, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);
            var users = await query
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var departmentIds = users.Where(user => user.DepartmentId.HasValue)
                .Select(user => user.DepartmentId!.Value)
                .Distinct()
                .ToList();
            var departments = _context.Departments.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                departments = departments.IgnoreQueryFilters();
            var departmentNames = await departments
                .Where(department => departmentIds.Contains(department.Id) && !department.IsDeleted)
                .ToDictionaryAsync(department => department.Id, department => department.Name);

            return new PagedResult<UserListDto>
            {
                Total = total,
                Data = users.Select(user => Map(
                    user,
                    user.DepartmentId is Guid departmentId
                        ? departmentNames.GetValueOrDefault(departmentId)
                        : null)).ToList()
            };
        }

        public async Task<UserListDto?> GetByIdAsync(Guid id)
        {
            var users = _context.Users.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                users = users.IgnoreQueryFilters().Where(item => !item.IsDeleted);
            var user = await users
                .Include(item => item.Role)
                .Include(item => item.Position)
                .Include(item => item.PositionAssignments.Where(assignment =>
                    assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                    && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow)))
                    .ThenInclude(assignment => assignment.Position)
                .Include(item => item.Company)
                .Include(item => item.CompanyAccesses.Where(access =>
                    access.IsActive && access.EffectiveFrom <= DateTime.UtcNow
                    && (!access.EffectiveTo.HasValue || access.EffectiveTo.Value >= DateTime.UtcNow)))
                    .ThenInclude(access => access.Company)
                .Include(item => item.Branch)
                .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(item => item.Id == id
                    && (_userContext.IsSuperAdmin || item.TenantId == _userContext.TenantId));

            if (user == null)
                return null;

            await EnsureUserScopeAsync(user, "View");
            return Map(user, await GetDepartmentNameAsync(user.DepartmentId));
        }

        public async Task<List<UserRoleOptionDto>> GetRolesAsync()
        {
            EnsureAuthorized("View");
            var roles = await _context.Roles.AsNoTracking()
                .Where(role => role.TenantId == _userContext.TenantId && role.IsActive && !role.IsDeleted)
                .Include(role => role.RolePermissions).ThenInclude(mapping => mapping.Permission)
                .Include(role => role.MenuRoles).ThenInclude(mapping => mapping.Menu)
                .OrderBy(role => role.Priority)
                .ThenBy(role => role.Name)
                .ToListAsync();

            return roles.Where(IsRoleAssignable)
                .Select(role => new UserRoleOptionDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Code = role.Code
                })
                .ToList();
        }

        public async Task<List<UserCompanyOptionDto>> GetCompaniesAsync()
        {
            EnsureAuthorized("View");
            if (_userContext.IsSuperAdmin)
                return await _context.Companies.IgnoreQueryFilters().AsNoTracking()
                    .Where(company => company.IsActive && !company.IsDeleted)
                    .OrderBy(company => company.Name)
                    .Select(company => new UserCompanyOptionDto { Id = company.Id, Name = company.Name })
                    .ToListAsync();

            if (_userContext.IsCompanyAdmin)
            {
                var companyIds = await GetAccessibleCompanyIdsAsync();
                return await _context.Companies.AsNoTracking()
                    .Where(company => companyIds.Contains(company.Id) && company.TenantId == _userContext.TenantId && company.IsActive)
                    .OrderBy(company => company.Name)
                    .Select(company => new UserCompanyOptionDto { Id = company.Id, Name = company.Name })
                    .ToListAsync();
            }

            return await _context.Companies.AsNoTracking()
                .Where(company => company.IsActive && company.TenantId == _userContext.TenantId)
                .OrderBy(company => company.Name)
                .Select(company => new UserCompanyOptionDto { Id = company.Id, Name = company.Name })
                .ToListAsync();
        }

        public async Task<List<DepartmentDropdownDto>> GetDepartmentsAsync(Guid? requestedCompanyId)
        {
            var companyId = await ResolveCompanyScopeAsync(requestedCompanyId, "View");
            if (!companyId.HasValue)
                return [];

            var departments = _context.Departments.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                departments = departments.IgnoreQueryFilters();
            return await departments
                .Where(department => department.CompanyId == companyId && department.IsActive && !department.IsDeleted)
                .OrderBy(department => department.Name)
                .Select(department => new DepartmentDropdownDto(
                    department.Id, department.Name, department.Code, department.ParentDepartmentId, department.CompanyId))
                .ToListAsync();
        }

        public async Task<List<PositionDropdownDto>> GetPositionsAsync(Guid? requestedCompanyId)
        {
            var companyId = await ResolveCompanyScopeAsync(requestedCompanyId, "View");
            if (!companyId.HasValue)
                return [];

            var positions = _context.Positions.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                positions = positions.IgnoreQueryFilters();
            return await positions
                .Where(position => position.CompanyId == companyId && position.IsActive && !position.IsDeleted)
                .OrderBy(position => position.PositionName)
                .Select(position => new PositionDropdownDto(
                    position.Id, position.PositionName, position.PositionCode, position.DepartmentId,
                    position.ParentPositionId, position.PositionLevel, position.CompanyId))
                .ToListAsync();
        }

        public async Task<List<UserBranchOptionDto>> GetBranchesAsync(Guid? requestedCompanyId)
        {
            var companyId = await ResolveCompanyScopeAsync(requestedCompanyId, "View");
            if (!companyId.HasValue)
                return [];

            var branches = _context.Branches.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                branches = branches.IgnoreQueryFilters();
            return await branches
                .Where(branch => branch.CompanyId == companyId && branch.IsActive && !branch.IsDeleted)
                .OrderBy(branch => branch.Name)
                .Select(branch => new UserBranchOptionDto(branch.Id, branch.Name, branch.Code, branch.CompanyId))
                .ToListAsync();
        }

        public async Task<List<UserTypeOptionDto>> GetUserTypesAsync(Guid? requestedCompanyId)
        {
            var companyId = await ResolveCompanyScopeAsync(requestedCompanyId, "View");
            if (!companyId.HasValue)
                return [];

            var settingsQuery = _context.CompanyUserSettings.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                settingsQuery = settingsQuery.IgnoreQueryFilters();
            var settings = await settingsQuery
                .FirstOrDefaultAsync(item => item.CompanyId == companyId);
            var result = new List<UserTypeOptionDto>
            {
                new("Internal", "Internal User")
            };
            if (settings?.AllowExternalUsers != false)
            {
                result.Add(new("External", "External User"));
                result.Add(new("Consultant", "Consultant"));
                result.Add(new("Freelancer", "Freelancer"));
            }
            if (settings?.AllowVendorUsers != false)
                result.Add(new("Vendor", "Vendor User"));
            if (settings?.AllowCustomerUsers != false)
                result.Add(new("Customer", "Customer User"));
            return result;
        }

        public async Task<UserListDto> CreateAsync(CreateUserRequest request)
        {
            var companyId = await ResolveCompanyScopeAsync(request.CompanyId, "Create", required: true);
            var companyIds = (request.CompanyIds ?? [companyId!.Value])
                .Append(companyId!.Value)
                .Distinct()
                .ToList();
            foreach (var requestedCompanyId in companyIds)
                await ResolveCompanyScopeAsync(requestedCompanyId, "Create", required: true);
            if (request.SendInvitation)
                EnsureAuthorized("Invitation");

            var settings = await _context.CompanyUserSettings.AsNoTracking()
                .FirstOrDefaultAsync(item => item.CompanyId == companyId);
            if (settings?.AllowUserRegistration == false)
                throw new InvalidOperationException("User registration is disabled for this company.");
            if (settings?.RequireEmailVerification == true && !request.SendInvitation)
                throw new InvalidOperationException("This company requires email invitations for new accounts.");
            EnsureUserTypeAllowed(request.UserType, settings);
            if (settings?.MaxActiveUsers is int maxUsers
                && request.IsActive
                && await _context.Users.CountAsync(item => item.CompanyId == companyId && item.IsActive) >= maxUsers)
                throw new InvalidOperationException("The company has reached its active user limit.");

            if (request.BranchId.HasValue
                && !await _context.Branches.AnyAsync(branch => branch.Id == request.BranchId
                    && branch.CompanyId == companyId && branch.IsActive && !branch.IsDeleted))
                throw new InvalidOperationException("Select an active branch in the selected company.");

            var username = request.Username.Trim();
            var email = request.Email.Trim();

            if (await _context.Users.AnyAsync(user => user.TenantId == _userContext.TenantId
                && user.Username == username))
                throw new InvalidOperationException("Username is already in use in this tenant.");

            if (await _context.Users.AnyAsync(user => user.TenantId == _userContext.TenantId
                && user.Email == email))
                throw new InvalidOperationException("Email is already in use in this tenant.");

            var role = await GetAssignableRoleAsync(request.RoleId);
            if (role == null)
                throw new InvalidOperationException("Select a role you are authorized to assign.");

            var profile = await ResolveProfileAsync(
                request.EmployeeCode, request.DepartmentId, request.PositionId, null, companyId);

            var user = new Users
            {
                Id = Guid.NewGuid(),
                TenantId = _userContext.TenantId,
                CompanyId = companyId,
                FullName = request.FullName.Trim(),
                UserType = NormalizeUserType(request.UserType),
                Username = username,
                Email = email,
                MobileNo = NormalizeOptional(request.MobileNo),
                BranchId = request.BranchId,
                EmployeeCode = profile.EmployeeCode,
                DepartmentId = profile.DepartmentId,
                PositionId = null,
                Position = null,
                RoleId = role.Id,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.SendInvitation
                    ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                    : request.Password),
                IsActive = request.SendInvitation ? false : request.IsActive,
                MustChangePassword = true
            };

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            _context.Users.Add(user);
            var assignmentTime = DateTime.UtcNow;
            foreach (var accessCompanyId in companyIds)
            {
                _context.UserCompanyAccesses.Add(new UserCompanyAccess
                {
                    Id = Guid.NewGuid(),
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    CompanyId = accessCompanyId,
                    EffectiveFrom = assignmentTime,
                    IsActive = true,
                    IsDefault = accessCompanyId == companyId,
                    CanManageUsers = false
                });
            }
            if (request.BranchId.HasValue)
            {
                _context.UserBranchAccesses.Add(new UserBranchAccess
                {
                    Id = Guid.NewGuid(),
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    BranchId = request.BranchId.Value,
                    EffectiveFrom = assignmentTime,
                    IsActive = true,
                    IsDefault = true
                });
            }
            await _context.SaveChangesAsync();
            await _roleAssignments.SyncLegacyRolesAsync(user.Id, [role.Id], allowInactiveUser: true);
            if (profile.PositionId.HasValue)
            {
                await _positionAssignments.AssignAsync(user.Id, new SaveUserPositionAssignmentRequest(
                    profile.PositionId.Value,
                    DateTimeOffset.UtcNow,
                    null,
                    "Initial position assignment"));
            }
            await transaction.CommitAsync();
            if (request.SendInvitation)
                await _invitations.SendInvitationAsync(user.Id);

            return await GetByIdAsync(user.Id)
                ?? throw new InvalidOperationException("The user was created but could not be loaded.");
        }

        public async Task<UserListDto?> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
                .Include(item => item.PositionAssignments.Where(assignment =>
                    assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                    && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow)))
                    .ThenInclude(assignment => assignment.Position)
                .Include(item => item.Company)
                .Include(item => item.CompanyAccesses.Where(assignment =>
                    assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                    && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow)))
                    .ThenInclude(assignment => assignment.Company)
                .Include(item => item.Branch)
                .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == _userContext.TenantId);
            if (user == null)
                return null;

            await EnsureUserScopeAsync(user, "Update");
            if (request.CompanyId.HasValue && request.CompanyId != user.CompanyId)
                throw new InvalidOperationException("A user's company cannot be changed through this form.");
            List<Guid>? requestedCompanyIds = null;
            if (request.CompanyIds is not null)
            {
                if (!user.CompanyId.HasValue)
                    throw new InvalidOperationException("A default company is required for company access.");
                requestedCompanyIds = request.CompanyIds.Append(user.CompanyId.Value).Distinct().ToList();
                foreach (var requestedCompanyId in requestedCompanyIds)
                    await ResolveCompanyScopeAsync(requestedCompanyId, "Update", required: true);
            }
            if (request.BranchId.HasValue
                && !await _context.Branches.AnyAsync(branch => branch.Id == request.BranchId
                    && branch.CompanyId == user.CompanyId && branch.IsActive && !branch.IsDeleted))
                throw new InvalidOperationException("Select an active branch in the user's company.");
            var settings = user.CompanyId.HasValue
                ? await _context.CompanyUserSettings.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.CompanyId == user.CompanyId)
                : null;
            EnsureUserTypeAllowed(request.UserType, settings);

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            if (await _context.Users.AnyAsync(item => item.TenantId == _userContext.TenantId
                && item.Id != id && item.Username == username))
                throw new InvalidOperationException("Username is already in use in this tenant.");

            if (await _context.Users.AnyAsync(item => item.TenantId == _userContext.TenantId
                && item.Id != id && item.Email == email))
                throw new InvalidOperationException("Email is already in use in this tenant.");

            var role = await GetAssignableRoleAsync(request.RoleId);
            if (role == null)
                throw new InvalidOperationException("Select a role you are authorized to assign.");

            var profile = await ResolveProfileAsync(
                request.EmployeeCode, request.DepartmentId, request.PositionId, id, user.CompanyId);

            var previousBranchId = user.BranchId;
            user.FullName = request.FullName.Trim();
            user.Username = username;
            user.Email = email;
            user.MobileNo = NormalizeOptional(request.MobileNo);
            user.UserType = NormalizeUserType(request.UserType);
            user.BranchId = request.BranchId;
            user.EmployeeCode = profile.EmployeeCode;
            user.DepartmentId = profile.DepartmentId;
            user.PositionId = null;
            user.Position = null;
            user.RoleId = role.Id;
            user.Role = role;

            var assignmentTime = DateTime.UtcNow;
            if (requestedCompanyIds is not null)
            {
                var existingCompanyAccesses = await _context.UserCompanyAccesses
                    .Where(assignment => assignment.TenantId == user.TenantId
                        && assignment.UserId == user.Id && assignment.IsActive)
                    .ToListAsync();
                foreach (var assignment in existingCompanyAccesses)
                {
                    if (!requestedCompanyIds.Contains(assignment.CompanyId))
                    {
                        assignment.IsActive = false;
                        assignment.IsDefault = false;
                        assignment.EffectiveTo = assignmentTime;
                    }
                    else
                    {
                        assignment.IsDefault = assignment.CompanyId == user.CompanyId;
                    }
                }

                var existingCompanyIds = existingCompanyAccesses
                    .Where(assignment => assignment.IsActive)
                    .Select(assignment => assignment.CompanyId)
                    .ToHashSet();
                foreach (var accessCompanyId in requestedCompanyIds.Where(id => !existingCompanyIds.Contains(id)))
                {
                    _context.UserCompanyAccesses.Add(new UserCompanyAccess
                    {
                        Id = Guid.NewGuid(),
                        TenantId = user.TenantId,
                        UserId = user.Id,
                        CompanyId = accessCompanyId,
                        EffectiveFrom = assignmentTime,
                        IsActive = true,
                        IsDefault = accessCompanyId == user.CompanyId,
                        CanManageUsers = false
                    });
                }
            }
            var activeBranchAssignments = await _context.UserBranchAccesses
                .Where(assignment => assignment.TenantId == user.TenantId
                    && assignment.UserId == user.Id && assignment.IsActive)
                .ToListAsync();
            if (previousBranchId != request.BranchId)
            {
                foreach (var assignment in activeBranchAssignments)
                {
                    assignment.IsActive = false;
                    assignment.IsDefault = false;
                    assignment.EffectiveTo = assignmentTime;
                }
                if (request.BranchId.HasValue)
                {
                    _context.UserBranchAccesses.Add(new UserBranchAccess
                    {
                        Id = Guid.NewGuid(),
                        TenantId = user.TenantId,
                        UserId = user.Id,
                        BranchId = request.BranchId.Value,
                        EffectiveFrom = assignmentTime,
                        IsActive = true,
                        IsDefault = true
                    });
                }
            }
            await _context.SaveChangesAsync();

            var roleIds = user.UserRoles.Select(userRole => userRole.RoleId).Append(role.Id).Distinct().ToList();
            await _roleAssignments.SyncLegacyRolesAsync(user.Id, roleIds);
            if (profile.PositionId.HasValue)
            {
                var existingPositionId = user.PositionAssignments
                    .Where(assignment => assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                        && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow))
                    .Select(assignment => assignment.PositionId)
                    .FirstOrDefault();
                if (existingPositionId != profile.PositionId.Value)
                {
                    await _positionAssignments.AssignAsync(user.Id, new SaveUserPositionAssignmentRequest(
                        profile.PositionId.Value,
                        DateTimeOffset.UtcNow,
                        null,
                        "Updated from user management"));
                }
            }
            return await GetByIdAsync(id);
        }

        public async Task<UserListDto?> SetRolesAsync(Guid id, SetUserRolesRequest request)
        {
            var user = await _context.Users.Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == _userContext.TenantId);
            if (user == null)
                return null;

            await EnsureUserScopeAsync(user, "Update");
            foreach (var roleId in request.RoleIds.Distinct())
                if (await GetAssignableRoleAsync(roleId) == null)
                    throw new InvalidOperationException("One or more selected roles are unavailable.");

            await _roleAssignments.SyncLegacyRolesAsync(id, request.RoleIds);
            return await GetByIdAsync(id);
        }

        public async Task<UserListDto?> SetStatusAsync(Guid id, bool isActive)
        {
            var user = await _context.Users.Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == _userContext.TenantId);
            if (user == null)
                return null;

            await EnsureUserScopeAsync(user, isActive ? "Activate" : "Deactivate");
            if (user.IsSystem && !isActive)
                throw new InvalidOperationException("System users cannot be deactivated.");

            user.IsActive = isActive;
            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task ResendInvitationAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == _userContext.TenantId, cancellationToken);
            if (user == null)
                throw new KeyNotFoundException("User was not found.");

            await EnsureUserScopeAsync(user, "Invitation");
            await _invitations.SendInvitationAsync(id, cancellationToken);
        }

        private async Task<Roles?> GetAssignableRoleAsync(Guid roleId)
        {
            var role = await _context.Roles.AsNoTracking()
                .Where(item => item.Id == roleId && item.TenantId == _userContext.TenantId
                    && item.IsActive && !item.IsDeleted)
                .Include(item => item.RolePermissions).ThenInclude(mapping => mapping.Permission)
                .Include(item => item.MenuRoles).ThenInclude(mapping => mapping.Menu)
                .FirstOrDefaultAsync();
            return role != null && IsRoleAssignable(role) ? role : null;
        }

        private bool IsRoleAssignable(Roles role)
        {
            if (role.IsSystem || string.Equals(role.Code, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
                return false;
            if (_userContext.IsSuperAdmin)
                return true;
            if (string.Equals(role.ScopeLevel, "Platform", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(role.Code, "COMPANY_ADMIN", StringComparison.OrdinalIgnoreCase))
                return false;

            var actorPermissions = _userContext.PermissionKeys;
            var assignedPermissions = role.RolePermissions
                .Where(mapping => mapping.IsGranted && mapping.Permission is { IsActive: true })
                .Select(mapping => mapping.Permission.PermissionKey)
                .Concat(role.MenuRoles
                    .Where(mapping => mapping.IsActive && mapping.CanView && mapping.Menu is { IsActive: true })
                    .Select(mapping => mapping.Menu!.Permission)
                    .Where(permission => !string.IsNullOrWhiteSpace(permission)))
                .Select(permission => permission!)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return assignedPermissions.All(permission => actorPermissions.Contains(permission));
        }

        private async Task<(Guid? DepartmentId, string? EmployeeCode, long? PositionId, Position? Position)>
            ResolveProfileAsync(
                string? employeeCode, Guid? departmentId, long? positionId, Guid? excludedUserId, Guid? companyId)
        {
            var normalizedEmployeeCode = NormalizeOptional(employeeCode);
            if (normalizedEmployeeCode?.Length > 50)
                throw new InvalidOperationException("Employee Code cannot exceed 50 characters.");

            if (normalizedEmployeeCode is not null)
            {
                var matchingUsers = _context.Users.Where(user =>
                    user.TenantId == _userContext.TenantId && user.EmployeeCode == normalizedEmployeeCode);
                if (excludedUserId.HasValue)
                    matchingUsers = matchingUsers.Where(user => user.Id != excludedUserId.Value);
                if (await matchingUsers.AnyAsync())
                    throw new InvalidOperationException("Employee Code is already in use in this tenant.");
            }

            if (departmentId.HasValue
                && !await _context.Departments.AnyAsync(department =>
                    department.Id == departmentId && department.CompanyId == companyId
                    && department.IsActive && !department.IsDeleted))
                throw new InvalidOperationException("Select an active department in the selected company.");

            Position? position = null;
            if (positionId.HasValue)
            {
                position = await _context.Positions.FirstOrDefaultAsync(item =>
                    item.Id == positionId && item.CompanyId == companyId && item.IsActive && !item.IsDeleted);
                if (position is null)
                    throw new InvalidOperationException("Select an active position in the selected company.");

                if (departmentId.HasValue && departmentId.Value != position.DepartmentId)
                    throw new InvalidOperationException("The selected position belongs to a different department.");

                departmentId = position.DepartmentId;
                if (!await _context.Departments.AnyAsync(department =>
                    department.Id == departmentId && department.CompanyId == companyId
                    && department.IsActive && !department.IsDeleted))
                    throw new InvalidOperationException("The selected position's department is inactive.");
            }

            return (departmentId, normalizedEmployeeCode, position?.Id, position);
        }

        private async Task<string?> GetDepartmentNameAsync(Guid? departmentId)
        {
            if (!departmentId.HasValue)
                return null;

            var departments = _context.Departments.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                departments = departments.IgnoreQueryFilters();
            return await departments
                .Where(department => department.Id == departmentId.Value)
                .Select(department => department.Name)
                .FirstOrDefaultAsync();
        }

        private async Task<Guid?> ResolveCompanyScopeAsync(Guid? requestedCompanyId, string action, bool required = false)
        {
            EnsureAuthorized(action);
            Guid? companyId;

            if (_userContext.IsSuperAdmin)
            {
                companyId = requestedCompanyId;
                if (required && !companyId.HasValue)
                    throw new InvalidOperationException("Select a company.");
            }
            else if (_userContext.IsTenantAdmin)
            {
                companyId = requestedCompanyId;
                if (required && !companyId.HasValue)
                    throw new InvalidOperationException("Select a company.");
            }
            else
            {
                var accessibleCompanyIds = await GetAccessibleCompanyIdsAsync();
                companyId = requestedCompanyId ?? _userContext.CompanyId;
                if (!companyId.HasValue && accessibleCompanyIds.Count == 1)
                    companyId = accessibleCompanyIds[0];
                if (required && !companyId.HasValue)
                    throw new InvalidOperationException("Select an authorized company.");
                if (companyId.HasValue && !accessibleCompanyIds.Contains(companyId.Value))
                    throw new UnauthorizedAccessException("You cannot access users from another company.");
            }

            if (!companyId.HasValue)
                return null;

            var companies = _context.Companies.AsNoTracking();
            if (_userContext.IsSuperAdmin)
                companies = companies.IgnoreQueryFilters();
            var companyIsAvailable = await companies.AnyAsync(company =>
                company.Id == companyId.Value
                && (_userContext.IsSuperAdmin || company.TenantId == _userContext.TenantId)
                && company.IsActive && !company.IsDeleted);
            if (!companyIsAvailable)
                throw new InvalidOperationException("The selected company is inactive or outside your tenant.");

            return companyId;
        }

        private async Task<List<Guid>> GetAccessibleCompanyIdsAsync()
        {
            var actorCompanyId = await _context.Users.AsNoTracking()
                .Where(user => user.Id == _userContext.UserId && user.TenantId == _userContext.TenantId)
                .Select(user => user.CompanyId)
                .FirstOrDefaultAsync();

            if (_userContext.CompanyId.HasValue && actorCompanyId != _userContext.CompanyId)
                throw new UnauthorizedAccessException("The signed-in company assignment is no longer valid.");

            var effectiveAt = DateTime.UtcNow;
            var companyIds = await _context.UserCompanyAccesses.AsNoTracking()
                .Where(access => access.TenantId == _userContext.TenantId
                    && access.UserId == _userContext.UserId
                    && access.IsActive
                    && access.EffectiveFrom <= effectiveAt
                    && (!access.EffectiveTo.HasValue || access.EffectiveTo.Value >= effectiveAt))
                .Select(access => access.CompanyId)
                .Distinct()
                .ToListAsync();

            if (actorCompanyId.HasValue)
                companyIds.Add(actorCompanyId.Value);

            return companyIds.Distinct().ToList();
        }

        private async Task EnsureUserScopeAsync(Users user, string action)
        {
            EnsureAuthorized(action);
            if (_userContext.IsSuperAdmin || _userContext.IsTenantAdmin)
            {
                if (user.CompanyId.HasValue)
                    await ResolveCompanyScopeAsync(user.CompanyId, action);
                return;
            }

            var companyId = await ResolveCompanyScopeAsync(user.CompanyId, action, required: true);
            if (user.CompanyId != companyId)
                throw new UnauthorizedAccessException("You cannot access users from another company.");
        }

        private void EnsureAuthorized(string action)
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedAccessException("Authentication is required.");

            if (_userContext.IsSuperAdmin)
                return;

            var authorized = _userContext.IsCompanyAdmin
                ? _userContext.HasPermission($"UserManagement.Company.{action}")
                : _userContext.IsTenantAdmin
                    && _userContext.HasPermission($"UserManagement.Tenant.{action}");
            if (!authorized)
                throw new UnauthorizedAccessException("You are not authorized to perform this user management action.");
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static UserListDto Map(Users user, string? departmentName)
        {
            var assignedRoles = user.UserRoles
                .Where(userRole => userRole.Role != null)
                .Select(userRole => userRole.Role!)
                .ToList();

            if (assignedRoles.Count == 0 && user.Role != null)
                assignedRoles.Add(user.Role);

            return new UserListDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                MobileNo = user.MobileNo,
                TenantId = user.TenantId,
                TenantName = user.Tenant?.Name,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name,
                RoleIds = assignedRoles.Select(role => role.Id).Distinct().ToList(),
                RoleNames = assignedRoles.Select(role => role.Name).Distinct().ToList(),
                DepartmentId = user.DepartmentId,
                DepartmentName = departmentName,
                EmployeeCode = user.EmployeeCode,
                PositionId = user.PositionAssignments
                    .Where(assignment => assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                        && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow))
                    .Select(assignment => (long?)assignment.PositionId)
                    .FirstOrDefault() ?? user.PositionId,
                PositionName = user.PositionAssignments
                    .Where(assignment => assignment.IsActive && assignment.EffectiveFrom <= DateTime.UtcNow
                        && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo.Value >= DateTime.UtcNow))
                    .Select(assignment => assignment.Position.PositionName)
                    .FirstOrDefault() ?? user.Position?.PositionName,
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.Name,
                CompanyAccessIds = user.CompanyAccesses
                    .Where(access => access.IsActive && access.Company != null)
                    .Select(access => access.CompanyId)
                    .Distinct()
                    .ToList(),
                CompanyAccessNames = user.CompanyAccesses
                    .Where(access => access.IsActive && access.Company != null)
                    .Select(access => access.Company!.Name)
                    .Distinct()
                    .ToList(),
                BranchId = user.BranchId,
                BranchName = user.Branch?.Name,
                UserType = user.UserType,
                InvitationPending = !user.IsEmailVerified && !user.IsActive,
                IsActive = user.IsActive,
                IsLocked = user.IsLocked,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        private static string NormalizeUserType(string? userType) =>
            string.IsNullOrWhiteSpace(userType) ? "Internal" : userType.Trim();

        private static void EnsureUserTypeAllowed(string? userType, CompanyUserSettings? settings)
        {
            var normalized = NormalizeUserType(userType);
            if (!new[] { "Internal", "External", "Vendor", "Customer", "Consultant", "Freelancer" }
                .Contains(normalized, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a supported user type.");

            var isAllowed = normalized switch
            {
                "Vendor" => settings?.AllowVendorUsers != false,
                "Customer" => settings?.AllowCustomerUsers != false,
                "External" or "Consultant" or "Freelancer" => settings?.AllowExternalUsers != false,
                _ => true
            };
            if (!isAllowed)
                throw new InvalidOperationException($"The company does not allow {normalized.ToLowerInvariant()} users.");
        }
    }
}
