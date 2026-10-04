using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public sealed class CompanyManagementService : ICompanyManagementService
    {
        private readonly WorkflowDbContext _context;
        private readonly IUserContext _userContext;

        public CompanyManagementService(WorkflowDbContext context, IUserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<List<CompanyListDto>> GetAllAsync()
        {
            var companies = await _context.Companies.AsNoTracking()
                .Include(company => company.ModuleAccess).ThenInclude(access => access.Module)
                .OrderBy(company => company.Name)
                .ToListAsync();

            return companies.Select(Map).ToList();
        }

        public async Task<CompanyListDto?> GetByIdAsync(Guid id)
        {
            var company = await _context.Companies.AsNoTracking()
                .Include(item => item.ModuleAccess).ThenInclude(access => access.Module)
                .FirstOrDefaultAsync(item => item.Id == id);

            return company == null ? null : Map(company);
        }

        public async Task<List<CompanyModuleOptionDto>> GetModulesAsync()
        {
            return await _context.Modules.AsNoTracking()
                .Where(module => module.TenantId == _userContext.TenantId && module.IsActive && !module.IsDeleted)
                .OrderBy(module => module.Category)
                .ThenBy(module => module.Name)
                .Select(module => new CompanyModuleOptionDto
                {
                    Id = module.Id,
                    Name = module.Name,
                    Code = module.Code,
                    Category = module.Category
                })
                .ToListAsync();
        }

        public async Task<CompanyListDto> CreateAsync(SaveCompanyRequest request)
        {
            var code = NormalizeCode(request.Code);
            await EnsureUniqueCodeAsync(code, null);
            var modules = await GetSelectedModulesAsync(request.ModuleIds);

            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Code = code,
                LegalName = NormalizeOptional(request.LegalName),
                Email = NormalizeOptional(request.Email),
                Phone = NormalizeOptional(request.Phone),
                Website = NormalizeOptional(request.Website),
                TaxNumber = NormalizeOptional(request.TaxNumber),
                Address = NormalizeOptional(request.Address),
                City = NormalizeOptional(request.City),
                State = NormalizeOptional(request.State),
                Country = NormalizeOptional(request.Country),
                AccessLevel = NormalizeAccessLevel(request.AccessLevel),
                IsActive = request.IsActive,
                ModuleAccess = modules.Select(module => new CompanyModuleAccess
                {
                    ModuleId = module.Id,
                    IsEnabled = true
                }).ToList()
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return Map(company);
        }

        public async Task<CompanyListDto?> UpdateAsync(Guid id, SaveCompanyRequest request)
        {
            var company = await _context.Companies
                .Include(item => item.ModuleAccess)
                .FirstOrDefaultAsync(item => item.Id == id);
            if (company == null) return null;

            var code = NormalizeCode(request.Code);
            await EnsureUniqueCodeAsync(code, id);
            var modules = await GetSelectedModulesAsync(request.ModuleIds);
            var requestedModuleIds = modules.Select(module => module.Id).ToHashSet();
            var existingAccess = company.ModuleAccess.ToDictionary(access => access.ModuleId);

            foreach (var access in company.ModuleAccess)
                access.IsEnabled = requestedModuleIds.Contains(access.ModuleId);

            foreach (var module in modules.Where(module => !existingAccess.ContainsKey(module.Id)))
            {
                company.ModuleAccess.Add(new CompanyModuleAccess
                {
                    CompanyId = company.Id,
                    ModuleId = module.Id,
                    IsEnabled = true
                });
            }

            company.Name = request.Name.Trim();
            company.Code = code;
            company.LegalName = NormalizeOptional(request.LegalName);
            company.Email = NormalizeOptional(request.Email);
            company.Phone = NormalizeOptional(request.Phone);
            company.Website = NormalizeOptional(request.Website);
            company.TaxNumber = NormalizeOptional(request.TaxNumber);
            company.Address = NormalizeOptional(request.Address);
            company.City = NormalizeOptional(request.City);
            company.State = NormalizeOptional(request.State);
            company.Country = NormalizeOptional(request.Country);
            company.AccessLevel = NormalizeAccessLevel(request.AccessLevel);
            company.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return Map(company);
        }

        private async Task<List<Module>> GetSelectedModulesAsync(List<Guid> moduleIds)
        {
            var uniqueIds = moduleIds.Distinct().ToList();
            var modules = await _context.Modules
                .Where(module => uniqueIds.Contains(module.Id)
                    && module.TenantId == _userContext.TenantId
                    && module.IsActive
                    && !module.IsDeleted)
                .ToListAsync();

            if (modules.Count != uniqueIds.Count)
                throw new InvalidOperationException("One or more selected modules are inactive or unavailable.");

            return modules;
        }

        private async Task EnsureUniqueCodeAsync(string code, Guid? excludedId)
        {
            if (await _context.Companies.AnyAsync(company => company.Code == code
                && (!excludedId.HasValue || company.Id != excludedId.Value)))
                throw new InvalidOperationException("A company with this code already exists.");
        }

        private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

        private static string NormalizeAccessLevel(string value) => string.IsNullOrWhiteSpace(value)
            ? "Custom"
            : value.Trim();

        private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

        private static CompanyListDto Map(Company company)
        {
            var enabledAccess = company.ModuleAccess.Where(access => access.IsEnabled).ToList();
            return new CompanyListDto
            {
                Id = company.Id,
                Name = company.Name,
                Code = company.Code,
                LegalName = company.LegalName,
                Email = company.Email,
                Phone = company.Phone,
                Website = company.Website,
                TaxNumber = company.TaxNumber,
                Address = company.Address,
                City = company.City,
                State = company.State,
                Country = company.Country,
                AccessLevel = company.AccessLevel,
                IsActive = company.IsActive,
                ModuleIds = enabledAccess.Select(access => access.ModuleId).ToList(),
                ModuleNames = enabledAccess.Where(access => access.Module != null)
                    .Select(access => access.Module!.Name).ToList()
            };
        }
    }
}
