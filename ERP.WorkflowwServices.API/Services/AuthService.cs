using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services
{
    public class AuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;

        public AuthService(IUnitOfWork uow, IJwtService jwt, IConfiguration config)
        {
            _uow = uow;
            _jwt = jwt;
            _config = config;
        }

        // =========================
        // LOGIN
        // =========================
        public async Task<AuthResponseDto> Login(LoginDto dto)
        {
            var user = await _uow.Users.Query().Include(x => x.Role).ThenInclude(r => r.MenuRoles).ThenInclude(m => m.Menu)
                .FirstOrDefaultAsync(x => x.Username == dto.Username);

            if (user == null)
                throw new Exception("Invalid username");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Invalid password");

            // 🔥 Get permissions from MenuRole
            var permissions = user.Role.MenuRoles
                .Where(x => x.CanView)
                .Select(x => x.Menu.Permission)
                .Where(p => p != null)
                .ToList();

            var token = _jwt.GenerateToken(user, permissions);

            var refresh = _jwt.GenerateRefreshToken();

            // 🔥 Save refresh token
            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = refresh,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // 🔥 Update user
            user.LastLoginAt = DateTime.UtcNow;
            _uow.Users.Update(user);

            // ✅ SINGLE COMMIT
            await _uow.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refresh,
                Expiry = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:AccessTokenExpiryMinutes"])
                ),
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.Name,
                Permissions = permissions
            };
        }

        // =========================
        // REFRESH TOKEN
        // =========================
        public async Task<AuthResponseDto> RefreshTokenAsync(string token, HttpContext context)
        {
            var existingToken = await _uow.RefreshTokens.Query().FirstOrDefaultAsync(x => x.Token == token);

            if (existingToken == null)
                throw new Exception("Invalid refresh token");

            // 🚨 Reuse attack detection
            if (existingToken.IsRevoked && existingToken.ReplacedByToken != null)
            {
                await RevokeAllUserTokens(existingToken.UserId);
                throw new Exception("Token reuse detected!");
            }

            if (existingToken.IsRevoked)
                throw new Exception("Token already revoked");

            if (existingToken.Expires < DateTime.UtcNow)
                throw new Exception("Token expired");

            var user = await _uow.Users.Query().Include(x => x.Role).ThenInclude(r => r.MenuRoles).ThenInclude(m => m.Menu)
                .FirstOrDefaultAsync(x => x.Id == existingToken.UserId);

            if (user == null)
                throw new Exception("User not found");

            // 🔥 Revoke old token
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReasonRevoked = "Replaced by new token";
            existingToken.RevokedByIp = context.Connection.RemoteIpAddress?.ToString();

            // 🔄 New refresh token
            var newRefreshTokenValue = _jwt.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenValue,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedByIp = context.Connection.RemoteIpAddress?.ToString()
            };

            // 🔗 Link old → new
            existingToken.ReplacedByToken = newRefreshTokenValue;

            _uow.RefreshTokens.Update(existingToken);
            await _uow.RefreshTokens.AddAsync(newRefreshToken);

            // 🔑 Permissions
            var permissions = user.Role.MenuRoles
                .Where(x => x.CanView)
                .Select(x => x.Menu.Permission!)
                .ToList();

            var newAccessToken = _jwt.GenerateToken(user, permissions);

            // ✅ IMPORTANT
            await _uow.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshTokenValue,
                Expiry = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!)
                ),
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.Name,
                Permissions = permissions
            };
        }

        // =========================
        // LOGOUT
        // =========================
        public async Task LogoutAsync(string refreshToken)
        {
            var token = await _uow.RefreshTokens.Query().FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (token == null)
                return;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = "User logout";

            _uow.RefreshTokens.Update(token);
            await _uow.SaveChangesAsync();
        }

        // =========================
        // REVOKE ALL TOKENS
        // =========================
        public async Task RevokeAllUserTokens(Guid userId)
        {
            var tokens = await _uow.RefreshTokens.Query().Where(x => x.UserId == userId).ToListAsync();

            foreach (var token in tokens)
            {
                if (!token.IsRevoked)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.ReasonRevoked = "Security breach - token reuse";
                    _uow.RefreshTokens.Update(token);
                }
            }
            await _uow.SaveChangesAsync();
        }
    }
}
