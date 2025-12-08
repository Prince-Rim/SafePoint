using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Data;
using SafePoint_IRS.Models;
using SafePoint_IRS.DTOs;
using BCrypt.Net;
using System;
using System.Threading.Tasks;

namespace SafePoint_IRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPut("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { error = "Current and new passwords are required." });
            }

            if (request.UserRole == "User")
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null) return NotFound(new { error = "User not found." });

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Userpassword))
                {
                    return Unauthorized(new { error = "Incorrect current password." });
                }

                user.Userpassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }
            else if (request.UserRole == "Admin")
            {
                var admin = await _context.Admins.FindAsync(request.UserId);
                if (admin == null) return NotFound(new { error = "Admin not found." });

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, admin.Adminpassword))
                {
                    return Unauthorized(new { error = "Incorrect current password." });
                }

                admin.Adminpassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }
            else if (request.UserRole == "Moderator")
            {
                var moderator = await _context.Moderators.FindAsync(request.UserId);
                if (moderator == null) return NotFound(new { error = "Moderator not found." });

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, moderator.Modpassword))
                {
                    return Unauthorized(new { error = "Incorrect current password." });
                }

                moderator.Modpassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }
            else
            {
                return BadRequest(new { error = "Invalid user role." });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully." });
        }

        [HttpGet("CheckEmail")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Email is required.");

            // Check if email exists in any table
            var userExists = await _context.Users.AnyAsync(u => u.Email == email);
            var adminExists = await _context.Admins.AnyAsync(a => a.Email == email);
            var modExists = await _context.Moderators.AnyAsync(m => m.Email == email);

            if (userExists || adminExists || modExists)
            {
                return Ok(new { exists = true });
            }

            return Ok(new { exists = false });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request.");

            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Check Users
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user != null)
            {
                user.Userpassword = newPasswordHash;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Password reset successfully." });
            }

            // Check Admins
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (admin != null)
            {
                admin.Adminpassword = newPasswordHash;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Password reset successfully." });
            }

            // Check Moderators
            var moderator = await _context.Moderators.FirstOrDefaultAsync(m => m.Email == request.Email);
            if (moderator != null)
            {
                moderator.Modpassword = newPasswordHash;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Password reset successfully." });
            }

            return NotFound(new { error = "Email not linked to any account." });
        }
    }


}
