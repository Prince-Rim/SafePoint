using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("Moderator")]
    public class Moderator
    {
        [Key]
        public Guid Modid { get; set; }

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
        [MaxLength(255)]
        public string Area_Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("MPassword")]
        public string Modpassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = "Moderator"; 

        [ForeignKey("Area_Code")]
        public Area? Area { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? SuspensionEndTime { get; set; }
    }
}
