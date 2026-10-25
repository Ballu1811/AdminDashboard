using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ERP.WorkflowwServices.API.Services;

public sealed class SubModuleService : ISubModuleService
{
    private readonly WorkflowDbContext _db;
    private readonly IUserContext _user;
    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
        { "name", "code", "moduleKey", "orderNo", "parentName", "isLicensed", "isActive" };

    public SubModuleService(WorkflowDbContext db, IUserContext user) { _db = db; _user = user; }

    public async Task<PagedResult<SubModuleDto>> GetAllAsync(FilterModel filter)
    {
        var query = _db.Set<SubModule>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.TenantId == _user.TenantId);
        if (filter.ParentId.HasValue) query = query.Where(x => x.ParentId == filter.ParentId.Value);
        if (filter.Status == 0) query = query.Where(x => !x.IsDeleted && x.IsActive);
        else if (filter.Status == 1) query = query.Where(x => x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(filter.KeyWord) && filter.KeyWord != "null")
        {
            var keyword = filter.KeyWord.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword) || x.ModuleKey.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)));
        }
        var total = await query.CountAsync();
        var ascending = string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var sort = SortFields.Contains(filter.SortBy ?? "") ? filter.SortBy!.ToLowerInvariant() : "orderno";
        query = (sort, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.Name), ("name", false) => query.OrderByDescending(x => x.Name),
            ("code", true) => query.OrderBy(x => x.Code), ("code", false) => query.OrderByDescending(x => x.Code),
            ("modulekey", true) => query.OrderBy(x => x.ModuleKey), ("modulekey", false) => query.OrderByDescending(x => x.ModuleKey),
            ("islicensed", true) => query.OrderBy(x => x.IsLicensed), ("islicensed", false) => query.OrderByDescending(x => x.IsLicensed),
            ("isactive", true) => query.OrderBy(x => x.IsActive), ("isactive", false) => query.OrderByDescending(x => x.IsActive),
            ("parentname", true) => query.OrderBy(x => x.Parent!.Name), ("parentname", false) => query.OrderByDescending(x => x.Parent!.Name),
            _ when ascending => query.OrderBy(x => x.OrderNo), _ => query.OrderByDescending(x => x.OrderNo)
        };
        var page = Math.Max(1, filter.PageNumber); var size = Math.Clamp(filter.PageSize, 1, 100);
        return new PagedResult<SubModuleDto> { Total = total, Data = await Project(query).Skip((page - 1) * size).Take(size).ToListAsync() };
    }

    public Task<SubModuleDto?> GetByIdAsync(Guid id) => Project(_db.Set<SubModule>().IgnoreQueryFilters().AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IReadOnlyList<SubModuleDto>> GetByParentIdAsync(Guid parentId) => await Project(_db.Set<SubModule>().AsNoTracking())
        .Where(x => x.ParentId == parentId && x.IsActive && x.IsVisible).OrderBy(x => x.OrderNo).ToListAsync();

    public async Task<SubModuleDto> CreateAsync(SubModuleCreateDto dto)
    {
        Validate(dto);
        await ValidateParentAsync(dto.ParentId);
        await EnsureUniqueAsync(dto, null);
        var entity = new SubModule(); Apply(entity, dto); entity.Id = Guid.NewGuid();
        _db.Add(entity); await _db.SaveChangesAsync();
        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Submodule was created but could not be loaded.");
    }

    public async Task UpdateAsync(SubModuleUpdateDto dto)
    {
        Validate(dto);
        var entity = await _db.Set<SubModule>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == _user.TenantId);
        if (entity is null) throw new KeyNotFoundException("Submodule not found.");
        await ValidateParentAsync(dto.ParentId); await EnsureUniqueAsync(dto, dto.Id);
        Apply(entity, dto); await _db.SaveChangesAsync();
    }

    public async Task ArchiveAsync(Guid id)
    {
        var entity = await FindIncludingDeleted(id);
        if (entity is null) throw new KeyNotFoundException("Submodule not found.");
        if (entity.IsDeleted) throw new InvalidOperationException("Submodule is already archived.");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedBy = _user.UserId;
        await _db.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid id)
    {
        var entity = await FindIncludingDeleted(id);
        if (entity is null) throw new KeyNotFoundException("Submodule not found.");
        if (!entity.IsDeleted) throw new InvalidOperationException("Submodule is not archived.");
        if (!await _db.Modules.AnyAsync(x => x.Id == entity.ParentId && x.TenantId == _user.TenantId && x.IsActive && !x.IsDeleted))
            throw new InvalidOperationException("The parent module must be active before restoring this submodule.");
        entity.IsDeleted = false; entity.DeletedAt = null; entity.DeletedBy = null;
        await EnsureUniqueAsync(new SubModuleCreateDto { ParentId = entity.ParentId, Name = entity.Name, Code = entity.Code, ModuleKey = entity.ModuleKey }, entity.Id); await _db.SaveChangesAsync();
    }

    private IQueryable<SubModuleDto> Project(IQueryable<SubModule> q) => q.Where(x => x.TenantId == _user.TenantId).Select(x => new SubModuleDto
    {
        Id = x.Id, ParentId = x.ParentId, ParentName = x.Parent == null || x.Parent.TenantId != _user.TenantId ? "" : x.Parent.Name, Name = x.Name, Code = x.Code,
        ModuleKey = x.ModuleKey, Description = x.Description, OrderNo = x.OrderNo, Icon = x.Icon, DefaultRoute = x.DefaultRoute,
        ComponentKey = x.ComponentKey, Permission = x.Permission, IsActive = x.IsActive, IsVisible = x.IsVisible,
        IsLicensed = x.IsLicensed, ShowInSidebar = x.ShowInSidebar, ShowInTopbar = x.ShowInTopbar, IsDeleted = x.IsDeleted
    });

    private async Task<SubModule?> FindIncludingDeleted(Guid id) => await _db.Set<SubModule>().IgnoreQueryFilters()
        .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _user.TenantId);

    private async Task ValidateParentAsync(Guid id)
    {
        if (id == Guid.Empty || !await _db.Modules.AnyAsync(x => x.Id == id && x.TenantId == _user.TenantId && x.IsActive && !x.IsDeleted))
            throw new InvalidOperationException("Select an active parent module from this tenant.");
    }
    private async Task EnsureUniqueAsync(SubModuleCreateDto dto, Guid? excludeId)
    {
        var duplicate = await _db.Set<SubModule>().IgnoreQueryFilters().AnyAsync(x => x.TenantId == _user.TenantId && !x.IsDeleted && x.Id != excludeId &&
            (x.Code == dto.Code || x.ModuleKey == dto.ModuleKey));
        if (duplicate) throw new InvalidOperationException("Submodule code and key must be unique within this tenant.");
    }
    private static void Validate(SubModuleCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100 || string.IsNullOrWhiteSpace(dto.Code) || dto.Code.Length > 50 ||
            string.IsNullOrWhiteSpace(dto.ModuleKey) || dto.ModuleKey.Length > 100 || dto.OrderNo < 0 ||
            !Regex.IsMatch(dto.Code, "^[A-Za-z0-9_-]+$") || !Regex.IsMatch(dto.ModuleKey, "^[a-z0-9_-]+$"))
            throw new InvalidOperationException("Submodule fields are invalid. Check required values, lengths, formats, and display order.");
    }
    private static void Apply(SubModule entity, SubModuleCreateDto dto)
    {
        entity.ParentId = dto.ParentId; entity.Name = dto.Name.Trim(); entity.Code = dto.Code.Trim(); entity.ModuleKey = dto.ModuleKey.Trim();
        entity.Description = dto.Description; entity.OrderNo = dto.OrderNo; entity.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "pi pi-box" : dto.Icon;
        entity.DefaultRoute = dto.DefaultRoute; entity.ComponentKey = dto.ComponentKey; entity.Permission = dto.Permission;
        entity.IsActive = dto.IsActive; entity.IsVisible = dto.IsVisible; entity.IsLicensed = dto.IsLicensed;
        entity.ShowInSidebar = dto.ShowInSidebar; entity.ShowInTopbar = dto.ShowInTopbar;
    }
}
