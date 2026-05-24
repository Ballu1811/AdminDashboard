using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ERP.WorkflowwServices.API.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Users user, List<string> permissions)
        {
            var claims = new List<Claim>
            {
                /* 🔥 CORE IDENTIFIERS */
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // userId
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // standard .NET

                new Claim("tenantId", user.TenantId.ToString()),

                /* BASIC INFO */
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username ?? ""),
                new Claim(ClaimTypes.Name, user.Username ?? ""),

                new Claim(ClaimTypes.Role, user.Role?.Name ?? ""),

                /* SECURITY */
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            /* 🔥 PERMISSIONS */
            if (permissions != null && permissions.Any())
            {
                claims.AddRange(permissions.Select(p => new Claim("permission", p)));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"]);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
