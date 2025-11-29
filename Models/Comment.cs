using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafePoint_IRS.Models
{
    [Table("Comment")]
    public class Comment
    {
        [Key]
        [Column(TypeName = "numeric(18,0)")]
        public int Comment_ID { get; set; }

        [Required]
        public int IncidentID { get; set; }

        public Guid? Userid { get; set; }

        public Guid? AdminId { get; set; }

        public Guid? ModId { get; set; }

        [Required]
        public string comment { get; set; } = string.Empty;

        public DateTime dttm { get; set; }

        [ForeignKey("IncidentID")]
        public Incident? Incident { get; set; }

        [ForeignKey("Userid")]
        public User? User { get; set; }

        [ForeignKey("AdminId")]
        public Admin? Admin { get; set; }

        [ForeignKey("ModId")]
        public Moderator? Moderator { get; set; }
    }
}
