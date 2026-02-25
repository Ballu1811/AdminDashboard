using ERP.WorkflowwServices.API.Interfaces.Common;

namespace ERP.WorkflowwServices.API.Models
{
    public abstract class BaseAuditableEntity : ISoftDelete
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }
    }
}
