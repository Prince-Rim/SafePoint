using System;

namespace SafePoint_IRS.DTOs
{
    public class CommentDto
    {
        public int IncidentId { get; set; }
        public required string CommentText { get; set; }
    }

    public class CommentResponseDto
    {
        public int Comment_ID { get; set; }
        public int IncidentID { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Dttm { get; set; }
        public string? Userid { get; set; }
        public UserDto? User { get; set; }
    }
}
