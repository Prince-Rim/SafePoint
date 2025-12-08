using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using SafePoint_IRS.Data;
using SafePoint_IRS.Models;
using SafePoint_IRS.DTOs;
using BCrypt.Net;
using System;

namespace SafePoint_IRS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user != null && user.Username.Equals(request.Username, StringComparison.Ordinal))
            {
                if (!user.IsActive)
                {
                    if (user.SuspensionEndTime.HasValue)
                    {
                        if (user.SuspensionEndTime.Value <= DateTime.Now)
                        {
                            // Suspension expired, reactivate
                            user.IsActive = true;
                            user.SuspensionEndTime = null;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Still suspended
                            var localTime = user.SuspensionEndTime.Value;
                            return Unauthorized(new { error = $"Account is suspended until {localTime}." });
                        }
                    }
                    else
                    {
                        return Unauthorized(new { error = "Account is currently inactive or deactivated." });
                    }
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Userpassword);
                if (isPasswordValid)
                {
                    return Ok(new LoginResponse
                    {
                        Message = "Login successful",
                        UserId = user.Userid,
                        UserRole = user.UserRole,
                        UserType = "User",
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        MiddleName = user.MiddleName
                    });
                }
            }

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == request.Username);

            if (admin != null && admin.Username.Equals(request.Username, StringComparison.Ordinal))
            {
                if (!admin.IsActive)
                {
                    if (admin.SuspensionEndTime.HasValue)
                    {
                        if (admin.SuspensionEndTime.Value <= DateTime.Now)
                        {
                            admin.IsActive = true;
                            admin.SuspensionEndTime = null;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            var localTime = admin.SuspensionEndTime.Value;
                            return Unauthorized(new { error = $"Account is suspended until {localTime}." });
                        }
                    }
                    else
                    {
                        return Unauthorized(new { error = "Account is currently inactive or deactivated." });
                    }
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, admin.Adminpassword);
                if (isPasswordValid)
                {
                    return Ok(new LoginResponse
                    {
                        Message = "Login successful",
                        UserId = admin.Adminid,
                        UserRole = admin.UserRole,
                        UserType = "Admin",
                        Email = admin.Email,
                        FirstName = admin.FirstName,
                        LastName = admin.LastName,
                        MiddleName = admin.MiddleName
                    });
                }
            }

            var moderator = await _context.Moderators
                .FirstOrDefaultAsync(m => m.Username == request.Username);

            if (moderator != null && moderator.Username.Equals(request.Username, StringComparison.Ordinal))
            {
                if (!moderator.IsActive)
                {
                    if (moderator.SuspensionEndTime.HasValue)
                    {
                        if (moderator.SuspensionEndTime.Value <= DateTime.Now)
                        {
                            moderator.IsActive = true;
                            moderator.SuspensionEndTime = null;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            var localTime = moderator.SuspensionEndTime.Value;
                            return Unauthorized(new { error = $"Account is suspended until {localTime}." });
                        }
                    }
                    else
                    {
                        return Unauthorized(new { error = "Account is currently inactive or deactivated." });
                    }
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, moderator.Modpassword);
                if (isPasswordValid)
                {
                    return Ok(new LoginResponse
                    {
                        Message = "Login successful",
                        UserId = moderator.Modid,
                        UserRole = moderator.UserRole,
                        UserType = "Moderator",
                        AreaCode = moderator.Area_Code,
                        Email = moderator.Email,
                        FirstName = moderator.FirstName,
                        LastName = moderator.LastName,
                        MiddleName = moderator.MiddleName
                    });
                }
            }

            return Unauthorized(new { error = "Incorrect username or password" });
        }
    }
}