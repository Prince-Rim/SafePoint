using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("RejectedIncidents")]
    public class RejectedIncident
    {
        [Key]
        public int RejectedIncidentID { get; set; }

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

        public DateTime RejectionDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("Userid")]
        public User? User { get; set; }

        [ForeignKey("Area_Code")]
        public Area? Area { get; set; }

        [ForeignKey("Incident_Code")]
        public IType? IType { get; set; }

        public Guid? RejectorID { get; set; }
    }
}
