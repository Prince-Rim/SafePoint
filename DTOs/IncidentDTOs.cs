using Microsoft.AspNetCore.Http;
using System;

namespace SafePoint_IRS.DTOs
{
    public class IncidentReportDto
    {
        public Guid Userid { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Incident_Code { get; set; } = string.Empty;
        public string? OtherHazard { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string IncidentDateTime { get; set; } = string.Empty; 
        public string Area_Code { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Descr { get; set; } = string.Empty;
        public IFormFile? Img { get; set; }
    }

    public class UpdateIncidentByModeratorDto
    {
        public string? Title { get; set; }
        public string? Incident_Code { get; set; }
        public string? OtherHazard { get; set; }
        public string? Severity { get; set; }
        public string? Descr { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
