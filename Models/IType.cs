using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("IType")]
    public class IType
    {
        [Key]
        [MaxLength(50)]
        public string Incident_Code { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Incident_Type { get; set; }

        public ICollection<Incident>? Incidents { get; set; }
    }
}
