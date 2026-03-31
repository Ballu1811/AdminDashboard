using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<List<RefreshToken>> GetByUserIdAsync(Guid userId);

        Task AddAsync(RefreshToken token);
        void Update(RefreshToken token); 
        IQueryable<RefreshToken> Query();
    }
}
