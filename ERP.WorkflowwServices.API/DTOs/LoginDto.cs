using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.DTOs
{
    public class LoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public string? RefreshToken { get; set; }

        public DateTime Expiry { get; set; }

        public Guid UserId { get; set; }
        public required string Username { get; set; }

        public required string Role { get; set; }

        public List<string>? Permissions { get; set; }
    } 
}
