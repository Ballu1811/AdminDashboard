using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        protected readonly WorkflowDbContext _context;
        public RefreshTokenRepository(WorkflowDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        }

        public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId)
        {
            return await _context.RefreshTokens.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);           
        }

        public void Update(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
        }

        public IQueryable<RefreshToken> Query()
        {
            return _context.RefreshTokens.AsQueryable();
        }
    }
}
