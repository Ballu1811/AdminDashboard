using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ERP.WorkflowwServices.API.Common
{
    public interface IUserContext
    {
        Guid UserId { get; }
        Guid TenantId { get; }
        Guid? CompanyId { get; }
        string Username { get; }
        bool IsAuthenticated { get; }
        bool IsSuperAdmin { get; }
        bool IsTenantAdmin { get; }
        bool IsCompanyAdmin { get; }
        IReadOnlySet<string> PermissionKeys { get; }
        bool HasPermission(string permission);
    }
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _http;

        public UserContext(IHttpContextAccessor http)
        {
            _http = http;
        }

        public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        public bool IsSuperAdmin => _http.HttpContext?.User?.IsInRole("SuperAdmin") ?? false;
        public bool IsTenantAdmin => _http.HttpContext?.User?.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value.Replace(" ", string.Empty, StringComparison.Ordinal))
            .Any(role => string.Equals(role, "TenantAdmin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "TenantOwner", StringComparison.OrdinalIgnoreCase)) ?? false;
        public bool IsCompanyAdmin => _http.HttpContext?.User?.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value.Replace(" ", string.Empty, StringComparison.Ordinal))
            .Any(role => string.Equals(role, "CompanyAdmin", StringComparison.OrdinalIgnoreCase)) ?? false;

        public bool HasPermission(string permission) =>
            _http.HttpContext?.User?.Claims.Any(claim =>
                claim.Type == "permission" &&
                string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase)) ?? false;

        public IReadOnlySet<string> PermissionKeys => (_http.HttpContext?.User?.Claims
            .Where(claim => claim.Type == "permission")
            .Select(claim => claim.Value) ?? [])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public Guid? CompanyId
        {
            get
            {
                var value = _http.HttpContext?.User?.FindFirst("companyId")?.Value;
                return Guid.TryParse(value, out var companyId) ? companyId : null;
            }
        }

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
