using ERP.WorkflowwServices.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services.Data
{
    public class WorkflowDbContext:DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
        {
        }

       public DbSet<WFEvent> WFEvents { get; set; }
    }
}
