using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("UserArchive")]
    public class UserArchive
    {
        [Key]
        public int ArchiveId { get; set; }

        public Guid Userid { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Contact { get; set; }

        [Required]
        public string Userpassword { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = "User";

        public DateTime? SuspensionEndTime { get; set; }

        public DateTime DeletionDate { get; set; } = DateTime.UtcNow;
    }
}
