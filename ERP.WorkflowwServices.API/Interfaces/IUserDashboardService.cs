using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces;

public interface IUserDashboardService
{
    Task<UserDashboardResponse> GetDashboardAsync(UserDashboardFilter filter, CancellationToken cancellationToken);
}
