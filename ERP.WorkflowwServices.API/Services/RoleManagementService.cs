using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public sealed class RoleManagementService : IRoleManagementService
    {
        private readonly WorkflowDbContext _context;

        public RoleManagementService(WorkflowDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleListDto>> GetAllAsync()
        {
            var roles = await _context.Roles.AsNoTracking()
                .OrderBy(role => role.Priority)
                .ThenBy(role => role.Name)
                .ToListAsync();

            return roles.Select(Map).ToList();
        }

        public async Task<RoleListDto> CreateAsync(SaveRoleRequest request)
        {
            var name = request.Name.Trim();
            var code = NormalizeCode(request.Code);
            await EnsureUniqueAsync(name, code, null);

            var role = new Roles
            {
                Id = Guid.NewGuid(),
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                Code = code,
                Description = NormalizeOptional(request.Description),
                Priority = request.Priority,
                IsActive = request.IsActive
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return Map(role);
        }

        public async Task<RoleListDto?> UpdateAsync(Guid id, SaveRoleRequest request)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(item => item.Id == id);
            if (role == null) return null;
            if (role.IsSystem || !role.IsEditable)
                throw new InvalidOperationException("This system role cannot be edited.");
            if (role.IsActive && !request.IsActive && await _context.UserRoles.AnyAsync(userRole => userRole.RoleId == id))
                throw new InvalidOperationException("Remove this role from users before deactivating it.");

            var name = request.Name.Trim();
            var code = NormalizeCode(request.Code);
            await EnsureUniqueAsync(name, code, id);

            role.Name = name;
            role.NormalizedName = name.ToUpperInvariant();
            role.Code = code;
            role.Description = NormalizeOptional(request.Description);
            role.Priority = request.Priority;
            role.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return Map(role);
        }

        private async Task EnsureUniqueAsync(string name, string? code, Guid? excludedId)
        {
            if (await _context.Roles.AnyAsync(role => (!excludedId.HasValue || role.Id != excludedId.Value) && role.Name == name))
                throw new InvalidOperationException("A role with this name already exists.");

            if (code != null && await _context.Roles.AnyAsync(role => (!excludedId.HasValue || role.Id != excludedId.Value) && role.Code == code))
                throw new InvalidOperationException("A role with this code already exists.");
        }

        private static string? NormalizeCode(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static RoleListDto Map(Roles role)
        {
            return new RoleListDto
            {
                Id = role.Id,
                Name = role.Name,
                Code = role.Code,
                Description = role.Description,
                Priority = role.Priority,
                IsActive = role.IsActive,
                IsSystem = role.IsSystem,
                IsDefault = role.IsDefault,
                IsEditable = role.IsEditable
            };
        }
    }
}
