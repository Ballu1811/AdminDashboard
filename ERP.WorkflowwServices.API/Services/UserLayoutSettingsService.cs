using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class UserLayoutSettingsService : IUserLayoutSettingsService
    {
        private readonly IUnitOfWork _uow;

        public UserLayoutSettingsService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /* =========================================================
           🔥 GET EFFECTIVE (USER + TENANT MERGED)
        ========================================================= */
        public async Task<UserLayoutSettingsDto> GetEffectiveAsync(Guid userId, Guid tenantId)
        {
            var user = await _uow.UserLayoutSettings.FirstOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId);
            var tenant = await _uow.TenantThemeSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId);
            if (tenant == null)
                throw new Exception("Tenant theme not configured");

            return new UserLayoutSettingsDto
            {
                UserId = userId,
                TenantId = tenantId,

                /* 🔥 OVERRIDE FLAGS */
                OverrideTheme = user?.OverrideTheme ?? false,
                OverrideLayout = user?.OverrideLayout ?? false,

                /* ================= SIDEBAR ================= */
                SidebarCollapsed = user?.SidebarCollapsed ?? false,
                SidebarPinned = user?.SidebarPinned ?? true,
                MenuType = (user?.OverrideLayout == true) ? user.MenuType : tenant.MenuType,

                /* ================= THEME ================= */
                Theme = (user?.OverrideTheme == true) ? user.Theme : tenant.Theme,
                IsDarkMode = (user?.OverrideTheme == true) ? user?.IsDarkMode ?? tenant.IsDarkMode : tenant.IsDarkMode,

                /* ================= COLORS ================= */
                PrimaryColor = (user?.OverrideTheme == true) ? user.PrimaryColor : tenant.PrimaryColor,
                SurfaceColor = (user?.OverrideTheme == true) ? user.SurfaceColor : tenant.SurfaceColor,
                SidebarColor = (user?.OverrideTheme == true) ? user.SidebarColor : tenant.SidebarColor,
                TopbarColor = (user?.OverrideTheme == true) ? user.TopbarColor : tenant.TopbarColor,

                /* ================= UI ================= */
                Density = user?.Density ?? tenant.Density ?? "default",
                HeaderFixed = user?.HeaderFixed ?? true,
                HeaderDensity = user?.HeaderDensity ?? "default",

                ThemePreset = user?.ThemePreset,
                UseSystemTheme = user?.UseSystemTheme ?? false
            };
        }

        /* =========================================================
           GET RAW USER SETTINGS
        ========================================================= */
        public async Task<UserLayoutSettingsDto?> GetAsync(Guid userId, Guid tenantId)
        {
            var entity = await _uow.UserLayoutSettings.FirstOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId);

            return entity == null ? null : MapToDto(entity);
        }

        /* =========================================================
           RESET TO TENANT DEFAULT
        ========================================================= */
        public async Task ResetToTenantAsync(Guid userId, Guid tenantId)
        {
            var entity = await _uow.UserLayoutSettings.FirstOrDefaultTrackedAsync(x => x.UserId == userId && x.TenantId == tenantId);

            if (entity != null)
            {
                _uow.UserLayoutSettings.Remove(entity);
                await _uow.SaveChangesAsync();
            }
        }

        /* =========================================================
          SAVE / UPDATE USER SETTINGS
       ========================================================= */
        public async Task<UserLayoutSettingsDto> SaveAsync(UserLayoutSettingsDto dto)
        {
            var entity = await _uow.UserLayoutSettings.FirstOrDefaultTrackedAsync(x => x.UserId == dto.UserId && x.TenantId == dto.TenantId);

            if (entity == null)
            {
                entity = new UserLayoutSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    TenantId = dto.TenantId
                };

                await _uow.UserLayoutSettings.AddAsync(entity);
            }

            /* 🔥 OVERRIDE FLAGS */
            entity.OverrideTheme = dto.OverrideTheme;
            entity.OverrideLayout = dto.OverrideLayout;

            /* SIDEBAR */
            entity.SidebarCollapsed = dto.SidebarCollapsed;
            entity.SidebarPinned = dto.SidebarPinned;

            if (!string.IsNullOrEmpty(dto.MenuType)) entity.MenuType = dto.MenuType;

            /* THEME */
            if (!string.IsNullOrEmpty(dto.Theme)) entity.Theme = dto.Theme;

            entity.IsDarkMode = dto.IsDarkMode;

            /* COLORS */
            entity.PrimaryColor = dto.PrimaryColor ?? entity.PrimaryColor;
            entity.SurfaceColor = dto.SurfaceColor ?? entity.SurfaceColor;
            entity.SidebarColor = dto.SidebarColor ?? entity.SidebarColor;
            entity.TopbarColor = dto.TopbarColor ?? entity.TopbarColor;

            /* UI */
            if (!string.IsNullOrEmpty(dto.Density)) entity.Density = dto.Density;

            entity.HeaderFixed = dto.HeaderFixed;

            if (!string.IsNullOrEmpty(dto.HeaderDensity)) entity.HeaderDensity = dto.HeaderDensity;

            /* ADVANCED */
            entity.ThemePreset = dto.ThemePreset ?? entity.ThemePreset;
            entity.UseSystemTheme = dto.UseSystemTheme;

            try
            {
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            

            return MapToDto(entity);
        }

        /* =========================================================
           🔁 MAPPING
        ========================================================= */
        private UserLayoutSettingsDto MapToDto(UserLayoutSettings e)
        {
            return new UserLayoutSettingsDto
            {
                UserId = e.UserId,
                TenantId = e.TenantId,

                OverrideTheme = e.OverrideTheme,
                OverrideLayout = e.OverrideLayout,

                SidebarCollapsed = e.SidebarCollapsed,
                SidebarPinned = e.SidebarPinned,
                MenuType = e.MenuType,

                Theme = e.Theme,
                IsDarkMode = e.IsDarkMode,

                PrimaryColor = e.PrimaryColor,
                SurfaceColor = e.SurfaceColor,
                SidebarColor = e.SidebarColor,
                TopbarColor = e.TopbarColor,

                Density = e.Density,
                HeaderFixed = e.HeaderFixed,
                HeaderDensity = e.HeaderDensity,

                ThemePreset = e.ThemePreset,
                UseSystemTheme = e.UseSystemTheme
            };
        }
    }
}
