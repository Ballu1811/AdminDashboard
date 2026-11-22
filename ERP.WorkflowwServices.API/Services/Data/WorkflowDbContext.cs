using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.Interfaces.Common;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.WorkflowContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ERP.WorkflowwServices.API.Services.Data
{
    public class WorkflowDbContext : DbContext
    {
        private readonly IWorkflowExecutionContextAccessor _workflow;
        public Guid CurrentTenantId => _workflow.Context.TenantId;
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options, IWorkflowExecutionContextAccessor workflow) : base(options)
        {
            _workflow = workflow;
        }

        public DbSet<WFEvent> WFEvents { get; set; }
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<PositionRole> PositionRoles => Set<PositionRole>();
        public DbSet<RoleGroup> RoleGroups => Set<RoleGroup>();
        public DbSet<RoleGroupMapping> RoleGroupMappings => Set<RoleGroupMapping>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<GlobalWorkflow> GlobalWorkflows => Set<GlobalWorkflow>();
        public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
        public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
        public DbSet<WorkflowStageAction> WorkflowStageActions => Set<WorkflowStageAction>();
        public DbSet<WorkflowCondition> WorkflowConditions => Set<WorkflowCondition>();
        public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<MenuPlacement> MenuPlacements { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleDependency> ModuleDependencies { get; set; }
        public DbSet<SubModule> SubModules { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<CompanyModuleAccess> CompanyModuleAccesses { get; set; }
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<CompanyUserSettings> CompanyUserSettings => Set<CompanyUserSettings>();
        public DbSet<UserPositionAssignment> UserPositionAssignments => Set<UserPositionAssignment>();
        public DbSet<UserCompanyAccess> UserCompanyAccesses => Set<UserCompanyAccess>();
        public DbSet<UserBranchAccess> UserBranchAccesses => Set<UserBranchAccess>();
        public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

        #region Auth & Users
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
        public DbSet<MenuRole> MenuRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        #endregion

        #region Theme Managements
        public DbSet<UserLayoutSettings> UserLayoutSettings { get; set; }
        public DbSet<TenantThemeSettings> TenantThemeSettings { get; set; }
        public DbSet<ThemePreset> ThemePresets { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ApplyGlobalFilters(modelBuilder);

            modelBuilder.Entity<Department>().HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<Department>().HasOne(x => x.ParentDepartment).WithMany(x => x.ChildDepartments).HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Position>().HasIndex(x => new { x.TenantId, x.CompanyId, x.PositionCode }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<Position>().HasIndex(x => new { x.TenantId, x.DepartmentId });
            modelBuilder.Entity<Position>().HasIndex(x => new { x.TenantId, x.ParentPositionId });
            modelBuilder.Entity<Position>().HasOne(x => x.ParentPosition).WithMany(x => x.ChildPositions).HasForeignKey(x => x.ParentPositionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Position>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Position>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PositionRole>().HasIndex(x => new { x.TenantId, x.PositionId, x.RoleId }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<PositionRole>().HasOne(x => x.Position).WithMany(x => x.PositionRoles).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PositionRole>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RoleGroup>().HasIndex(x => new { x.TenantId, x.CompanyId, x.GroupCode }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<RoleGroup>().HasIndex(x => new { x.TenantId, x.CompanyId, x.NormalizedGroupName }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<RoleGroup>().HasIndex(x => new { x.TenantId, x.DepartmentId });
            modelBuilder.Entity<RoleGroup>().HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RoleGroup>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RoleGroupMapping>().HasIndex(x => new { x.TenantId, x.RoleGroupId, x.RoleId }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<RoleGroupMapping>().HasOne(x => x.RoleGroup).WithMany(x => x.RoleMappings).HasForeignKey(x => x.RoleGroupId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RoleGroupMapping>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Permission>().HasIndex(x => new { x.TenantId, x.PermissionKey }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<Permission>().HasIndex(x => new { x.TenantId, x.ModuleId });
            modelBuilder.Entity<Permission>().HasIndex(x => new { x.TenantId, x.SubModuleId });
            modelBuilder.Entity<Permission>().HasIndex(x => new { x.TenantId, x.MenuId });
            modelBuilder.Entity<Permission>().HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Permission>().HasOne(x => x.SubModule).WithMany().HasForeignKey(x => x.SubModuleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Permission>().HasOne(x => x.Menu).WithMany().HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.TenantId, x.RoleId, x.PermissionId }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<RolePermission>().HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RolePermission>().HasOne(x => x.Permission).WithMany(x => x.RoleMappings).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GlobalWorkflow>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowStage>().HasIndex(x => new { x.TenantId, x.WorkflowId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowStage>().HasIndex(x => new { x.TenantId, x.WorkflowId, x.SequenceNumber }).HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowAction>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowStageAction>().HasIndex(x => new { x.TenantId, x.WorkflowStageId, x.WorkflowActionId }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowCondition>().HasIndex(x => new { x.TenantId, x.WorkflowId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowTransition>().HasIndex(x => new { x.TenantId, x.WorkflowId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<WorkflowStage>().HasOne<GlobalWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WorkflowCondition>().HasOne<GlobalWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WorkflowTransition>().HasOne<GlobalWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WorkflowStageAction>().HasOne<WorkflowStage>().WithMany().HasForeignKey(x => x.WorkflowStageId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WorkflowStageAction>().HasOne<WorkflowAction>().WithMany().HasForeignKey(x => x.WorkflowActionId).OnDelete(DeleteBehavior.Restrict);

            // ✅ Self reference safe
            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuItem>()
                .HasIndex(menu => new { menu.TenantId, menu.Code })
                .IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<MenuItem>()
                .HasIndex(menu => new { menu.TenantId, menu.MenuKey })
                .IsUnique().HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Module>()
                .HasOne(module => module.ParentModule)
                .WithMany(module => module.ChildModules)
                .HasForeignKey(module => module.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Module>()
                .HasIndex(module => new { module.TenantId, module.ModuleKey })
                .IsUnique().HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<ModuleDependency>()
                .HasIndex(dependency => new { dependency.TenantId, dependency.ModuleId, dependency.RequiredModuleId })
                .IsUnique();
            modelBuilder.Entity<ModuleDependency>()
                .HasOne(dependency => dependency.Module)
                .WithMany()
                .HasForeignKey(dependency => dependency.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ModuleDependency>()
                .HasOne(dependency => dependency.RequiredModule)
                .WithMany()
                .HasForeignKey(dependency => dependency.RequiredModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubModule>()
                .HasOne(subModule => subModule.Parent)
                .WithMany(module => module.SubModules)
                .HasForeignKey(subModule => subModule.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubModule>()
                .HasIndex(subModule => new { subModule.TenantId, subModule.Code })
                .IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<SubModule>()
                .HasIndex(subModule => new { subModule.TenantId, subModule.ModuleKey })
                .IsUnique().HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<MenuPlacement>()
                .HasIndex(placement => new { placement.TenantId, placement.ModuleId, placement.MenuId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<MenuPlacement>()
                .HasOne(placement => placement.Module)
                .WithMany()
                .HasForeignKey(placement => placement.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuPlacement>()
                .HasOne(placement => placement.Menu)
                .WithMany()
                .HasForeignKey(placement => placement.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuPlacement>()
                .HasOne(placement => placement.ParentMenu)
                .WithMany()
                .HasForeignKey(placement => placement.ParentMenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasKey(userRole => new { userRole.UserId, userRole.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>().HasIndex(user => new { user.TenantId, user.EmployeeCode })
                .IsUnique().HasFilter("[IsDeleted] = 0 AND [EmployeeCode] IS NOT NULL");
            modelBuilder.Entity<Users>().HasIndex(user => new { user.TenantId, user.CompanyId });
            modelBuilder.Entity<Users>().HasIndex(user => new { user.TenantId, user.BranchId });
            modelBuilder.Entity<Users>().HasIndex(user => new { user.TenantId, user.PositionId });
            modelBuilder.Entity<Company>().HasIndex(company => new { company.TenantId, company.ParentCompanyId });
            modelBuilder.Entity<Company>()
                .HasOne(company => company.Tenant)
                .WithMany()
                .HasForeignKey(company => company.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Company>()
                .HasOne(company => company.ParentCompany)
                .WithMany(company => company.ChildCompanies)
                .HasForeignKey(company => company.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Users>()
                .HasOne(user => user.Company)
                .WithMany()
                .HasForeignKey(user => user.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Users>()
                .HasOne(user => user.Tenant)
                .WithMany()
                .HasForeignKey(user => user.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Users>()
                .HasOne(user => user.Branch)
                .WithMany()
                .HasForeignKey(user => user.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>()
                .HasIndex(branch => new { branch.TenantId, branch.CompanyId, branch.Code })
                .IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<Branch>()
                .HasOne(branch => branch.Company)
                .WithMany()
                .HasForeignKey(branch => branch.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CompanyUserSettings>()
                .HasIndex(settings => new { settings.TenantId, settings.CompanyId })
                .IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<CompanyUserSettings>()
                .HasOne(settings => settings.Company)
                .WithMany()
                .HasForeignKey(settings => settings.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserPositionAssignment>()
                .HasIndex(assignment => new { assignment.TenantId, assignment.UserId, assignment.IsActive });
            modelBuilder.Entity<UserPositionAssignment>()
                .HasOne(assignment => assignment.User)
                .WithMany(user => user.PositionAssignments)
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserPositionAssignment>()
                .HasOne(assignment => assignment.Position)
                .WithMany()
                .HasForeignKey(assignment => assignment.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserInvitation>()
                .HasIndex(invitation => invitation.TokenHash)
                .IsUnique();
            modelBuilder.Entity<UserInvitation>()
                .HasIndex(invitation => new { invitation.TenantId, invitation.UserId, invitation.ExpiresAt });
            modelBuilder.Entity<UserInvitation>()
                .HasOne(invitation => invitation.User)
                .WithMany(user => user.Invitations)
                .HasForeignKey(invitation => invitation.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Users>()
                .HasOne(user => user.Position)
                .WithMany()
                .HasForeignKey(user => user.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRoleAssignment>()
                .HasIndex(assignment => new { assignment.TenantId, assignment.UserId, assignment.RoleId, assignment.IsActive });
            modelBuilder.Entity<UserRoleAssignment>()
                .HasIndex(assignment => new { assignment.TenantId, assignment.EffectiveFrom, assignment.EffectiveTo });
            modelBuilder.Entity<UserRoleAssignment>()
                .HasOne(assignment => assignment.User)
                .WithMany(user => user.UserRoleAssignments)
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRoleAssignment>()
                .HasOne(assignment => assignment.Role)
                .WithMany(role => role.UserRoleAssignments)
                .HasForeignKey(assignment => assignment.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRoleAssignment>()
                .HasOne(assignment => assignment.Company)
                .WithMany()
                .HasForeignKey(assignment => assignment.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRoleAssignment>()
                .HasIndex(assignment => new { assignment.TenantId, assignment.UserId, assignment.CompanyId, assignment.IsDefault })
                .IsUnique()
                .HasFilter("[IsDefault] = 1 AND [IsActive] = 1 AND [IsDeleted] = 0 AND [CompanyId] IS NOT NULL");
            modelBuilder.Entity<UserCompanyAccess>()
                .HasIndex(access => new { access.TenantId, access.UserId, access.CompanyId, access.EffectiveFrom })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
            modelBuilder.Entity<UserCompanyAccess>()
                .HasIndex(access => new { access.TenantId, access.UserId })
                .IsUnique()
                .HasFilter("[IsDefault] = 1 AND [IsActive] = 1 AND [IsDeleted] = 0");
            modelBuilder.Entity<UserCompanyAccess>()
                .HasOne(access => access.User)
                .WithMany(user => user.CompanyAccesses)
                .HasForeignKey(access => access.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserCompanyAccess>()
                .HasOne(access => access.Company)
                .WithMany(company => company.UserAccesses)
                .HasForeignKey(access => access.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserBranchAccess>()
                .HasIndex(access => new { access.TenantId, access.UserId, access.BranchId, access.EffectiveFrom })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
            modelBuilder.Entity<UserBranchAccess>()
                .HasIndex(access => new { access.TenantId, access.UserId })
                .IsUnique()
                .HasFilter("[IsDefault] = 1 AND [IsActive] = 1 AND [IsDeleted] = 0");
            modelBuilder.Entity<UserBranchAccess>()
                .HasOne(access => access.User)
                .WithMany(user => user.BranchAccesses)
                .HasForeignKey(access => access.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserBranchAccess>()
                .HasOne(access => access.Branch)
                .WithMany(branch => branch.UserAccesses)
                .HasForeignKey(access => access.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompanyModuleAccess>()
                .HasKey(access => new { access.CompanyId, access.ModuleId });

            modelBuilder.Entity<CompanyModuleAccess>()
                .HasOne(access => access.Company)
                .WithMany(company => company.ModuleAccess)
                .HasForeignKey(access => access.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanyModuleAccess>()
                .HasOne(access => access.Module)
                .WithMany()
                .HasForeignKey(access => access.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompanyModuleAccess>()
                .HasQueryFilter(access => !access.Company!.IsDeleted);

            // ✅ Tenant Filter (CLEAN VERSION)
            //modelBuilder.Entity<MenuItem>()
            //    .HasQueryFilter(m => m.TenantId == CurrentTenantId);          
        }

        // ⭐ AUTO TENANT ASSIGNMENT
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var ctx = _workflow.Context;
            var now = DateTime.UtcNow;

            // ✅ Tenant handling
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                        entry.Entity.TenantId = ctx.TenantId;
                }
            }

            // ✅ Audit + SoftDelete handling
            foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = ctx.ActorId ?? SystemDefaults.SystemUserId;
                }

                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = ctx.ActorId ?? SystemDefaults.SystemUserId;
                }

                else if (entry.State == EntityState.Deleted)
                {
                    // soft delete
                    entry.State = EntityState.Modified;

                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = ctx.ActorId ?? SystemDefaults.SystemUserId;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyGlobalFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.IsOwned())
                    continue;

                var clrType = entityType.ClrType;
                var parameter = Expression.Parameter(clrType, "e");
                Expression? body = null;

                // Soft Delete
                if (typeof(ISoftDelete).IsAssignableFrom(clrType))
                {
                    var prop = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                    var condition = Expression.Equal(prop, Expression.Constant(false));

                    body = condition;
                }

                // Tenant Filter
                if (typeof(ITenantEntity).IsAssignableFrom(clrType))
                {
                    var tenantProp = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                    var dbContext = Expression.Property(Expression.Constant(this, typeof(WorkflowDbContext)), nameof(CurrentTenantId));

                    var tenantCondition =
                        Expression.OrElse(
                            Expression.Equal(dbContext,
                                Expression.Constant(Guid.Empty)),
                            Expression.Equal(
                                tenantProp,
                                dbContext)
                        );

                    body = body == null
                        ? tenantCondition
                        : Expression.AndAlso(body, tenantCondition);
                }

                if (body != null)
                {
                    var lambda = Expression.Lambda(body, parameter);
                    modelBuilder.Entity(clrType).HasQueryFilter(lambda);
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}
