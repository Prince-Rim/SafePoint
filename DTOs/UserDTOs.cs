using System;

namespace SafePoint_IRS.DTOs
{
    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserRole { get; set; } = "User";
    }

    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? FirstName { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? Password { get; set; } 
        public bool? IsActive { get; set; }
        public DateTime? SuspensionEndTime { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
    }

    public class ChangeRoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string? Area_Code { get; set; } // For Moderator
        public string? Permissions { get; set; } // For Admin, comma-separated
        
        // Fields to update during transfer
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? SuspensionEndTime { get; set; }
        public string? Password { get; set; } // Optional: New password if provided
    }
}
