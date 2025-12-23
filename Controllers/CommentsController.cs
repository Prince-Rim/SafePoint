using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Data;
using SafePoint_IRS.Models;
using SafePoint_IRS.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SafePoint_IRS.Hubs;

namespace SafePoint_IRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CommentsController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("incident/{incidentId}")]
        public async Task<IActionResult> GetCommentsForIncident(int incidentId)
        {
            var comments = await _context.Comments
                .Where(c => c.IncidentID == incidentId)
                .Include(c => c.User)
                    .ThenInclude(u => u.Badges)
                .Include(c => c.Admin)
                .Include(c => c.Moderator)
                .OrderByDescending(c => c.dttm)
                .ToListAsync();

            var response = comments.Select(c =>
            {
                var userDto = new UserDto();
                string userIdStr = "";

                if (c.User != null)
                {
                    userDto.Username = c.User.Username;
                    userDto.UserRole = c.User.UserRole;
                    userDto.Badges = c.User.Badges?.Select(b => new { b.BadgeName }).ToList();
                    userIdStr = c.Userid.ToString();
                }
                else if (c.Admin != null)
                {
                    userDto.Username = c.Admin.Username;
                    userDto.UserRole = c.Admin.UserRole;
                    userIdStr = c.AdminId.ToString();
                }
                else if (c.Moderator != null)
                {
                    userDto.Username = c.Moderator.Username;
                    userDto.UserRole = c.Moderator.UserRole;
                    userIdStr = c.ModId.ToString();
                }
                else
                {
                    userDto.Username = "Unknown";
                    userDto.UserRole = "Guest";
                }

                return new CommentResponseDto
                {
                    Comment_ID = c.Comment_ID,
                    IncidentID = c.IncidentID,
                    Comment = c.comment,
                    Dttm = c.dttm,
                    Userid = userIdStr,
                    User = userDto
                };
            });

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] CommentDto request)
        {
            Guid requesterId = Guid.Empty;
            if (Request.Headers.TryGetValue("X-Requester-Id", out var userIdHeader) && Guid.TryParse(userIdHeader, out var parsedId))
            {
                requesterId = parsedId;
            }
            else
            {
                return Unauthorized("User ID header missing or invalid.");
            }

            var newComment = new Comment
            {
                IncidentID = request.IncidentId,
                comment = request.CommentText,
                dttm = DateTime.UtcNow
            };


            var user = await _context.Users.FindAsync(requesterId);
            if (user != null)
            {
                newComment.Userid = requesterId;
            }
            else
            {
                var admin = await _context.Admins.FindAsync(requesterId);
                if (admin != null)
                {
                    newComment.AdminId = requesterId;
                }
                else
                {
                    var mod = await _context.Moderators.FindAsync(requesterId);
                    if (mod != null)
                    {
                        newComment.ModId = requesterId;
                    }
                    else
                    {
                        return Unauthorized("User not found.");
                    }
                }
            }

            try
            {
                _context.Comments.Add(newComment);
                await _context.SaveChangesAsync();


                if (requesterId != Guid.Empty && newComment.Userid.HasValue && newComment.Userid.Value != Guid.Empty)
                {
                    int commentCount = await _context.Comments.CountAsync(c => c.Userid == newComment.Userid);
                    if (commentCount >= 20)
                    {
                        string badgeName = "Sociable";
                        bool hasBadge = await _context.UserBadges.AnyAsync(b => b.UserId == newComment.Userid.Value && b.BadgeName == badgeName);
                        if (!hasBadge)
                        {
                             var autoBadge = new UserBadge
                             {
                                 UserId = newComment.Userid.Value,
                                 BadgeName = badgeName,
                                 AwardedBy = "System",
                                 AwardedAt = DateTime.UtcNow
                             };
                             _context.UserBadges.Add(autoBadge);
                             await _context.SaveChangesAsync();


                             await _hubContext.Clients.All.SendAsync("ReceiveBadgeNotification", newComment.Userid.Value.ToString(), badgeName);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message} - {ex.InnerException?.Message}");
            }

            return CreatedAtAction(nameof(GetCommentsForIncident), new { incidentId = newComment.IncidentID }, newComment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutComment(int id, [FromBody] CommentDto request)
        {
            Guid requesterId = Guid.Empty;
            if (Request.Headers.TryGetValue("X-Requester-Id", out var userIdHeader) && Guid.TryParse(userIdHeader, out var parsedId))
            {
                requesterId = parsedId;
            }
            else
            {
                return Unauthorized("User ID header missing or invalid.");
            }

            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }


            bool isAuthorized = false;
            if (comment.Userid == requesterId) isAuthorized = true;
            else if (comment.AdminId == requesterId) isAuthorized = true;
            else if (comment.ModId == requesterId) isAuthorized = true;

            if (!isAuthorized)
            {
                return Forbid("You are not authorized to edit this comment.");
            }

            comment.comment = request.CommentText;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            Guid requesterId = Guid.Empty;
            if (Request.Headers.TryGetValue("X-Requester-Id", out var userIdHeader) && Guid.TryParse(userIdHeader, out var parsedId))
            {
                requesterId = parsedId;
            }
            else
            {
                return Unauthorized("User ID header missing or invalid.");
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }


            bool isAuthorized = false;
            if (comment.Userid == requesterId) isAuthorized = true;
            else if (comment.AdminId == requesterId) isAuthorized = true;
            else if (comment.ModId == requesterId) isAuthorized = true;

            if (!isAuthorized)
            {
                return Forbid("You are not authorized to delete this comment.");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.Comment_ID == id);
        }
    }
}