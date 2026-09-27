using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public sealed class UserManagementService : IUserManagementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserManagementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<UserListDto>> GetAllAsync(FilterModel filter)
        {
            IQueryable<Users> query = _unitOfWork.Users.Query().AsNoTracking().Include(user => user.Role);

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
            var user = await _unitOfWork.Users.FirstOrDefaultTrackedAsync(item => item.Id == id);
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

            await _unitOfWork.SaveChangesAsync();

            user.Role = role;
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
            return new UserListDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                MobileNo = user.MobileNo,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name,
                DepartmentId = user.DepartmentId,
                IsActive = user.IsActive,
                IsLocked = user.IsLocked,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}