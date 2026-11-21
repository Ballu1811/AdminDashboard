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
                .Where(module => module.TenantId == _userContext.TenantId && module.IsActive &&
                    !module.IsDeleted && !module.IsSystemModule)
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
                LegalName = request.LegalName.Trim(),
                CompanyType = request.CompanyType.Trim(),
                Industry = NormalizeOptional(request.Industry),
                RegistrationNumber = NormalizeOptional(request.RegistrationNumber),
                PanNumber = NormalizeOptional(request.PanNumber)?.ToUpperInvariant(),
                Gstin = NormalizeOptional(request.Gstin)?.ToUpperInvariant(),
                Email = request.Email.Trim(),
                PrimaryContactName = request.PrimaryContactName.Trim(),
                Phone = request.Phone.Trim(),
                Website = NormalizeOptional(request.Website),
                TaxNumber = NormalizeOptional(request.TaxNumber),
                Address = request.Address.Trim(),
                AddressLine2 = NormalizeOptional(request.AddressLine2),
                City = request.City.Trim(),
                State = request.State.Trim(),
                PostalCode = request.PostalCode.Trim(),
                Country = request.Country.Trim(),
                BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant(),
                TimeZoneId = request.TimeZoneId.Trim(),
                FiscalYearStartMonth = request.FiscalYearStartMonth,
                FiscalYearStartDay = request.FiscalYearStartDay,
                Locale = request.Locale.Trim(),
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
            company.LegalName = request.LegalName.Trim();
            company.CompanyType = request.CompanyType.Trim();
            company.Industry = NormalizeOptional(request.Industry);
            company.RegistrationNumber = NormalizeOptional(request.RegistrationNumber);
            company.PanNumber = NormalizeOptional(request.PanNumber)?.ToUpperInvariant();
            company.Gstin = NormalizeOptional(request.Gstin)?.ToUpperInvariant();
            company.Email = request.Email.Trim();
            company.PrimaryContactName = request.PrimaryContactName.Trim();
            company.Phone = request.Phone.Trim();
            company.Website = NormalizeOptional(request.Website);
            company.TaxNumber = NormalizeOptional(request.TaxNumber);
            company.Address = request.Address.Trim();
            company.AddressLine2 = NormalizeOptional(request.AddressLine2);
            company.City = request.City.Trim();
            company.State = request.State.Trim();
            company.PostalCode = request.PostalCode.Trim();
            company.Country = request.Country.Trim();
            company.BaseCurrency = request.BaseCurrency.Trim().ToUpperInvariant();
            company.TimeZoneId = request.TimeZoneId.Trim();
            company.FiscalYearStartMonth = request.FiscalYearStartMonth;
            company.FiscalYearStartDay = request.FiscalYearStartDay;
            company.Locale = request.Locale.Trim();
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

            var requirements = await _context.ModuleDependencies.AsNoTracking()
                .Select(dependency => new { dependency.ModuleId, dependency.RequiredModuleId })
                .ToListAsync();
            var selected = uniqueIds.ToHashSet();
            var availableNames = modules.ToDictionary(module => module.Id, module => module.Name);
            var graph = requirements.GroupBy(item => item.ModuleId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.RequiredModuleId).ToArray());
            foreach (var selectedModuleId in uniqueIds)
            {
                var pending = new Stack<Guid>();
                var visited = new HashSet<Guid>();
                pending.Push(selectedModuleId);
                while (pending.TryPop(out var current))
                {
                    if (!visited.Add(current) || !graph.TryGetValue(current, out var requiredIds)) continue;
                    foreach (var requiredId in requiredIds)
                    {
                        if (!selected.Contains(requiredId))
                        {
                            var moduleName = availableNames.GetValueOrDefault(current, "Selected module");
                            var requiredName = await _context.Modules.AsNoTracking()
                                .Where(module => module.Id == requiredId)
                                .Select(module => module.Name)
                                .FirstOrDefaultAsync() ?? "another module";
                            throw new InvalidOperationException($"{moduleName} requires {requiredName}. Select the required module before saving.");
                        }
                        pending.Push(requiredId);
                    }
                }
            }

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
                CompanyType = company.CompanyType,
                Industry = company.Industry,
                RegistrationNumber = company.RegistrationNumber,
                PanNumber = company.PanNumber,
                Gstin = company.Gstin,
                Email = company.Email,
                PrimaryContactName = company.PrimaryContactName,
                Phone = company.Phone,
                Website = company.Website,
                TaxNumber = company.TaxNumber,
                Address = company.Address,
                AddressLine2 = company.AddressLine2,
                City = company.City,
                State = company.State,
                PostalCode = company.PostalCode,
                Country = company.Country,
                BaseCurrency = company.BaseCurrency,
                TimeZoneId = company.TimeZoneId,
                FiscalYearStartMonth = company.FiscalYearStartMonth,
                FiscalYearStartDay = company.FiscalYearStartDay,
                Locale = company.Locale,
                AccessLevel = company.AccessLevel,
                IsActive = company.IsActive,
                ModuleIds = enabledAccess.Select(access => access.ModuleId).ToList(),
                ModuleNames = enabledAccess.Where(access => access.Module != null)
                    .Select(access => access.Module!.Name).ToList()
            };
        }
    }
}
