using System;

namespace SafePoint_IRS.DTOs
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string? AreaCode { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
