using System;

using System.ComponentModel.DataAnnotations;

namespace SafePoint_IRS.DTOs
{
    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must be 11 digits and start with '09'.")]
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

        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must be 11 digits and start with '09'.")]
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
        public object? Badges { get; set; } // Added Badges
    }

    public class ChangeRoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string? Area_Code { get; set; }
        public string? Permissions { get; set; }
        

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? SuspensionEndTime { get; set; }
        public string? Password { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public Guid UserId { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        
        [EmailAddress]
        public string? Email { get; set; }
    }

    public class AddBadgeDto
    {
        public Guid UserId { get; set; }
        public string BadgeName { get; set; } = string.Empty;
    }
}
