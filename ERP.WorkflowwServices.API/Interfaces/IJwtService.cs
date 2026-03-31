using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Users user, List<string> permissions);
        string GenerateRefreshToken();
    }
}
