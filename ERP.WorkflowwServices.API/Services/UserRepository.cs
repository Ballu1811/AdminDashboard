using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class UserRepository:IUserRepository
    {
        protected readonly WorkflowDbContext _context;
        public UserRepository(WorkflowDbContext context)
        {
            _context = context;
        }
        public async Task<Users?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(x => x.Role)
                .ThenInclude(r => r.MenuRoles)
                .ThenInclude(m => m.Menu)
                .FirstOrDefaultAsync(x => x.Username == username);
        }
    }
}
