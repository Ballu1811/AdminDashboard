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

            var moduleCatalog = new[]
            {
                new { Name = "Sales", Code = "SAL", Icon = "pi pi-chart-line", Category = "Operations", OrderNo = 4 },
                new { Name = "Inventory", Code = "INV", Icon = "pi pi-box", Category = "Operations", OrderNo = 5 },
                new { Name = "Warehouse", Code = "WH", Icon = "pi pi-warehouse", Category = "Supply Chain", OrderNo = 6 },
                new { Name = "Supply Chain", Code = "SCM", Icon = "pi pi-truck", Category = "Supply Chain", OrderNo = 7 },
                new { Name = "Manufacturing", Code = "MFG", Icon = "pi pi-cog", Category = "Operations", OrderNo = 8 },
                new { Name = "CRM", Code = "CRM", Icon = "pi pi-users", Category = "Customer", OrderNo = 9 },
                new { Name = "Human Resources", Code = "HR", Icon = "pi pi-id-card", Category = "People", OrderNo = 10 },
                new { Name = "Payroll", Code = "PAY", Icon = "pi pi-wallet", Category = "People", OrderNo = 11 },
                new { Name = "Project Management", Code = "PM", Icon = "pi pi-briefcase", Category = "Operations", OrderNo = 12 },
                new { Name = "Quality Management", Code = "QM", Icon = "pi pi-check-circle", Category = "Operations", OrderNo = 13 },
                new { Name = "Asset Management", Code = "AM", Icon = "pi pi-building", Category = "Finance", OrderNo = 14 },
                new { Name = "Reporting & Analytics", Code = "RPT", Icon = "pi pi-chart-bar", Category = "Analytics", OrderNo = 15 }
            };

            var existingModuleCodes = (await _context.Modules.IgnoreQueryFilters()
                .Where(module => module.TenantId == tenantId && !module.IsDeleted)
                .Select(module => module.Code)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in moduleCatalog)
            {
                if (!existingModuleCodes.Add(definition.Code)) continue;

                await _context.Modules.AddAsync(new Module
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = definition.Name,
                    Code = definition.Code,
                    ModuleKey = definition.Code.ToLowerInvariant(),
                    ModuleType = "Standard",
                    Version = "1.0.0",
                    Icon = definition.Icon,
                    Category = definition.Category,
                    OrderNo = definition.OrderNo,
                    IsLicensed = true,
                    IsActive = true,
                    IsVisible = true
                });
            }

            // The frontend loads its platform administration menu from a module named
            // "Administration". Keep that system module present even on a clean database.
            var administrationModule = await _context.Modules.IgnoreQueryFilters()
                .FirstOrDefaultAsync(module => module.TenantId == tenantId && !module.IsDeleted &&
                    (module.Name == "Administration" || module.Code == "ADMIN"));
            if (administrationModule == null)
            {
                administrationModule = new Module
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = "Administration",
                    Code = "ADMIN",
                    ModuleKey = "administration",
                    ModuleType = "System",
                    Version = "1.0.0",
                    Icon = "pi pi-cog",
                    Category = "Platform",
                    OrderNo = 1,
                    IsSystemModule = true,
                    IsLicensed = true,
                    ShowInSidebar = true,
                    IsActive = true,
                    IsVisible = true
                };
                await _context.Modules.AddAsync(administrationModule);
            }
            else
            {
                administrationModule.Name = "Administration";
                administrationModule.IsSystemModule = true;
                administrationModule.ShowInSidebar = true;
                administrationModule.IsActive = true;
                administrationModule.IsVisible = true;
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

            var companyAdminRole = await _context.Roles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Code == "COMPANY_ADMIN");
            if (companyAdminRole == null)
            {
                companyAdminRole = new Roles
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = "Company Admin",
                    Code = "COMPANY_ADMIN",
                    Description = "Company-scoped user administration",
                    IsSystem = false,
                    IsDefault = false,
                    IsEditable = false,
                    IsActive = true,
                    Priority = 2,
                    ShowInUI = true
                };
                await _context.Roles.AddAsync(companyAdminRole);
            }

            var managementActions = new[] { "View", "Create", "Update", "Activate", "Deactivate", "Invitation" };
            var permissionDefinitions = managementActions
                .Select(action => new { Key = $"UserManagement.Global.{action}", Name = $"Global user management: {action}", Role = role })
                .Concat(managementActions.Select(action => new
                {
                    Key = $"UserManagement.Company.{action}",
                    Name = $"Company user management: {action}",
                    Role = companyAdminRole
                }))
                .ToList();
            var permissionKeys = permissionDefinitions.Select(item => item.Key).ToList();
            var permissions = await _context.Permissions.IgnoreQueryFilters()
                .Where(permission => permission.TenantId == tenantId && permissionKeys.Contains(permission.PermissionKey))
                .ToDictionaryAsync(permission => permission.PermissionKey, StringComparer.OrdinalIgnoreCase);

            foreach (var definition in permissionDefinitions)
            {
                if (!permissions.TryGetValue(definition.Key, out var permission))
                {
                    permission = new Permission
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ModuleId = administrationModule.Id,
                        Module = administrationModule,
                        PermissionName = definition.Name,
                        PermissionKey = definition.Key,
                        PermissionType = "Action",
                        Description = definition.Name,
                        IsSystemDefined = true,
                        IsActive = true
                    };
                    permissions.Add(definition.Key, permission);
                    await _context.Permissions.AddAsync(permission);
                }

                var existingMapping = await _context.RolePermissions.IgnoreQueryFilters()
                    .AnyAsync(mapping => mapping.TenantId == tenantId
                        && mapping.RoleId == definition.Role.Id
                        && mapping.PermissionId == permission.Id);
                if (!existingMapping)
                {
                    await _context.RolePermissions.AddAsync(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        RoleId = definition.Role.Id,
                        Role = definition.Role,
                        PermissionId = permission.Id,
                        Permission = permission,
                        IsGranted = true
                    });
                }
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

            if (administrationMenu == null)
            {
                administrationMenu = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = administrationModule.Id,
                    Label = "Administration",
                    Code = "ADMINISTRATION_ROOT",
                    MenuKey = "administration_root",
                    Icon = "pi pi-cog",
                    MenuType = "group",
                    OrderNo = 1,
                    IsSystem = true,
                    IsActive = true,
                    IsVisible = true,
                    ShowInSidebar = true
                };
                await _context.MenuItems.AddAsync(administrationMenu);
            }
            else
            {
                administrationMenu.ModuleId = administrationModule.Id;
                administrationMenu.MenuType = "group";
                if (string.IsNullOrWhiteSpace(administrationMenu.Code)) administrationMenu.Code = "ADMINISTRATION_ROOT";
                if (string.IsNullOrWhiteSpace(administrationMenu.MenuKey)) administrationMenu.MenuKey = "administration_root";
                administrationMenu.IsSystem = true;
                administrationMenu.IsActive = true;
                administrationMenu.IsVisible = true;
                administrationMenu.ShowInSidebar = true;
            }

            if (administrationMenu != null && !await _context.MenuPlacements.AnyAsync(placement =>
                placement.MenuId == administrationMenu.Id))
            {
                await _context.MenuPlacements.AddAsync(new MenuPlacement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = administrationMenu.ModuleId,
                    MenuId = administrationMenu.Id,
                    OrderNo = administrationMenu.OrderNo
                });
            }

            var platformMenus = new[]
            {
                new { Label = "Company Master", Code = "PLATFORM_COMPANIES", MenuKey = "platform_companies", Route = "/administration/companies", Icon = "pi pi-building", Permission = "PLATFORM_COMPANIES", ComponentKey = "company-master", OrderNo = 5 },
                new { Label = "Module Master", Code = "PLATFORM_MODULES", MenuKey = "platform_modules", Route = "/administration/platform/modules", Icon = "pi pi-th-large", Permission = "PLATFORM_MODULES", ComponentKey = "module-master", OrderNo = 6 },
                new { Label = "Module Dependencies", Code = "PLATFORM_MODULE_DEPENDENCIES", MenuKey = "platform_module_dependencies", Route = "/administration/module-dependencies", Icon = "pi pi-share-alt", Permission = "PLATFORM_MODULES", ComponentKey = "module-dependency-master", OrderNo = 7 },
                new { Label = "User Role Mapping", Code = "USER_ROLE_MAPPING", MenuKey = "user_role_mapping", Route = "/administration/user-roles", Icon = "pi pi-users", Permission = "USER_ROLE_MAPPING", ComponentKey = "user-role-mapping", OrderNo = 8 }
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
                        ModuleId = administrationMenu?.ModuleId,
                        ParentId = administrationMenu?.Id,
                        Label = definition.Label,
                        Code = definition.Code,
                        MenuKey = definition.MenuKey,
                        Route = definition.Route,
                        Icon = definition.Icon,
                        Permission = definition.Permission,
                        ComponentKey = definition.ComponentKey,
                        MenuType = "link",
                        OrderNo = definition.OrderNo,
                        Target = "_self",
                        IsActive = true,
                        IsVisible = true,
                        ShowInSidebar = true
                    };
                    await _context.MenuItems.AddAsync(menu);
                }

                // Sidebar module trees are queried by MenuItem.ModuleId. The placement row alone
                // does not register this menu in the Administration module tree.
                if (administrationMenu != null)
                {
                    menu.ModuleId = administrationMenu.ModuleId;
                    menu.ParentId = administrationMenu.Id;
                }
                menu.ComponentKey = definition.ComponentKey;
                if (string.IsNullOrWhiteSpace(menu.Code)) menu.Code = definition.Code;
                if (string.IsNullOrWhiteSpace(menu.MenuKey)) menu.MenuKey = definition.MenuKey;

                var menuPlacement = await _context.MenuPlacements.FirstOrDefaultAsync(placement => placement.MenuId == menu.Id);
                if (menuPlacement == null)
                {
                    await _context.MenuPlacements.AddAsync(new MenuPlacement
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ModuleId = administrationMenu?.ModuleId,
                        MenuId = menu.Id,
                        ParentMenuId = administrationMenu?.Id,
                        OrderNo = definition.OrderNo
                    });
                }
                else
                {
                    menuPlacement.ModuleId = administrationMenu?.ModuleId;
                    menuPlacement.ParentMenuId = administrationMenu?.Id;
                    menuPlacement.OrderNo = definition.OrderNo;
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

            var rolePermissionGroup = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(menu =>
                menu.TenantId == tenantId && !menu.IsDeleted && menu.MenuType == "group" &&
                (menu.MenuKey == "role_permission_management" || menu.Label == "Role & Permission Management"));
            if (rolePermissionGroup == null)
            {
                rolePermissionGroup = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = administrationMenu?.ModuleId,
                    ParentId = administrationMenu?.Id,
                    Label = "Role & Permission Management",
                    Code = "ROLE_PERMISSION_MANAGEMENT",
                    MenuKey = "role_permission_management",
                    Icon = "pi pi-shield",
                    MenuType = "group",
                    OrderNo = 9,
                    IsSystem = true,
                    IsActive = true,
                    IsVisible = true,
                    ShowInSidebar = true
                };
                await _context.MenuItems.AddAsync(rolePermissionGroup);
            }
            rolePermissionGroup.ModuleId = administrationMenu?.ModuleId;
            rolePermissionGroup.ParentId = administrationMenu?.Id;
            rolePermissionGroup.Label = "Role & Permission Management";
            rolePermissionGroup.Code = "ROLE_PERMISSION_MANAGEMENT";
            rolePermissionGroup.MenuKey = "role_permission_management";
            rolePermissionGroup.MenuType = "group";
            rolePermissionGroup.OrderNo = 9;
            rolePermissionGroup.IsSystem = true;
            rolePermissionGroup.IsActive = true;
            rolePermissionGroup.IsVisible = true;
            rolePermissionGroup.ShowInSidebar = true;
            rolePermissionGroup.IsHidden = false;

            var rolePermissionPlacement = await _context.MenuPlacements.IgnoreQueryFilters().FirstOrDefaultAsync(placement =>
                placement.TenantId == tenantId && placement.MenuId == rolePermissionGroup.Id);
            if (rolePermissionPlacement == null)
            {
                await _context.MenuPlacements.AddAsync(new MenuPlacement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = rolePermissionGroup.ModuleId,
                    MenuId = rolePermissionGroup.Id,
                    ParentMenuId = administrationMenu?.Id,
                    OrderNo = rolePermissionGroup.OrderNo
                });
            }
            else
            {
                rolePermissionPlacement.ModuleId = rolePermissionGroup.ModuleId;
                rolePermissionPlacement.ParentMenuId = administrationMenu?.Id;
                rolePermissionPlacement.OrderNo = rolePermissionGroup.OrderNo;
                rolePermissionPlacement.IsDeleted = false;
                rolePermissionPlacement.DeletedAt = null;
                rolePermissionPlacement.DeletedBy = null;
            }

            var roleGroupMenu = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(menu =>
                menu.TenantId == tenantId && menu.Route == "/administration/role-groups");
            if (roleGroupMenu == null)
            {
                roleGroupMenu = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Label = "Role Group Master",
                    Code = "ROLE_GROUP_MASTER",
                    MenuKey = "role_group_master",
                    Route = "/administration/role-groups",
                    Icon = "pi pi-users",
                    Permission = "ROLE_GROUP_MASTER",
                    ComponentKey = "role-group-master",
                    MenuType = "link",
                    OrderNo = 1,
                    Target = "_self",
                    IsActive = true,
                    IsVisible = true,
                    ShowInSidebar = true
                };
                await _context.MenuItems.AddAsync(roleGroupMenu);
            }

            roleGroupMenu.Label = "Role Group Master";
            roleGroupMenu.Code = "ROLE_GROUP_MASTER";
            roleGroupMenu.MenuKey = "role_group_master";
            roleGroupMenu.Route = "/administration/role-groups";
            roleGroupMenu.Icon = "pi pi-users";
            roleGroupMenu.Permission = "ROLE_GROUP_MASTER";
            roleGroupMenu.ComponentKey = "role-group-master";
            roleGroupMenu.MenuType = "link";
            roleGroupMenu.ModuleId = rolePermissionGroup.ModuleId ?? administrationMenu?.ModuleId;
            roleGroupMenu.ParentId = rolePermissionGroup.Id;
            roleGroupMenu.OrderNo = 1;
            roleGroupMenu.IsActive = true;
            roleGroupMenu.IsVisible = true;
            roleGroupMenu.ShowInSidebar = true;
            roleGroupMenu.Target = "_self";
            roleGroupMenu.IsDeleted = false;
            roleGroupMenu.DeletedAt = null;
            roleGroupMenu.DeletedBy = null;

            var roleGroupPlacement = await _context.MenuPlacements.IgnoreQueryFilters().FirstOrDefaultAsync(placement =>
                placement.TenantId == tenantId && placement.MenuId == roleGroupMenu.Id);
            if (roleGroupPlacement == null)
            {
                await _context.MenuPlacements.AddAsync(new MenuPlacement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = roleGroupMenu.ModuleId,
                    MenuId = roleGroupMenu.Id,
                    ParentMenuId = rolePermissionGroup.Id,
                    OrderNo = roleGroupMenu.OrderNo
                });
            }
            else
            {
                roleGroupPlacement.ModuleId = roleGroupMenu.ModuleId;
                roleGroupPlacement.ParentMenuId = rolePermissionGroup.Id;
                roleGroupPlacement.OrderNo = roleGroupMenu.OrderNo;
                roleGroupPlacement.IsDeleted = false;
                roleGroupPlacement.DeletedAt = null;
                roleGroupPlacement.DeletedBy = null;
            }

            var roleGroupPermission = await _context.MenuRoles.IgnoreQueryFilters().FirstOrDefaultAsync(menuRole =>
                menuRole.TenantId == tenantId && menuRole.MenuId == roleGroupMenu.Id && menuRole.RoleId == role.Id);
            if (roleGroupPermission == null)
            {
                await _context.MenuRoles.AddAsync(new MenuRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    MenuId = roleGroupMenu.Id,
                    RoleId = role.Id,
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    IsActive = true
                });
            }
            else
            {
                roleGroupPermission.IsActive = true;
                roleGroupPermission.IsDeleted = false;
                roleGroupPermission.DeletedAt = null;
                roleGroupPermission.DeletedBy = null;
                roleGroupPermission.CanView = true;
                roleGroupPermission.CanCreate = true;
                roleGroupPermission.CanEdit = true;
                roleGroupPermission.CanDelete = true;
            }

            var permissionMasterMenu = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(menu =>
                menu.TenantId == tenantId && menu.Route == "/administration/permissions");
            if (permissionMasterMenu == null)
            {
                permissionMasterMenu = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Label = "Permission Master",
                    Code = "PERMISSION_MASTER",
                    MenuKey = "permission_master",
                    Route = "/administration/permissions",
                    Icon = "pi pi-shield",
                    Permission = "PERMISSION_MASTER",
                    ComponentKey = "permission-master",
                    MenuType = "link",
                    OrderNo = 2,
                    Target = "_self",
                    IsSystem = true,
                    IsActive = true,
                    IsVisible = true,
                    ShowInSidebar = true
                };
                await _context.MenuItems.AddAsync(permissionMasterMenu);
            }

            permissionMasterMenu.Label = "Permission Master";
            permissionMasterMenu.Code = "PERMISSION_MASTER";
            permissionMasterMenu.MenuKey = "permission_master";
            permissionMasterMenu.Route = "/administration/permissions";
            permissionMasterMenu.Icon = "pi pi-shield";
            permissionMasterMenu.Permission = "PERMISSION_MASTER";
            permissionMasterMenu.ComponentKey = "permission-master";
            permissionMasterMenu.MenuType = "link";
            permissionMasterMenu.ModuleId = rolePermissionGroup.ModuleId ?? administrationMenu?.ModuleId;
            permissionMasterMenu.ParentId = rolePermissionGroup.Id;
            permissionMasterMenu.OrderNo = 2;
            permissionMasterMenu.Target = "_self";
            permissionMasterMenu.IsSystem = true;
            permissionMasterMenu.IsActive = true;
            permissionMasterMenu.IsVisible = true;
            permissionMasterMenu.ShowInSidebar = true;
            permissionMasterMenu.IsDeleted = false;
            permissionMasterMenu.DeletedAt = null;
            permissionMasterMenu.DeletedBy = null;

            var permissionMasterPlacement = await _context.MenuPlacements.IgnoreQueryFilters().FirstOrDefaultAsync(placement =>
                placement.TenantId == tenantId && placement.MenuId == permissionMasterMenu.Id);
            if (permissionMasterPlacement == null)
            {
                await _context.MenuPlacements.AddAsync(new MenuPlacement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = permissionMasterMenu.ModuleId,
                    MenuId = permissionMasterMenu.Id,
                    ParentMenuId = rolePermissionGroup.Id,
                    OrderNo = permissionMasterMenu.OrderNo
                });
            }
            else
            {
                permissionMasterPlacement.ModuleId = permissionMasterMenu.ModuleId;
                permissionMasterPlacement.ParentMenuId = rolePermissionGroup.Id;
                permissionMasterPlacement.OrderNo = permissionMasterMenu.OrderNo;
                permissionMasterPlacement.IsDeleted = false;
                permissionMasterPlacement.DeletedAt = null;
                permissionMasterPlacement.DeletedBy = null;
            }

            var permissionMasterAccess = await _context.MenuRoles.IgnoreQueryFilters().FirstOrDefaultAsync(menuRole =>
                menuRole.TenantId == tenantId && menuRole.MenuId == permissionMasterMenu.Id && menuRole.RoleId == role.Id);
            if (permissionMasterAccess == null)
            {
                await _context.MenuRoles.AddAsync(new MenuRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    MenuId = permissionMasterMenu.Id,
                    RoleId = role.Id,
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    IsActive = true
                });
            }
            else
            {
                permissionMasterAccess.IsActive = true;
                permissionMasterAccess.IsDeleted = false;
                permissionMasterAccess.DeletedAt = null;
                permissionMasterAccess.DeletedBy = null;
                permissionMasterAccess.CanView = true;
                permissionMasterAccess.CanCreate = true;
                permissionMasterAccess.CanEdit = true;
                permissionMasterAccess.CanDelete = true;
            }

            var legacyRolePermissionMappingMenu = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(menu =>
                menu.TenantId == tenantId &&
                (menu.Route == "/administration/role-group-permissions" ||
                 menu.ComponentKey == "RoleGroupPermissionMapping" ||
                 menu.Label == "Role Group Permission Mapping"));
            var newRolePermissionMappingMenu = await _context.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(menu =>
                menu.TenantId == tenantId && menu.Route == "/administration/role-permissions");
            var useLegacyRolePermissionMappingMenu = legacyRolePermissionMappingMenu != null;
            var rolePermissionMappingMenu = legacyRolePermissionMappingMenu ?? newRolePermissionMappingMenu;
            if (rolePermissionMappingMenu == null)
            {
                rolePermissionMappingMenu = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Label = "Role Permission Mapping",
                    Code = "ROLE_PERMISSION_MAPPING",
                    MenuKey = "role_permission_mapping",
                    Route = "/administration/role-permissions",
                    Icon = "pi pi-lock",
                    Permission = "ROLE_PERMISSION_MAPPING",
                    ComponentKey = "role-permission-mapping",
                    MenuType = "link",
                    OrderNo = 3,
                    Target = "_self",
                    IsSystem = true,
                    IsActive = true,
                    IsVisible = true,
                    ShowInSidebar = true
                };
                await _context.MenuItems.AddAsync(rolePermissionMappingMenu);
            }

            if (useLegacyRolePermissionMappingMenu &&
                newRolePermissionMappingMenu != null &&
                newRolePermissionMappingMenu.Id != rolePermissionMappingMenu.Id)
            {
                newRolePermissionMappingMenu.IsDeleted = true;
                newRolePermissionMappingMenu.DeletedAt = DateTime.UtcNow;
                newRolePermissionMappingMenu.DeletedBy = role.CreatedBy;

                var duplicatePlacements = await _context.MenuPlacements.IgnoreQueryFilters()
                    .Where(placement => placement.TenantId == tenantId && placement.MenuId == newRolePermissionMappingMenu.Id)
                    .ToListAsync();
                foreach (var placement in duplicatePlacements)
                {
                    placement.IsDeleted = true;
                    placement.DeletedAt = DateTime.UtcNow;
                    placement.DeletedBy = role.CreatedBy;
                }

                var duplicateMenuRoles = await _context.MenuRoles.IgnoreQueryFilters()
                    .Where(menuRole => menuRole.TenantId == tenantId && menuRole.MenuId == newRolePermissionMappingMenu.Id)
                    .ToListAsync();
                foreach (var menuRole in duplicateMenuRoles)
                {
                    menuRole.IsDeleted = true;
                    menuRole.DeletedAt = DateTime.UtcNow;
                    menuRole.DeletedBy = role.CreatedBy;
                }
            }

            rolePermissionMappingMenu.Label = useLegacyRolePermissionMappingMenu
                ? "Role Group Permission Mapping"
                : "Role Permission Mapping";
            rolePermissionMappingMenu.Code = useLegacyRolePermissionMappingMenu
                ? "ROLE_GROUP_PERMISSION_MAPPING"
                : "ROLE_PERMISSION_MAPPING";
            rolePermissionMappingMenu.MenuKey = useLegacyRolePermissionMappingMenu
                ? "role_group_permission_mapping"
                : "role_permission_mapping";
            rolePermissionMappingMenu.Route = useLegacyRolePermissionMappingMenu
                ? "/administration/role-group-permissions"
                : "/administration/role-permissions";
            rolePermissionMappingMenu.Icon = "pi pi-lock";
            rolePermissionMappingMenu.Permission = useLegacyRolePermissionMappingMenu
                ? "ROLE_GROUP_PERMISSION_MAPPING"
                : "ROLE_PERMISSION_MAPPING";
            rolePermissionMappingMenu.ComponentKey = useLegacyRolePermissionMappingMenu
                ? "role-group-permission-mapping"
                : "role-permission-mapping";
            rolePermissionMappingMenu.MenuType = "link";
            rolePermissionMappingMenu.ModuleId = rolePermissionGroup.ModuleId ?? administrationMenu?.ModuleId;
            rolePermissionMappingMenu.ParentId = rolePermissionGroup.Id;
            rolePermissionMappingMenu.OrderNo = 3;
            rolePermissionMappingMenu.Target = "_self";
            rolePermissionMappingMenu.IsSystem = true;
            rolePermissionMappingMenu.IsActive = true;
            rolePermissionMappingMenu.IsVisible = true;
            rolePermissionMappingMenu.ShowInSidebar = true;
            rolePermissionMappingMenu.IsDeleted = false;
            rolePermissionMappingMenu.DeletedAt = null;
            rolePermissionMappingMenu.DeletedBy = null;

            var rolePermissionMappingPlacement = await _context.MenuPlacements.IgnoreQueryFilters().FirstOrDefaultAsync(placement =>
                placement.TenantId == tenantId && placement.MenuId == rolePermissionMappingMenu.Id);
            if (rolePermissionMappingPlacement == null)
            {
                await _context.MenuPlacements.AddAsync(new MenuPlacement
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = rolePermissionMappingMenu.ModuleId,
                    MenuId = rolePermissionMappingMenu.Id,
                    ParentMenuId = rolePermissionGroup.Id,
                    OrderNo = rolePermissionMappingMenu.OrderNo
                });
            }
            else
            {
                rolePermissionMappingPlacement.ModuleId = rolePermissionMappingMenu.ModuleId;
                rolePermissionMappingPlacement.ParentMenuId = rolePermissionGroup.Id;
                rolePermissionMappingPlacement.OrderNo = rolePermissionMappingMenu.OrderNo;
                rolePermissionMappingPlacement.IsDeleted = false;
                rolePermissionMappingPlacement.DeletedAt = null;
                rolePermissionMappingPlacement.DeletedBy = null;
            }

            var rolePermissionMappingAccess = await _context.MenuRoles.IgnoreQueryFilters().FirstOrDefaultAsync(menuRole =>
                menuRole.TenantId == tenantId && menuRole.MenuId == rolePermissionMappingMenu.Id && menuRole.RoleId == role.Id);
            if (rolePermissionMappingAccess == null)
            {
                await _context.MenuRoles.AddAsync(new MenuRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    MenuId = rolePermissionMappingMenu.Id,
                    RoleId = role.Id,
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true,
                    IsActive = true
                });
            }
            else
            {
                rolePermissionMappingAccess.IsActive = true;
                rolePermissionMappingAccess.IsDeleted = false;
                rolePermissionMappingAccess.DeletedAt = null;
                rolePermissionMappingAccess.DeletedBy = null;
                rolePermissionMappingAccess.CanView = true;
                rolePermissionMappingAccess.CanCreate = true;
                rolePermissionMappingAccess.CanEdit = true;
                rolePermissionMappingAccess.CanDelete = true;
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
