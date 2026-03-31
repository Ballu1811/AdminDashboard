using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IUserRepository
    {
        Task<Users?> GetByUsernameAsync(string username);
    }
}
