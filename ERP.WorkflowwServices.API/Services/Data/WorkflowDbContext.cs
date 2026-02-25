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
        public DbSet<MenuItem> MenuItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ApplyGlobalFilters(modelBuilder);

            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
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

                    var tenantCondition =
                        Expression.OrElse(
                            Expression.Equal(
                                Expression.Constant(CurrentTenantId),
                                Expression.Constant(Guid.Empty)),
                            Expression.Equal(
                                tenantProp,
                                Expression.Constant(CurrentTenantId))
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
    }
}
