using System;

namespace SafePoint_IRS.DTOs
{
    public class CreateModeratorDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Area_Code { get; set; } = string.Empty;
        public string UserRole { get; set; } = "Moderator";
    }

    public class UpdateModeratorDto : UpdateUserDto
    {
        public string? Area_Code { get; set; }
    }

    public class CreateUserByModeratorDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
