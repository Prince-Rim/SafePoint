using System;

namespace SafePoint_IRS.DTOs
{
    public class CreateAdminDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Permissions { get; set; }
    }

    public class UpdateAdminDto : UpdateUserDto
    {
        
    }
}
