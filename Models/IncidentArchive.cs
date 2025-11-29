using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("IncidentArchive")]
    public class IncidentArchive
    {
        [Key]
        public int ArchiveId { get; set; }

        public int OriginalIncidentID { get; set; }

        [Required]
        public Guid Userid { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Incident_Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OtherHazard { get; set; }

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        [Required]
        public DateTime IncidentDateTime { get; set; }

        [Required]
        [MaxLength(255)]
        public string Area_Code { get; set; } = string.Empty;

        [Required]
        public string Descr { get; set; } = string.Empty;

        public byte[]? Img { get; set; }

        public DateTime? CreatedAt { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [MaxLength(500)]
        public string? LocationAddress { get; set; }

        public DateTime DeletionDate { get; set; } = DateTime.UtcNow;
    }
}
