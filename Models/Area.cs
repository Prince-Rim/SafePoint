using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("Area")]
    public class Area
    {
        [Key]
        [MaxLength(255)]
        public string Area_Code { get; set; } = string.Empty;

        [Column("ALocation")]
        [MaxLength(200)]
        public string ALocation { get; set; } = string.Empty;

        public ICollection<Incident>? Incidents { get; set; }
        public ICollection<Moderator>? Moderators { get; set; }
    }
}
