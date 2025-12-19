using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SafePoint_IRS.Models
{
    [Table("UserBadge")]
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string BadgeName { get; set; } = string.Empty;

        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? AwardedBy { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore] 
        public User? User { get; set; }
    }
}
