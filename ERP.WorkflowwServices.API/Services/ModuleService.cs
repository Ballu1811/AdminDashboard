using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class ModuleService : IModuleService
    {
        private readonly IUnitOfWork _uow;
        public ModuleService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ===============================
        // GET ALL
        // ===============================
        public async Task<PagedResult<ModuleDto>> GetAllAsync(FilterModel filter)
        {
            bool? checkstatus = null;

            if (filter.Status <= 1)
                checkstatus = filter.Status > 0;

            var query = _uow.Modules.Query().AsNoTracking().Where(x => (checkstatus == null || x.IsDeleted == checkstatus) && x.IsActive);

            if (filter.HaveFilter && !string.IsNullOrWhiteSpace(filter.KeyWord))
            {
                var keyword = filter.KeyWord.Trim();
                query = query.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Description) && x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (x.Code != null && x.Code.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            var total = await query.CountAsync();
            if (!filter.ExportExcel)
            {
                query = query.Skip((filter.PageNumber - 1) * filter.PageSize)
                 .Take(filter.PageSize);
            }

            // ✅ Projection (DTO)
            var data = await query
                .OrderBy(x => x.OrderNo)
                .Select(x => new ModuleDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Icon = x.Icon,
                    DefaultRoute = x.DefaultRoute,
                    OrderNo = x.OrderNo,

                    IsDefault = x.IsDefault,
                    IsLicensed = x.IsLicensed,

                    Category = x.Category,
                    Permission = x.Permission,

                    IsActive = x.IsActive,
                    IsVisible = x.IsVisible,

                    Description = x.Description
                }).ToListAsync();

            return new PagedResult<ModuleDto>
            {
                Total = total,
                Data = data
            };
        }

        // ===============================
        // GET BY ID
        // ===============================
        public async Task<ModuleDto?> GetByIdAsync(Guid id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        // ===============================
        // CREATE MODULE
        // ===============================
        public async Task<ModuleDto> CreateAsync(ModuleCreateDto dto)
        {
            // 🔥 Duplicate check
            var exists = await _uow.Modules.AnyAsync(x => (x.Name == dto.Name || x.Code == dto.Code) && !x.IsDeleted);

            if (exists)
                throw new Exception("Module already exists");

            var entity = new Module
            {
                Id = Guid.NewGuid(),

                // 🔹 Basic
                Name = dto.Name,
                Code = dto.Code,
                Icon = dto.Icon,
                DefaultRoute = dto.DefaultRoute,
                OrderNo = dto.OrderNo,

                // 🔹 Control
                IsLicensed = dto.IsLicensed,
                Permission = dto.Permission,
                Category = dto.Category,

                // 🔹 Visibility
                IsActive = dto.IsActive,
                IsVisible = dto.IsVisible
            };

            await _uow.Modules.AddAsync(entity);
            await _uow.SaveChangesAsync();

            return await GetByIdAsync(entity.Id) ?? throw new Exception("Creation failed");
        }

        // ===============================
        // UPDATE MODULE
        // ===============================
        public async Task UpdateAsync(ModuleUpdateDto dto)
        {
            var entity = await _uow.Modules.GetByIdAsync(dto.Id);

            if (entity == null)
                throw new Exception("Module not found");

            // 🔥 Duplicate check (exclude self)
            var isDuplicate = await _uow.Modules.AnyAsync(x =>
                (x.Name == dto.Name || x.Code == dto.Code) &&
                x.Id != dto.Id &&
                !x.IsDeleted);

            if (isDuplicate)
                throw new Exception("Module name/code already exists");

            // ✅ Update
            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.Icon = dto.Icon;
            entity.DefaultRoute = dto.DefaultRoute;
            entity.OrderNo = dto.OrderNo;

            entity.IsLicensed = dto.IsLicensed;
            entity.Permission = dto.Permission;
            entity.Category = dto.Category;

            entity.IsActive = dto.IsActive;
            entity.IsVisible = dto.IsVisible;

            _uow.Modules.Update(entity);
            await _uow.SaveChangesAsync();
        }

        // ===============================
        // DELETE (SOFT DELETE)
        // ===============================
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _uow.Modules.GetByIdAsync(id);

            if (entity == null)
                throw new Exception("Module not found");

            entity.IsDeleted = true;

            _uow.Modules.Update(entity);
            await _uow.SaveChangesAsync();
        }

        // ===============================
        // CHECK DUPLICATE
        // ===============================
        public async Task<bool> ExistsAsync(string name, string code)
        {
            return await _uow.Modules.AnyAsync(x =>
                (x.Name == name || x.Code == code) &&
                !x.IsDeleted);
        }

        // ==========================================
        // 🔥 BASE QUERY (same like MenuService)
        // ==========================================
        private IQueryable<ModuleDto> BaseQuery()
        {
            return _uow.Modules.Query().AsNoTracking()
               .Where(x => x.IsActive && x.IsVisible && !x.IsDeleted)
               .Select(x => new ModuleDto
               {
                   Id = x.Id,
                   Name = x.Name,
                   Code = x.Code,
                   Icon = x.Icon,
                   DefaultRoute = x.DefaultRoute,
                   OrderNo = x.OrderNo,

                   IsDefault = x.IsDefault,
                   IsLicensed = x.IsLicensed,

                   Category = x.Category,
                   Permission = x.Permission,

                   IsActive = x.IsActive,
                   IsVisible = x.IsVisible,

                   Description = x.Description
               }).OrderBy(x => x.OrderNo);
        }
    }
}
