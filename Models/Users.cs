using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid Userid { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(11)]
        public string Contact { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Userpassword { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? SuspensionEndTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = "User"; 

        public int TrustScore { get; set; } = 0;

        public ICollection<UserBadge>? Badges { get; set; }

        public ICollection<Incident>? Incidents { get; set; }
        public ICollection<Comment>? Comments { get; set; }
    }
}
