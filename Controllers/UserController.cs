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
    }


}
