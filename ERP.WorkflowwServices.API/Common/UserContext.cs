using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ERP.WorkflowwServices.API.Common
{
    public interface IUserContext
    {
        Guid UserId { get; }
        Guid TenantId { get; }
        string Username { get; }
        bool IsAuthenticated { get; }
    }
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _http;

        public UserContext(IHttpContextAccessor http)
        {
            _http = http;
        }

        public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        public Guid UserId
        {
            get            
            {
                var user = _http.HttpContext?.User;

                // 🔥 not logged in
                if (user?.Identity?.IsAuthenticated != true)
                    return SystemDefaults.SystemUserId;

                var value = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return Guid.TryParse(value, out var id) ? id : SystemDefaults.SystemUserId;             
            }
        }

        public Guid TenantId => Guid.TryParse(_http.HttpContext?.User?.FindFirst("tenantId")?.Value, out var tid)
                ? tid : SystemDefaults.DefaultTenantId;

        public string Username => _http.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
    }
}
