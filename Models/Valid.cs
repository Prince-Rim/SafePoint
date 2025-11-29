using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("Valid")]
    public class Valid
    {
        [Key]
        [MaxLength(50)]
        public string Validation_ID { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int IncidentID { get; set; }

        [Required]
        public bool Validation_Status { get; set; } 

        public DateTime? Validation_Date { get; set; } = DateTime.UtcNow;

        [ForeignKey("IncidentID")]
        public Incident? Incident { get; set; }

        public Guid? ValidatorID { get; set; }
    }
}
