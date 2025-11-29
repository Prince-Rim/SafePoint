using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Data;
using SafePoint_IRS.Models;
using SafePoint_IRS.DTOs;
using BCrypt.Net;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SafePoint_IRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModeratorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModeratorController(AppDbContext context)
        {
            _context = context;
        }

        private string? GetRequesterId()
        {
            return Request.Headers["X-Requester-Id"].FirstOrDefault();
        }


        private async Task<Moderator?> GetModerator()
        {
            var requesterId = GetRequesterId();
            if (string.IsNullOrEmpty(requesterId)) return null;

            return await _context.Moderators
                .Include(m => m.Area)
                .FirstOrDefaultAsync(m => m.Modid.ToString() == requesterId);
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserByModeratorDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data provided." });
                }

                var moderator = await GetModerator();
                if (moderator == null)
                {
                    return Unauthorized(new { error = "Only Moderator can create users." });
                }

                if (await _context.Admins.AnyAsync(a => a.Username == userDto.Username || a.Email == userDto.Email) ||
                    await _context.Moderators.AnyAsync(m => m.Username == userDto.Username || m.Email == userDto.Email) ||
                    await _context.Users.AnyAsync(u => u.Username == userDto.Username || u.Email == userDto.Email))
                {
                    return Conflict(new { error = "Username or email already in use." });
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

                var newUser = new User
                {
                    Userid = Guid.NewGuid(),
                    Username = userDto.Username,
                    Email = userDto.Email,
                    Contact = userDto.Contact,
                    Userpassword = hashedPassword,
                    IsActive = true,
                    UserRole = "User"
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User created successfully.", userId = newUser.Userid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating user.", details = ex.Message });
            }
        }


        [HttpGet("my-incidents")]
        public async Task<IActionResult> GetMyAreaIncidents()
        {
            try
            {
                var moderator = await GetModerator();
                if (moderator == null)
                {
                    return Unauthorized(new { error = "Moderator not found." });
                }

                var incidents = await _context.Incident
                    .Include(i => i.User)
                    .Include(i => i.ValidStatus)
                    .Include(i => i.IType)
                    .Where(i => i.Area_Code == moderator.Area_Code)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.IncidentID,
                        i.Title,
                        i.Incident_Code,
                        i.Severity,
                        i.IncidentDateTime,
                        i.Descr,
                        i.Latitude,
                        i.Longitude,
                        i.CreatedAt,
                        User = i.User != null ? new { i.User.Username, i.User.Email } : null,
                        IncidentType = i.IType != null ? i.IType.Incident_Type : null,
                        ValidationStatus = i.ValidStatus != null ? i.ValidStatus.Validation_Status : (bool?)null,
                        ValidationDate = i.ValidStatus != null ? i.ValidStatus.Validation_Date : null,
                        i.OtherHazard
                    })
                    .ToListAsync();

                return Ok(new
                {
                    moderatorArea = moderator.Area?.ALocation ?? moderator.Area_Code,
                    areaCode = moderator.Area_Code,
                    incidents = incidents,
                    totalCount = incidents.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching incidents.", details = ex.Message });
            }
        }

        [HttpGet("pending-incidents")]
        public async Task<IActionResult> GetPendingIncidents()
        {
            try
            {
                var moderator = await GetModerator();
                if (moderator == null)
                {
                    return Unauthorized(new { error = "Moderator not found." });
                }

                var pendingIncidents = await _context.Incident
                    .Include(i => i.User)
                    .Include(i => i.ValidStatus)
                    .Include(i => i.IType)
                    .Where(i => i.Area_Code == moderator.Area_Code &&
                                 i.ValidStatus != null &&
                                 i.ValidStatus.Validation_Status == false)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.IncidentID,
                        i.Title,
                        i.Incident_Code,
                        i.Severity,
                        i.IncidentDateTime,
                        i.Descr,
                        i.Latitude,
                        i.Longitude,
                        i.CreatedAt,
                        User = i.User != null ? new { i.User.Username, i.User.Email } : null,
                        IncidentType = i.IType != null ? i.IType.Incident_Type : null,
                        i.OtherHazard
                    })
                    .ToListAsync();

                return Ok(new
                {
                    moderatorArea = moderator.Area?.ALocation ?? moderator.Area_Code,
                    areaCode = moderator.Area_Code,
                    pendingIncidents = pendingIncidents,
                    totalCount = pendingIncidents.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching pending incidents.", details = ex.Message });
            }
        }

        [HttpGet("incident/{id}")]
        public async Task<IActionResult> GetIncidentById(int id)
        {
            var moderator = await GetModerator();
            if (moderator == null)
            {
                return Unauthorized(new { error = "Moderator not found." });
            }

            var incident = await _context.Incident
                .Include(i => i.IType)
                .Include(i => i.ValidStatus)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.IncidentID == id);

            if (incident == null)
                return StatusCode(404, new { error = "Incident not found." });

            if (incident.Area_Code != moderator.Area_Code)
            {
                return StatusCode(403, new { error = "Access denied. Incident is outside your assigned area." });
            }

            return Ok(incident);
        }

        [HttpPut("incident/{id}")]
        public async Task<IActionResult> UpdateIncident(int id, [FromBody] UpdateIncidentByModeratorDto report)
        {
            var moderator = await GetModerator();
            if (moderator == null)
            {
                return Unauthorized(new { error = "Moderator not found." });
            }

            var incident = await _context.Incident.FindAsync(id);
            if (incident == null)
            {
                return StatusCode(404, new { message = "Incident not found." });
            }

            if (incident.Area_Code != moderator.Area_Code)
            {
                return StatusCode(403, new { error = "Access denied. Incident is outside your assigned area." });
            }

            if (!string.IsNullOrEmpty(report.Title)) incident.Title = report.Title;
            if (!string.IsNullOrEmpty(report.Incident_Code)) incident.Incident_Code = report.Incident_Code;
            if (!string.IsNullOrEmpty(report.Severity)) incident.Severity = report.Severity;
            if (!string.IsNullOrEmpty(report.Descr)) incident.Descr = report.Descr;

            if (!string.IsNullOrEmpty(report.Incident_Code) && report.Incident_Code.Equals("other", StringComparison.OrdinalIgnoreCase))
            {
                incident.OtherHazard = report.OtherHazard ?? string.Empty;
            }
            else
            {
                incident.OtherHazard = string.Empty;
            }

            if (report.Latitude.HasValue && report.Latitude.Value != 0) incident.Latitude = report.Latitude.Value;
            if (report.Longitude.HasValue && report.Longitude.Value != 0) incident.Longitude = report.Longitude.Value;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Incident updated successfully.", incidentId = incident.IncidentID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating incident.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("incident/{id}")]
        public async Task<IActionResult> DeleteIncident(int id)
        {
            var moderator = await GetModerator();
            if (moderator == null)
            {
                return Unauthorized(new { error = "Moderator not found." });
            }

            var incident = await _context.Incident.FindAsync(id);
            if (incident == null)
            {
                return StatusCode(404, new { message = "Incident not found." });
            }

            if (incident.Area_Code != moderator.Area_Code)
            {
                return StatusCode(403, new { error = "Access denied. Incident is outside your assigned area." });
            }

            var validationRecord = await _context.Valid.FirstOrDefaultAsync(v => v.IncidentID == id);

            if (validationRecord == null)
            {
                return StatusCode(404, new { error = "Validation record not found for this incident. Cannot reject." });
            }

            validationRecord.Validation_Status = false; 
            validationRecord.Validation_Date = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Incident ID {id} has been marked as rejected/deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error marking incident as deleted.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}