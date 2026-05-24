using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IUserLayoutSettingsService
    {
        Task<UserLayoutSettingsDto> GetEffectiveAsync(Guid userId, Guid tenantId);
        Task<UserLayoutSettingsDto?> GetAsync(Guid userId, Guid tenantId);
        Task<UserLayoutSettingsDto> SaveAsync(UserLayoutSettingsDto dto);
        Task ResetToTenantAsync(Guid userId, Guid tenantId);
    }
}
