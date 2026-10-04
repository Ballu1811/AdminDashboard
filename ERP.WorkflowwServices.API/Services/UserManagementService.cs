using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public sealed class UserManagementService : IUserManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly WorkflowDbContext _context;

        public UserManagementService(IUnitOfWork unitOfWork, WorkflowDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<PagedResult<UserListDto>> GetAllAsync(FilterModel filter)
        {
            IQueryable<Users> query = _unitOfWork.Users.Query().AsNoTracking()
                .Include(user => user.Role)
                .Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role);

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

            return new PagedResult<UserListDto>
            {
                Total = total,
                Data = users.Select(Map).ToList()
            };
        }

        public async Task<UserListDto?> GetByIdAsync(Guid id)
        {
            var user = await _unitOfWork.Users.Query()
                .AsNoTracking()
                .Include(item => item.Role)
                .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(item => item.Id == id);

            return user == null ? null : Map(user);
        }

        public async Task<List<UserRoleOptionDto>> GetRolesAsync()
        {
            return await _unitOfWork.Repository<Roles, Guid>().Query()
                .AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.Priority)
                .ThenBy(role => role.Name)
                .Select(role => new UserRoleOptionDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Code = role.Code
                })
                .ToListAsync();
        }

        public async Task<UserListDto> CreateAsync(CreateUserRequest request)
        {
            var username = request.Username.Trim();
            var email = request.Email.Trim();
            var users = _unitOfWork.Users.Query();

            if (await users.AnyAsync(user => user.Username == username))
                throw new InvalidOperationException("Username is already in use.");

            if (await users.AnyAsync(user => user.Email == email))
                throw new InvalidOperationException("Email is already in use.");

            var role = await GetActiveRoleAsync(request.RoleId);
            if (role == null)
                throw new InvalidOperationException("Select an active role.");

            var user = new Users
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Username = username,
                Email = email,
                MobileNo = NormalizeOptional(request.MobileNo),
                RoleId = role.Id,
                UserRoles =
                {
                    new UserRole { RoleId = role.Id, TenantId = role.TenantId, Role = role }
                },
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = request.IsActive,
                MustChangePassword = true
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            user.Role = role;
            return Map(user);
        }

        public async Task<UserListDto?> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(item => item.Id == id);
            if (user == null) return null;

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            var users = _unitOfWork.Users.Query();

            if (await users.AnyAsync(item => item.Id != id && item.Username == username))
                throw new InvalidOperationException("Username is already in use.");

            if (await users.AnyAsync(item => item.Id != id && item.Email == email))
                throw new InvalidOperationException("Email is already in use.");

            var role = await GetActiveRoleAsync(request.RoleId);
            if (role == null)
                throw new InvalidOperationException("Select an active role.");

            user.FullName = request.FullName.Trim();
            user.Username = username;
            user.Email = email;
            user.MobileNo = NormalizeOptional(request.MobileNo);
            user.RoleId = role.Id;
            user.Role = role;

            if (!user.UserRoles.Any(userRole => userRole.RoleId == role.Id))
            {
                user.UserRoles.Add(new UserRole { RoleId = role.Id, TenantId = role.TenantId, Role = role });
            }

            await _unitOfWork.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserListDto?> SetRolesAsync(Guid id, SetUserRolesRequest request)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(item => item.UserRoles).ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(item => item.Id == id);
            if (user == null) return null;

            var requestedIds = request.RoleIds.Distinct().ToList();
            if (requestedIds.Count == 0)
                throw new InvalidOperationException("Select at least one role.");

            var roles = await _unitOfWork.Repository<Roles, Guid>().Query()
                .Where(role => requestedIds.Contains(role.Id) && role.IsActive)
                .OrderBy(role => role.Priority)
                .ThenBy(role => role.Name)
                .ToListAsync();

            if (roles.Count != requestedIds.Count)
                throw new InvalidOperationException("One or more selected roles are inactive or unavailable.");

            var requestedRoleIds = roles.Select(role => role.Id).ToHashSet();
            var removedAssignments = user.UserRoles
                .Where(userRole => !requestedRoleIds.Contains(userRole.RoleId))
                .ToList();
            _context.UserRoles.RemoveRange(removedAssignments);
            foreach (var assignment in removedAssignments)
                user.UserRoles.Remove(assignment);

            var existingRoleIds = user.UserRoles.Select(userRole => userRole.RoleId).ToHashSet();
            foreach (var role in roles.Where(role => !existingRoleIds.Contains(role.Id)))
            {
                var assignment = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    TenantId = user.TenantId,
                    User = user,
                    Role = role
                };
                user.UserRoles.Add(assignment);
                _context.UserRoles.Add(assignment);
            }

            var primaryRole = roles.FirstOrDefault(role => role.Id == user.RoleId) ?? roles[0];
            user.RoleId = primaryRole.Id;
            user.Role = primaryRole;

            await _unitOfWork.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserListDto?> SetStatusAsync(Guid id, bool isActive)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultTrackedAsync(item => item.Id == id);
            if (user == null) return null;
            if (user.IsSystem && !isActive)
                throw new InvalidOperationException("System users cannot be deactivated.");

            user.IsActive = isActive;
            await _unitOfWork.SaveChangesAsync();

            user.Role = await _unitOfWork.Repository<Roles, Guid>()
                .FirstOrDefaultAsync(role => role.Id == user.RoleId);
            return Map(user);
        }

        private Task<Roles?> GetActiveRoleAsync(Guid roleId)
        {
            return _unitOfWork.Repository<Roles, Guid>()
                .FirstOrDefaultAsync(role => role.Id == roleId && role.IsActive);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static UserListDto Map(Users user)
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
                RoleId = user.RoleId,
                RoleName = user.Role?.Name,
                RoleIds = assignedRoles.Select(role => role.Id).Distinct().ToList(),
                RoleNames = assignedRoles.Select(role => role.Name).Distinct().ToList(),
                DepartmentId = user.DepartmentId,
                IsActive = user.IsActive,
                IsLocked = user.IsLocked,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}