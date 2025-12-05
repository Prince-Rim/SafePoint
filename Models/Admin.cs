using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("Admin")]
    public class Admin
    {
        [Key]
        public Guid Adminid { get; set; }

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
        [Column("APassword")]
        public string Adminpassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = "Admin"; 

        [Required]
        public bool IsActive { get; set; } = true;

        public bool IsSuperAdmin { get; set; } = false;

        public string? Permissions { get; set; }

        public DateTime? SuspensionEndTime { get; set; }
    }
}



