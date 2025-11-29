using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Models;
using SafePoint_IRS.Data;
using SafePoint_IRS.DTOs;
using BCrypt.Net; 

namespace SafePoint_IRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegisterController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostUser([FromBody] CreateUserDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Invalid data provided." });

            if (await _context.Admins.AnyAsync(a => a.Username == userDto.Username || a.Email == userDto.Email) ||
                await _context.Moderators.AnyAsync(m => m.Username == userDto.Username || m.Email == userDto.Email) ||
                await _context.Users.AnyAsync(u => u.Username == userDto.Username || u.Email == userDto.Email))
            {
                return Conflict(new { error = "Username or email already in use." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var user = new User
            {
                Userid = Guid.NewGuid(),
                Username = userDto.Username,
                Email = userDto.Email,
                Contact = userDto.Contact,
                Userpassword = hashedPassword,
                IsActive = true,
                UserRole = "User"
            };

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred during registration.", details = ex.Message });
            }
        }
    }
}