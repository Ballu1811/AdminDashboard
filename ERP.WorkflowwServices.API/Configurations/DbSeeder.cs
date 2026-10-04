using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Configurations
{
    public class DbSeeder
    {
        private readonly WorkflowDbContext _context;
        private readonly IConfiguration _config;
        public DbSeeder(WorkflowDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task SeedAsync()
        {
            // ================================
            // 1. ENSURE TENANT
            // ================================
            var tenantId = await _context.Users.Select(x => x.TenantId).FirstOrDefaultAsync();

            if (tenantId == Guid.Empty)
            {
                tenantId = Guid.NewGuid();
            }

            // ================================
            // 2. ROLE
            // ================================
            var role = await _context.Roles.FirstOrDefaultAsync(x => x.Code == "SUPER_ADMIN");
            if (role == null)
            {
                role = new Roles
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = "SuperAdmin",
                    Code = "SUPER_ADMIN",
                    IsSystem = true,
                    IsDefault = true,
                    IsActive = true,
                    Priority = 1
                };

                await _context.Roles.AddAsync(role);
            }

            // ================================
            // 3. MENU PERMISSIONS
            // ================================
            var existingMenuRoles = await _context.MenuRoles.AnyAsync(x => x.RoleId == role.Id);
            if (!existingMenuRoles)
            {
                var menus = await _context.MenuItems.ToListAsync();

                var menuRoles = menus.Select(menu => new MenuRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RoleId = role.Id,
                    MenuId = menu.Id,
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    IsActive = true
                });

                await _context.MenuRoles.AddRangeAsync(menuRoles);
            }

            var administrationMenu = await _context.MenuItems.FirstOrDefaultAsync(menu =>
                menu.TenantId == tenantId && menu.ParentId == null && menu.Label == "Administration");

            var platformMenus = new[]
            {
                new { Label = "Company Master", Route = "/administration/companies", Icon = "pi pi-building", Permission = "PLATFORM_COMPANIES", OrderNo = 5 },
                new { Label = "Module Master", Route = "/administration/platform/modules", Icon = "pi pi-th-large", Permission = "PLATFORM_MODULES", OrderNo = 6 }
            };

            foreach (var definition in platformMenus)
            {
                var menu = await _context.MenuItems.FirstOrDefaultAsync(item =>
                    item.TenantId == tenantId && item.Route == definition.Route);

                if (menu == null)
                {
                    menu = new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ParentId = administrationMenu?.Id,
                        Label = definition.Label,
                        Route = definition.Route,
                        Icon = definition.Icon,
                        Permission = definition.Permission,
                        MenuType = "link",
                        OrderNo = definition.OrderNo,
                        Target = "_self",
                        IsActive = true,
                        IsVisible = true,
                        ShowInSidebar = true
                    };
                    await _context.MenuItems.AddAsync(menu);
                }

                if (!await _context.MenuRoles.AnyAsync(menuRole => menuRole.MenuId == menu.Id && menuRole.RoleId == role.Id))
                {
                    await _context.MenuRoles.AddAsync(new MenuRole
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        MenuId = menu.Id,
                        RoleId = role.Id,
                        CanView = true,
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = true,
                        IsActive = true
                    });
                }
            }


            // ================================
            // 4. ADMIN USER
            // ================================
            var adminConfig = _config.GetSection("DefaultAdmin");
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == adminConfig["Username"]);
            if (user == null)
            {
                user = new Users
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FullName = adminConfig["FullName"],
                    Username = adminConfig["Username"],
                    Email = adminConfig["Email"],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminConfig["Password"]),
                    RoleId = role.Id,
                    IsActive = true,
                    IsSystem = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
            }

            // ================================
            // 5. THEME PRESET (DEFAULT TEMPLATE)
            // ================================
            var preset = await _context.ThemePresets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault);
            if (preset == null)
            {
                preset = new ThemePreset
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = "Default",
                    PrimaryColor = "#6366f1",
                    SurfaceColor = "#ffffff",
                    SidebarColor = "#111827",
                    TopbarColor = "#ffffff",
                    IsDefault = true,
                    IsActive = true
                };

                await _context.ThemePresets.AddAsync(preset);
            }

            // ================================
            // 6. TENANT THEME SETTINGS 🔥 (MAIN FIX)
            // ================================
            var tenantTheme = await _context.TenantThemeSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId);
            if (tenantTheme == null)
            {
                tenantTheme = new TenantThemeSettings
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,

                    Theme = "aura",
                    IsDarkMode = false,

                    PrimaryColor = preset.PrimaryColor,
                    SurfaceColor = preset.SurfaceColor,
                    SidebarColor = preset.SidebarColor,
                    TopbarColor = preset.TopbarColor,

                    MenuType = "static",
                    Density = "default",

                    CompanyName = "Default Company",
                    IsActive = true
                };

                await _context.TenantThemeSettings.AddAsync(tenantTheme);
            }

            // ================================
            // 7. DEFAULT USER LAYOUT SETTINGS
            // ================================
            var layoutExists = await _context.UserLayoutSettings.AnyAsync(x => x.UserId == user.Id);
            if (!layoutExists)
            {
                var layout = new UserLayoutSettings
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = user.Id,

                    OverrideTheme = false,
                    OverrideLayout = false,

                    Theme = tenantTheme.Theme,
                    IsDarkMode = tenantTheme.IsDarkMode,

                    PrimaryColor = tenantTheme.PrimaryColor,
                    SurfaceColor = tenantTheme.SurfaceColor,
                    SidebarColor = tenantTheme.SidebarColor,
                    TopbarColor = tenantTheme.TopbarColor,

                    SidebarCollapsed = false,
                    SidebarPinned = true,
                    MenuType = "static",

                    Density = "default",
                    HeaderFixed = true,
                    HeaderDensity = "default",

                    IsActive = true
                };

                await _context.UserLayoutSettings.AddAsync(layout);
            }

            // ================================
            // SAVE ALL
            // ================================
            await _context.SaveChangesAsync();
        }
    }
}
