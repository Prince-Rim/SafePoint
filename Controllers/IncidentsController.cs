using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Data;
using SafePoint_IRS.Models;
using SafePoint_IRS.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

using Microsoft.AspNetCore.SignalR;
using SafePoint_IRS.Hubs;

namespace SafePoint_IRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IncidentsController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        [EnableRateLimiting("Fixed")]
        public async Task<IActionResult> PostIncident([FromForm] IncidentReportDto report)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                      .SelectMany(v => v.Errors)
                      .Select(e => e.ErrorMessage)
                      .ToList();
                    return BadRequest(new { message = "Invalid input", errors });
                }

                if (!DateTime.TryParseExact(report.IncidentDateTime, 
                    new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime incidentDateTime))
                {
                    return BadRequest(new { message = "Invalid date format. Expected format: yyyy-MM-dd HH:mm:ss" });
                }

                var user = await _context.Users.FindAsync(report.Userid);
                if (user == null) return BadRequest(new { message = "User not found." });

                var requestedAreaCode = string.IsNullOrWhiteSpace(report.Area_Code) ? "DEFAULT" : report.Area_Code;
                var area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == requestedAreaCode);

                if (area == null)
                {
                    var defaultAreaCode = "DEFAULT";
                    area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == defaultAreaCode);

                    if (area == null)
                    {
                        try
                        {
                            area = new Area
                            {
                                Area_Code = defaultAreaCode,
                                ALocation = "Incident Reported"
                            };
                            _context.Area.Add(area);
                            await _context.SaveChangesAsync();
                        }
                        catch (DbUpdateException dbEx)
                        {
                            if (dbEx.InnerException?.Message?.Contains("duplicate key") == true || 
                                dbEx.InnerException?.Message?.Contains("PRIMARY KEY") == true)
                            {
                                _context.ChangeTracker.Entries<Area>()
                                    .Where(e => e.Entity.Area_Code == defaultAreaCode)
                                    .ToList()
                                    .ForEach(e => e.State = Microsoft.EntityFrameworkCore.EntityState.Detached);
                                
                                area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == defaultAreaCode);
                            }
                            
                            if (area == null)
                            {
                                return StatusCode(500, new { message = "Failed to create or retrieve area.", details = dbEx.InnerException?.Message ?? dbEx.Message });
                            }
                        }
                    }
                }

                var incidentType = await _context.IncidentTypes.FirstOrDefaultAsync(t => t.Incident_Code == report.Incident_Code);
                if (incidentType == null)
                {
                    try
                    {
                        incidentType = new IType
                        {
                            Incident_Code = report.Incident_Code,
                            Incident_Type = report.Incident_Code 
                        };
                        _context.IncidentTypes.Add(incidentType);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        if (dbEx.InnerException?.Message?.Contains("duplicate key") == true || 
                            dbEx.InnerException?.Message?.Contains("PRIMARY KEY") == true)
                        {
                            _context.ChangeTracker.Entries<IType>()
                                .Where(e => e.Entity.Incident_Code == report.Incident_Code)
                                .ToList()
                                .ForEach(e => e.State = Microsoft.EntityFrameworkCore.EntityState.Detached);
                            
                            incidentType = await _context.IncidentTypes.FirstOrDefaultAsync(t => t.Incident_Code == report.Incident_Code);
                        }
                        
                        if (incidentType == null)
                        {
                            return StatusCode(500, new { message = "Failed to create or retrieve incident type.", details = dbEx.InnerException?.Message ?? dbEx.Message });
                        }
                    }
                }

                byte[]? imageBytes = null;
                if (report.Img != null)
                {
                    using var ms = new MemoryStream();
                    await report.Img.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }

                var newIncident = new Incident
                {
                    Userid = report.Userid,
                    Title = report.Title,
                    Incident_Code = report.Incident_Code,
                    OtherHazard = report.OtherHazard ?? string.Empty,
                    Severity = report.Severity,
                    IncidentDateTime = incidentDateTime,
                    Area_Code = area.Area_Code,
                    Descr = report.Descr,
                    Img = imageBytes,
                    Latitude = report.Latitude,
                    Longitude = report.Longitude,
                    LocationAddress = report.LocationAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Incident.Add(newIncident);
                await _context.SaveChangesAsync();

                var initialValidation = new Valid
                {
                    IncidentID = newIncident.IncidentID,
                    Validation_Status = false,
                    Validation_Date = null
                };

                _context.Valid.Add(initialValidation);
                await _context.SaveChangesAsync();

                var assignedModerators = await _context.Moderators
                    .Where(m => m.Area_Code == newIncident.Area_Code)
                    .Select(m => new { m.Modid, m.Username, m.Email })
                    .ToListAsync();
                
                return CreatedAtAction(nameof(GetIncidentById), new { id = newIncident.IncidentID }, new
                {
                    incident = newIncident,
                    assignedModerators = assignedModerators,
                    message = assignedModerators.Any() 
                        ? $"Incident created and assigned to {assignedModerators.Count} moderator(s) in {area.ALocation}"
                        : $"Incident created in {area.ALocation} (No moderators assigned to this area)"
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                return StatusCode(500, new { message = "Failed to submit incident.", details = innerException });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to submit incident.", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAcceptedIncidents([FromQuery] string? type, [FromQuery] string? severity)
        {
            var query = _context.Incident
                .Include(i => i.IType)
                .Include(i => i.ValidStatus)
                .Include(i => i.User)
                .Include(i => i.Area)
                .Where(i => i.ValidStatus != null && i.ValidStatus.Validation_Status == true);


            if (!string.IsNullOrEmpty(type))
            {
                if (type.Equals("accident", StringComparison.OrdinalIgnoreCase) || type.Equals("road", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.IType != null && (i.IType.Incident_Code == "accident" || i.IType.Incident_Code == "road"));
                }
                else
                {
                    query = query.Where(i => i.IType != null && i.IType.Incident_Code == type);
                }
            }

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(i => i.Severity == severity);

            var incidents = await query
                .OrderByDescending(i => i.IncidentDateTime)
                .Select(i => new 
                {
                    i.IncidentID,
                    i.Userid,
                    i.Title,
                    i.Incident_Code,
                    i.Severity,
                    i.Descr,
                    i.Latitude,
                    i.Longitude,
                    i.IncidentDateTime,
                    i.LocationAddress,
                    i.CreatedAt,
                    User = i.User != null ? new { i.User.Username, i.User.Email } : null,
                    IncidentType = i.IType != null ? i.IType.Incident_Type : null,
                    Area = i.Area != null ? new { i.Area.ALocation, i.Area.Area_Code } : null,
                    i.Img,
                    i.OtherHazard,
                    ValidStatus = i.ValidStatus != null ? new { i.ValidStatus.Validation_Status, i.ValidStatus.Validation_Date } : null
                })
                .ToListAsync();

            return Ok(incidents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIncidentById(int id)
        {
            var incident = await _context.Incident
                .Include(i => i.IType)
                .Include(i => i.ValidStatus)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.IncidentID == id);

            if (incident == null)
                return NotFound(new { error = "Incident not found." });

            return Ok(incident);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutIncident(int id, [FromForm] IncidentReportDto report)
        {
            var incident = await _context.Incident.FindAsync(id);
            if (incident == null)
            {
                return NotFound(new { message = "Incident not found." });
            }

            incident.Title = report.Title;
            incident.Incident_Code = report.Incident_Code;
            incident.Severity = report.Severity;
            incident.Descr = report.Descr;

            if (report.Incident_Code.Equals("other", StringComparison.OrdinalIgnoreCase))
            {
                incident.OtherHazard = report.OtherHazard ?? string.Empty;
            }
            else
            {
                incident.OtherHazard = string.Empty;
            }

            if (report.Latitude != 0 && report.Longitude != 0)
            {
                incident.Latitude = report.Latitude;
                incident.Longitude = report.Longitude;
            }


            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Incident updated successfully.", incident });
            }
            catch (DbUpdateException ex)
            {
                var innerEx = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Error updating incident (DB constraint failure).", details = innerEx });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating incident.", details = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIncident(int id)
        {
            var incident = await _context.Incident.FindAsync(id);
            if (incident == null)
            {
                return NotFound(new { message = "Incident not found." });
            }


            var incidentArchive = new IncidentArchive
            {
                OriginalIncidentID = incident.IncidentID,
                Userid = incident.Userid,
                Title = incident.Title,
                Incident_Code = incident.Incident_Code,
                OtherHazard = incident.OtherHazard,
                Severity = incident.Severity,
                IncidentDateTime = incident.IncidentDateTime,
                Area_Code = incident.Area_Code,
                Descr = incident.Descr,
                Img = incident.Img,
                CreatedAt = incident.CreatedAt,
                Latitude = incident.Latitude,
                Longitude = incident.Longitude,
                LocationAddress = incident.LocationAddress,
                DeletionDate = DateTime.UtcNow
            };
            _context.IncidentArchives.Add(incidentArchive);

            _context.Incident.Remove(incident);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Incident deleted and archived successfully." });
        }
    }
}
