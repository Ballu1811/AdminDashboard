using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WorkflowwServices.API.Models
{
    [Table("tblCompanyModuleAccess")]
    [Index(nameof(ModuleId))]
    public sealed class CompanyModuleAccess
    {
        public Guid CompanyId { get; set; }
        public Guid ModuleId { get; set; }
        public bool IsEnabled { get; set; } = true;

        [ForeignKey(nameof(CompanyId))]
        public Company? Company { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public Module? Module { get; set; }
    }
}
