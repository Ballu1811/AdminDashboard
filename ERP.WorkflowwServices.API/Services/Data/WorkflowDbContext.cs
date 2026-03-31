using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.Interfaces.Common;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.WorkflowContext;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Module> Modules { get; set; }

        #region Auth & Users
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<MenuRole> MenuRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ApplyGlobalFilters(modelBuilder);

            // ✅ Self reference safe
            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

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
                    entry.Entity.TenantId = CurrentTenantId;
                }
            }

            // ✅ Audit + SoftDelete handling
            foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = ctx.ActorId ?? SystemUser.Id;
                }

                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = ctx.ActorId ?? SystemUser.Id;
                }

                else if (entry.State == EntityState.Deleted)
                {
                    // soft delete
                    entry.State = EntityState.Modified;

                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = ctx.ActorId ?? SystemUser.Id;
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
                    var dbContext = Expression.Property(Expression.Constant(this, typeof(WorkflowDbContext)),nameof(CurrentTenantId));

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
