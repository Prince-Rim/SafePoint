using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafePoint_IRS.Data;
using SafePoint_IRS.Models;
using SafePoint_IRS.DTOs;
using BCrypt.Net;
using System.Threading.Tasks;
using System;

using Microsoft.AspNetCore.SignalR;
using SafePoint_IRS.Hubs;

namespace SafePoint_IRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        private RequesterInfo? GetRequesterInfo()
        {
            var requesterId = Request.Headers["X-Requester-Id"].FirstOrDefault();
            var requesterRole = Request.Headers["X-Requester-Role"].FirstOrDefault();

            if (string.IsNullOrEmpty(requesterId) || string.IsNullOrEmpty(requesterRole))
                return null;

            return new RequesterInfo
            {
                RequesterId = requesterId,
                RequesterRole = requesterRole
            };
        }

        private async Task<bool> IsAdmin(string requesterId)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == requesterId && a.IsActive);
            return admin != null;
        }

        private async Task<bool> IsModerator(string requesterId)
        {
            var moderator = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid.ToString() == requesterId);
            return moderator != null;
        }

        private async Task<bool> HasPermission(string adminId, string permission)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == adminId && a.IsActive);
            if (admin == null) return false;
            
            if (admin.IsSuperAdmin) return true;

            if (string.IsNullOrEmpty(admin.Permissions)) return false;

            var permissions = admin.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(p => p.Trim())
                                               .ToList();
            
            return permissions.Contains(permission);
        }

        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto userDto)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can update users." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Incorrect admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageUsers"))
            {
                return StatusCode(403, new { error = "You do not have permission to manage users." });
            }

            if (!Guid.TryParse(id, out Guid userIdGuid))
            {
                return BadRequest(new { error = "Invalid User ID format." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Userid == userIdGuid);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            if (!string.IsNullOrEmpty(userDto.Username)) user.Username = userDto.Username;
            if (!string.IsNullOrEmpty(userDto.LastName)) user.LastName = userDto.LastName;
            if (userDto.MiddleName != null) user.MiddleName = userDto.MiddleName;
            if (!string.IsNullOrEmpty(userDto.FirstName)) user.FirstName = userDto.FirstName;
            if (!string.IsNullOrEmpty(userDto.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == userDto.Email && u.Userid != userIdGuid) ||
                    await _context.Admins.AnyAsync(a => a.Email == userDto.Email) ||
                    await _context.Moderators.AnyAsync(m => m.Email == userDto.Email))
                {
                    return Conflict(new { error = "Email is already in use." });
                }
                user.Email = userDto.Email;
            }
            if (!string.IsNullOrEmpty(userDto.Contact)) user.Contact = userDto.Contact;

            if (!string.IsNullOrEmpty(userDto.Password))
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
                user.Userpassword = hashedPassword;
            }

            if (userDto.IsActive.HasValue)
            {
                user.IsActive = userDto.IsActive.Value;
                if (user.IsActive)
                {
                    user.SuspensionEndTime = null;
                }
                else if (userDto.SuspensionEndTime.HasValue)
                {
                    user.SuspensionEndTime = userDto.SuspensionEndTime.Value;
                }
            }
            else if (userDto.SuspensionEndTime.HasValue) 
            {
                 user.SuspensionEndTime = userDto.SuspensionEndTime.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the user.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("update-moderator/{id}")]
        public async Task<IActionResult> UpdateModerator(string id, [FromBody] UpdateModeratorDto modDto)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can update moderators." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageModerators"))
            {
                return StatusCode(403, new { error = "You do not have permission to manage moderators." });
            }

            if (!Guid.TryParse(id, out Guid modIdGuid))
            {
                return BadRequest(new { error = "Invalid Moderator ID format." });
            }

            var moderator = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid == modIdGuid);
            if (moderator == null)
            {
                return NotFound(new { error = "Moderator not found." });
            }

            if (!string.IsNullOrEmpty(modDto.Username)) moderator.Username = modDto.Username;
            if (!string.IsNullOrEmpty(modDto.Email))
            {
                if (await _context.Moderators.AnyAsync(m => m.Email == modDto.Email && m.Modid != modIdGuid) ||
                    await _context.Admins.AnyAsync(a => a.Email == modDto.Email) ||
                    await _context.Users.AnyAsync(u => u.Email == modDto.Email))
                {
                    return Conflict(new { error = "Email is already in use." });
                }
                moderator.Email = modDto.Email;
            }
            if (!string.IsNullOrEmpty(modDto.Contact)) moderator.Contact = modDto.Contact;

            if (!string.IsNullOrEmpty(modDto.Area_Code))
            {
                moderator.Area_Code = modDto.Area_Code;
            }

            if (!string.IsNullOrEmpty(modDto.Password))
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(modDto.Password);
                moderator.Modpassword = hashedPassword;
            }

            if (modDto.IsActive.HasValue)
            {
                moderator.IsActive = modDto.IsActive.Value;
                if (moderator.IsActive)
                {
                    moderator.SuspensionEndTime = null;
                }
                else if (modDto.SuspensionEndTime.HasValue)
                {
                    moderator.SuspensionEndTime = modDto.SuspensionEndTime.Value;
                }
            }
            else if (modDto.SuspensionEndTime.HasValue)
            {
                moderator.SuspensionEndTime = modDto.SuspensionEndTime.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Moderator updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the moderator.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("update-admin/{id}")]
        public async Task<IActionResult> UpdateAdmin(string id, [FromBody] UpdateAdminDto adminDto)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can update other admin accounts." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageAdmins"))
            {
                return StatusCode(403, new { error = "You do not have permission to manage admins." });
            }

            if (!Guid.TryParse(id, out Guid adminIdGuid))
            {
                return BadRequest(new { error = "Invalid Admin ID format." });
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid == adminIdGuid);
            if (admin == null)
            {
                return NotFound(new { error = "Admin not found." });
            }

            if (!string.IsNullOrEmpty(adminDto.Username)) admin.Username = adminDto.Username;
            if (!string.IsNullOrEmpty(adminDto.Email))
            {
                if (await _context.Admins.AnyAsync(a => a.Email == adminDto.Email && a.Adminid != adminIdGuid) ||
                    await _context.Users.AnyAsync(u => u.Email == adminDto.Email) ||
                    await _context.Moderators.AnyAsync(m => m.Email == adminDto.Email))
                {
                    return Conflict(new { error = "Email is already in use." });
                }
                admin.Email = adminDto.Email;
            }
            if (!string.IsNullOrEmpty(adminDto.Contact)) admin.Contact = adminDto.Contact;
            if (adminDto.Permissions != null) admin.Permissions = adminDto.Permissions;

            if (!string.IsNullOrEmpty(adminDto.Password))
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminDto.Password);
                admin.Adminpassword = hashedPassword;
            }

            if (adminDto.IsActive.HasValue)
            {
                admin.IsActive = adminDto.IsActive.Value;
                if (admin.IsActive)
                {
                    admin.SuspensionEndTime = null;
                }
                else if (adminDto.SuspensionEndTime.HasValue)
                {
                    admin.SuspensionEndTime = adminDto.SuspensionEndTime.Value;
                }
            }
            else if (adminDto.SuspensionEndTime.HasValue)
            {
                 admin.SuspensionEndTime = adminDto.SuspensionEndTime.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Admin updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the admin.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can delete users." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageUsers"))
            {
                return StatusCode(403, new { error = "You do not have permission to delete users." });
            }

            if (!Guid.TryParse(id, out Guid userIdGuid))
            {
                return BadRequest(new { error = "Invalid User ID format." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Userid == userIdGuid);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }


            var userArchive = new UserArchive
            {
                Userid = user.Userid,
                Username = user.Username,
                Email = user.Email,
                Contact = user.Contact,
                Userpassword = user.Userpassword,
                IsActive = user.IsActive,
                UserRole = user.UserRole,
                SuspensionEndTime = user.SuspensionEndTime,
                DeletionDate = DateTime.UtcNow
            };
            _context.UserArchives.Add(userArchive);

            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "User deleted and archived successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the user.", details = ex.Message });
            }
        }

        [HttpDelete("delete-moderator/{id}")]
        public async Task<IActionResult> DeleteModerator(string id)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can delete moderators." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageModerators"))
            {
                return StatusCode(403, new { error = "You do not have permission to delete moderators." });
            }

            if (!Guid.TryParse(id, out Guid modIdGuid))
            {
                return BadRequest(new { error = "Invalid Moderator ID format." });
            }

            var moderator = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid == modIdGuid);
            if (moderator == null)
            {
                return NotFound(new { error = "Moderator not found." });
            }

            _context.Moderators.Remove(moderator);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Moderator deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the moderator.", details = ex.Message });
            }
        }

        [HttpDelete("delete-admin/{id}")]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can delete admins." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageAdmins"))
            {
                return StatusCode(403, new { error = "You do not have permission to delete admins." });
            }

            if (!Guid.TryParse(id, out Guid adminIdGuid))
            {
                return BadRequest(new { error = "Invalid Admin ID format." });
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid == adminIdGuid);
            if (admin == null)
            {
                return NotFound(new { error = "Admin not found." });
            }

            if (admin.Adminid.ToString() == requester.RequesterId)
            {
                return BadRequest(new { error = "You cannot delete your own account." });
            }

            _context.Admins.Remove(admin);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Admin deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the admin.", details = ex.Message });
            }
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto adminDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data provided." });
                }

                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != "Admin")
                {
                    return Unauthorized(new { error = "Only Admin can create Admin accounts." });
                }

                var requesterAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == requester.RequesterId);
                if (requesterAdmin == null || !requesterAdmin.IsSuperAdmin)
                {
                    return Unauthorized(new { error = "Only Super Admin can create other Admin accounts." });
                }

                if (await _context.Admins.AnyAsync(a => a.Username == adminDto.Username || a.Email == adminDto.Email) ||
                    await _context.Moderators.AnyAsync(m => m.Username == adminDto.Username || m.Email == adminDto.Email) ||
                    await _context.Users.AnyAsync(u => u.Username == adminDto.Username || u.Email == adminDto.Email))
                {
                    return Conflict(new { error = "Username or email already in use." });
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminDto.Password);

                var newAdmin = new Admin
                {
                    Adminid = Guid.NewGuid(),
                    Username = adminDto.Username,
                    Email = adminDto.Email,
                    Contact = adminDto.Contact,
                    Adminpassword = hashedPassword,
                    UserRole = "Admin",
                    IsActive = true,
                    IsSuperAdmin = false,
                    Permissions = adminDto.Permissions
                };

                _context.Admins.Add(newAdmin);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Admin created successfully.", adminId = newAdmin.Adminid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating admin.", details = ex.Message });
            }
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data provided." });
                }

                var requester = GetRequesterInfo();
                if (requester == null)
                {
                    return Unauthorized(new { error = "Requester information required." });
                }

                bool isAuthorized = false;
                if (requester.RequesterRole == "Admin")
                {
                    isAuthorized = await IsAdmin(requester.RequesterId);
                    if (isAuthorized && !await HasPermission(requester.RequesterId, "ManageUsers"))
                    {
                        return StatusCode(403, new { error = "You do not have permission to create users." });
                    }
                }
                else if (requester.RequesterRole == "Moderator")
                {
                    isAuthorized = await IsModerator(requester.RequesterId);
                }

                if (!isAuthorized)
                {
                    return Unauthorized(new { error = "Only Admin or Moderator can create users." });
                }

                userDto.UserRole = "User";

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
                    LastName = userDto.LastName,
                    MiddleName = userDto.MiddleName,
                    FirstName = userDto.FirstName,
                    Email = userDto.Email,
                    Contact = userDto.Contact,
                    Userpassword = hashedPassword,
                    IsActive = true,
                    UserRole = userDto.UserRole
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{userDto.UserRole} created successfully.", userId = newUser.Userid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating user.", details = ex.Message });
            }
        }

        [HttpPost("create-moderator")]
        public async Task<IActionResult> CreateModerator([FromBody] CreateModeratorDto modDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data provided." });
                }

                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != "Admin")
                {
                    return Unauthorized(new { error = "Only Admin can create Moderator accounts." });
                }

                if (!await IsAdmin(requester.RequesterId))
                {
                    return Unauthorized(new { error = "Invalid admin credentials." });
                }

                if (!await HasPermission(requester.RequesterId, "ManageModerators"))
                {
                    return StatusCode(403, new { error = "You do not have permission to create moderators." });
                }

                modDto.UserRole = "Moderator";
                var area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == modDto.Area_Code);
                if (area == null)
                {
                    if (modDto.Area_Code == "DEFAULT")
                    {
                        try
                        {
                            area = new Area
                            {
                                Area_Code = "DEFAULT",
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
                                    .Where(e => e.Entity.Area_Code == "DEFAULT")
                                    .ToList()
                                    .ForEach(e => e.State = Microsoft.EntityFrameworkCore.EntityState.Detached);

                                area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == "DEFAULT");
                            }

                            if (area == null)
                            {
                                return StatusCode(500, new { error = "Failed to create or retrieve DEFAULT area.", details = dbEx.InnerException?.Message ?? dbEx.Message });
                            }
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = $"Area '{modDto.Area_Code}' not found. Use 'DEFAULT' or create the area first." });
                    }
                }

                if (await _context.Admins.AnyAsync(a => a.Username == modDto.Username || a.Email == modDto.Email) ||
                    await _context.Moderators.AnyAsync(m => m.Username == modDto.Username || m.Email == modDto.Email) ||
                    await _context.Users.AnyAsync(u => u.Username == modDto.Username || u.Email == modDto.Email))
                {
                    return Conflict(new { error = "Username or email already in use." });
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(modDto.Password);

                var newModerator = new Moderator
                {
                    Modid = Guid.NewGuid(),
                    Username = modDto.Username,
                    Email = modDto.Email,
                    Contact = modDto.Contact,
                    Area_Code = modDto.Area_Code,
                    Modpassword = hashedPassword,
                    UserRole = modDto.UserRole,
                    IsActive = true
                };

                _context.Moderators.Add(newModerator);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{modDto.UserRole} created successfully.", moderatorId = newModerator.Modid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating moderator.", details = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {

            var suspendedUsers = await _context.Users
                .Where(u => !u.IsActive && u.SuspensionEndTime != null && u.SuspensionEndTime <= DateTime.Now)
                .ToListAsync();

            if (suspendedUsers.Any())
            {
                foreach (var user in suspendedUsers)
                {
                    user.IsActive = true;
                    user.SuspensionEndTime = null;
                }
                await _context.SaveChangesAsync();
            }

            var users = await _context.Users
                .Select(u => new
                {
                    u.Userid,
                    u.Username,
                    u.LastName,
                    u.MiddleName,
                    u.FirstName,
                    u.Email,
                    u.Contact,
                    u.UserRole,
                    u.IsActive,
                    u.SuspensionEndTime,
                    u.TrustScore,
                    Badges = u.Badges.Select(b => new { b.Id, b.BadgeName, b.AwardedAt, b.AwardedBy }).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("add-badge")]
        public async Task<IActionResult> AddBadge([FromBody] AddBadgeDto badgeDto)
        {
            var requester = GetRequesterInfo();
            if (requester == null || (requester.RequesterRole != "Admin" && requester.RequesterRole != "Moderator"))
            {
                return Unauthorized(new { error = "Only Admin/Moderator can add badges." });
            }

            var user = await _context.Users.FindAsync(badgeDto.UserId);
            if (user == null) return NotFound(new { error = "User not found." });

            var badge = new UserBadge
            {
                UserId = badgeDto.UserId,
                BadgeName = badgeDto.BadgeName,
                AwardedBy = await GetUsernameById(requester.RequesterId, requester.RequesterRole)
            };

            _context.UserBadges.Add(badge);
            await _context.SaveChangesAsync();
            

            await _hubContext.Clients.All.SendAsync("ReceiveBadgeNotification", badgeDto.UserId.ToString(), badgeDto.BadgeName);

            return Ok(new { message = "Badge added successfully.", badge });
        }

        [HttpDelete("remove-badge/{id}")]
        public async Task<IActionResult> RemoveBadge(int id)
        {
             var requester = GetRequesterInfo();
            if (requester == null || (requester.RequesterRole != "Admin" && requester.RequesterRole != "Moderator"))
            {
                return Unauthorized(new { error = "Only Admin/Moderator can remove badges." });
            }

            var badge = await _context.UserBadges.FindAsync(id);
            if (badge == null) return NotFound(new { error = "Badge not found." });

            _context.UserBadges.Remove(badge);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Badge removed successfully." });
        }

        private async Task<string> GetUsernameById(string id, string role)
        {
            if (role == "Admin")
            {
                 var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == id);
                 return admin?.Username ?? "Unknown Admin";
            }
            else if (role == "Moderator")
            {
                 var mod = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid.ToString() == id);
                 return mod?.Username ?? "Unknown Moderator";
            }
            return "Unknown";
        }

        [HttpGet("moderators")]
        public async Task<IActionResult> GetAllModerators()
        {

            var suspendedMods = await _context.Moderators
                .Where(m => !m.IsActive && m.SuspensionEndTime != null && m.SuspensionEndTime <= DateTime.Now)
                .ToListAsync();

            if (suspendedMods.Any())
            {
                foreach (var mod in suspendedMods)
                {
                    mod.IsActive = true;
                    mod.SuspensionEndTime = null;
                }
                await _context.SaveChangesAsync();
            }

            var moderators = await _context.Moderators
                .Include(m => m.Area)
                .Select(m => new
                {
                    m.Modid,
                    m.Username,
                    m.FirstName,
                    m.LastName,
                    m.MiddleName,
                    m.Email,
                    m.Contact,
                    m.Area_Code,
                    m.UserRole,
                    AreaName = m.Area != null ? m.Area.ALocation : null,
                    m.IsActive,
                    m.SuspensionEndTime,
                    TrustScore = 0,
                    Badges = new List<object>()
                })
                .ToListAsync();

            return Ok(moderators);
        }

        [HttpGet("admins")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var suspendedAdmins = await _context.Admins
                .Where(a => !a.IsActive && a.SuspensionEndTime != null && a.SuspensionEndTime <= DateTime.Now)
                .ToListAsync();

            if (suspendedAdmins.Any())
            {
                foreach (var admin in suspendedAdmins)
                {
                    admin.IsActive = true;
                    admin.SuspensionEndTime = null;
                }
                await _context.SaveChangesAsync();
            }

            var admins = await _context.Admins
                .Where(a => !a.IsSuperAdmin)
                .Select(a => new
                {
                    a.Adminid,
                    a.Username,
                    a.FirstName,
                    a.LastName,
                    a.MiddleName,
                    a.Email,
                    a.Contact,
                    a.UserRole,
                    a.IsActive,
                    a.SuspensionEndTime,
                    a.Permissions
                })
                .ToListAsync();

            return Ok(admins);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingIncidents()
        {
            var pendingIncidents = await _context.Incident
                .Include(i => i.ValidStatus)
                .Include(i => i.User)
                .Include(i => i.Area)
                .Include(i => i.IType)
                .Where(i => i.ValidStatus != null && i.ValidStatus.Validation_Status == false && i.ValidStatus.Validation_Date == null)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.IncidentID,
                    i.Userid,
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
                    Area = i.Area != null ? new { i.Area.ALocation, i.Area.Area_Code } : null,
                    i.Img,
                    i.OtherHazard,
                    i.LocationAddress
                })
                .ToListAsync();

            return Ok(pendingIncidents);
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedIncidents()
        {
            var deletedIncidents = await _context.RejectedIncidents
                .Include(i => i.User)
                .Include(i => i.Area)
                .Include(i => i.IType)
                .OrderByDescending(i => i.RejectionDate)
                .Select(i => new
                {
                    IncidentID = i.RejectedIncidentID, 
                    i.Userid,
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
                    Area = i.Area != null ? new { i.Area.ALocation, i.Area.Area_Code } : null,
                    i.Img,
                    i.OtherHazard,
                    ValidationDate = i.RejectionDate,
                    i.LocationAddress
                })
                .ToListAsync();

            return Ok(deletedIncidents);
        }

        [HttpPost("recover/{id}")]
        public async Task<IActionResult> RecoverIncident(int id)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can recover incidents." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageIncidents"))
            {
                return StatusCode(403, new { error = "You do not have permission to recover incidents." });
            }

            var rejected = await _context.RejectedIncidents.FindAsync(id);
            if (rejected == null)
            {
                return NotFound(new { error = "Rejected incident not found." });
            }

            var newIncident = new Incident
            {
                Userid = rejected.Userid,
                Title = rejected.Title,
                Incident_Code = rejected.Incident_Code,
                OtherHazard = rejected.OtherHazard,
                Severity = rejected.Severity,
                IncidentDateTime = rejected.IncidentDateTime,
                Area_Code = rejected.Area_Code,
                Descr = rejected.Descr,
                Img = rejected.Img,
                CreatedAt = rejected.CreatedAt, 
                Latitude = rejected.Latitude,
                Longitude = rejected.Longitude,
                LocationAddress = rejected.LocationAddress
            };

            _context.Incident.Add(newIncident);
            await _context.SaveChangesAsync(); 

            var validStatus = new Valid
            {
                IncidentID = newIncident.IncidentID,
                Validation_Status = false,
                Validation_Date = null 
            };
            _context.Valid.Add(validStatus);
            
            _context.RejectedIncidents.Remove(rejected);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Incident recovered successfully." });
        }

        [HttpPost("validate/{incidentId}")]
        public async Task<IActionResult> ValidateIncident(int incidentId, [FromQuery] bool isAccepted)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can validate incidents." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageIncidents"))
            {
                return StatusCode(403, new { error = "You do not have permission to validate incidents." });
            }

            var incident = await _context.Incident
                .Include(i => i.ValidStatus)
                .FirstOrDefaultAsync(i => i.IncidentID == incidentId);

            if (incident == null)
            {
                return NotFound(new { error = "Incident not found." });
            }

            if (isAccepted)
            {
                if (incident.ValidStatus == null)
                {
                     
                     incident.ValidStatus = new Valid { IncidentID = incident.IncidentID };
                }
                incident.ValidStatus.Validation_Status = true;
                incident.ValidStatus.Validation_Date = DateTime.UtcNow;
                
                
                

                if (requester != null && Guid.TryParse(requester.RequesterId, out Guid validatorId))
                {
                    incident.ValidStatus.ValidatorID = validatorId;
                }

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveIncidentNotification", incident.Title, incident.LocationAddress, incident.Latitude, incident.Longitude, incident.IncidentID, "Validated", incident.Userid);


                var userVerifiedCount = await _context.Incident
                    .Include(i => i.ValidStatus)
                    .CountAsync(i => i.Userid == incident.Userid && i.ValidStatus != null && i.ValidStatus.Validation_Status == true);

                if (userVerifiedCount >= 10)
                {
                    await CheckAndAwardBadge(incident.Userid, "Certified Reporter");
                }
                if (userVerifiedCount >= 25)
                {
                    await CheckAndAwardBadge(incident.Userid, "Reliable Source");
                }
                if (userVerifiedCount >= 50)
                {
                    await CheckAndAwardBadge(incident.Userid, "Top Contributor");
                }


                return Ok(new { message = $"Incident ID {incidentId} accepted." });
            }
            else
            {
                
                var rejected = new RejectedIncident
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
                    RejectionDate = DateTime.UtcNow
                };

                

                if (requester != null && Guid.TryParse(requester.RequesterId, out Guid rejectorId))
                {
                    rejected.RejectorID = rejectorId;
                }

                _context.RejectedIncidents.Add(rejected);
                _context.Incident.Remove(incident);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveIncidentNotification", incident.Title, incident.LocationAddress, incident.Latitude, incident.Longitude, incident.IncidentID, "Rejected", incident.Userid);

                return Ok(new { message = $"Incident ID {incidentId} rejected and moved to archive." });
            }
        }

        [HttpPost("unvalidate/{incidentId}")]
        public async Task<IActionResult> UnvalidateIncident(int incidentId)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != "Admin")
            {
                return Unauthorized(new { error = "Only Admin can unvalidate incidents." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }

            if (!await HasPermission(requester.RequesterId, "ManageIncidents"))
            {
                return StatusCode(403, new { error = "You do not have permission to unvalidate incidents." });
            }

            var incident = await _context.Incident
                .Include(i => i.ValidStatus)
                .FirstOrDefaultAsync(i => i.IncidentID == incidentId);

            if (incident == null)
            {
                return NotFound(new { error = "Incident not found." });
            }

            if (incident.ValidStatus == null)
            {
                incident.ValidStatus = new Valid 
                { 
                    IncidentID = incident.IncidentID,
                    Validation_Status = false,
                    Validation_Date = null
                };
                _context.Valid.Add(incident.ValidStatus);
            }
            else
            {
                incident.ValidStatus.Validation_Status = false;
                incident.ValidStatus.Validation_Date = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Incident ID {incidentId} unvalidated and moved to Pending." });
        }

        [HttpDelete("delete-permanent/{id}")]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            var requester = GetRequesterInfo();
            if (requester == null || (requester.RequesterRole != "Admin" && requester.RequesterRole != "Moderator"))
            {
                return Unauthorized(new { error = "Only Admin or Moderator can permanently delete incidents." });
            }

            if (requester.RequesterRole == "Admin")
            {
                if (!await IsAdmin(requester.RequesterId)) return Unauthorized(new { error = "Invalid admin credentials." });
                
                if (!await HasPermission(requester.RequesterId, "ManageIncidents"))
                {
                    return StatusCode(403, new { error = "You do not have permission to delete incidents." });
                }
            }
            else
            {
                if (!await IsModerator(requester.RequesterId)) return Unauthorized(new { error = "Invalid moderator credentials." });
            }

            var rejected = await _context.RejectedIncidents.FindAsync(id);
            if (rejected == null)
            {
                return NotFound(new { error = "Rejected incident not found." });
            }


            var incidentArchive = new IncidentArchive
            {
                OriginalIncidentID = rejected.OriginalIncidentID,
                Userid = rejected.Userid,
                Title = rejected.Title,
                Incident_Code = rejected.Incident_Code,
                OtherHazard = rejected.OtherHazard,
                Severity = rejected.Severity,
                IncidentDateTime = rejected.IncidentDateTime,
                Area_Code = rejected.Area_Code,
                Descr = rejected.Descr,
                Img = rejected.Img,
                CreatedAt = rejected.CreatedAt,
                Latitude = rejected.Latitude,
                Longitude = rejected.Longitude,
                LocationAddress = rejected.LocationAddress,
                DeletionDate = DateTime.UtcNow
            };
            _context.IncidentArchives.Add(incidentArchive);

            _context.RejectedIncidents.Remove(rejected);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Incident permanently deleted and archived." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the incident.", details = ex.Message });
            }
        }

        [HttpGet("moderators-by-area/{areaCode}")]
        public async Task<IActionResult> GetModeratorsByArea(string areaCode)
        {
            var moderators = await _context.Moderators
                .Include(m => m.Area)
                .Where(m => m.Area_Code == areaCode)
                .Select(m => new
                {
                    m.Modid,
                    m.Username,
                    m.Email,
                    m.Contact,
                    m.Area_Code,
                    m.UserRole,
                    AreaName = m.Area != null ? m.Area.ALocation : null
                })
                .ToListAsync();

            var area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == areaCode);

            return Ok(new
            {
                areaCode = areaCode,
                areaName = area != null ? area.ALocation : "Unknown Area",
                moderators = moderators,
                totalCount = moderators.Count
            });
        }

        [HttpGet("incidents-by-area/{areaCode}")]
        public async Task<IActionResult> GetIncidentsByArea(string areaCode)
        {
            var incidents = await _context.Incident
                .Include(i => i.User)
                .Include(i => i.ValidStatus)
                .Include(i => i.IType)
                .Where(i => i.Area_Code == areaCode)
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
                    i.LocationAddress
                })
                .ToListAsync();

            var area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == areaCode);

            return Ok(new
            {
                areaCode = areaCode,
                areaName = area != null ? area.ALocation : "Unknown Area",
                incidents = incidents,
                totalCount = incidents.Count
            });
        }

        [HttpPost("create-area")]
        public async Task<IActionResult> CreateArea([FromBody] CreateAreaDto areaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data provided." });
                }

                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != "Admin")
                {
                    return Unauthorized(new { error = "Only Admin can create areas." });
                }

                if (!await IsAdmin(requester.RequesterId))
                {
                    return Unauthorized(new { error = "Invalid admin credentials." });
                }

                var existingArea = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == areaDto.Area_Code);
                if (existingArea != null)
                {
                    return Conflict(new { error = $"Area with code '{areaDto.Area_Code}' already exists." });
                }

                var newArea = new Area
                {
                    Area_Code = areaDto.Area_Code,
                    ALocation = areaDto.ALocation
                };

                _context.Area.Add(newArea);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Area created successfully.",
                    area = new { newArea.Area_Code, newArea.ALocation }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating area.", details = ex.Message });
            }
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAllAreas()
        {
            var areas = await _context.Area
                .Select(a => new
                {
                    a.Area_Code,
                    a.ALocation,
                    moderatorCount = _context.Moderators.Count(m => m.Area_Code == a.Area_Code),
                    incidentCount = _context.Incident.Count(i => i.Area_Code == a.Area_Code)
                })
                .ToListAsync();

            return Ok(areas);
        }

        [HttpPut("update-area/{areaCode}")]
        public async Task<IActionResult> UpdateArea(string areaCode, [FromBody] CreateAreaDto areaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data provided." });
                }

                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != "Admin")
                {
                    return Unauthorized(new { error = "Only Admin can update areas." });
                }

                if (!await IsAdmin(requester.RequesterId))
                {
                    return Unauthorized(new { error = "Invalid admin credentials." });
                }

                var area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == areaCode);
                if (area == null)
                {
                    return NotFound(new { error = "Area not found." });
                }

                area.ALocation = areaDto.ALocation;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Area updated successfully.",
                    area = new { area.Area_Code, area.ALocation }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating area.", details = ex.Message });
            }
        }

        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
            {
                var approvedIncidents = await _context.Incident
                    .Include(i => i.ValidStatus)
                    .Where(i => i.ValidStatus != null && i.ValidStatus.Validation_Status == true && i.ValidStatus.Validation_Date != null)
                    .Select(i => new
                    {
                        IncidentID = i.IncidentID,
                        Title = i.Title,
                        ValidationDate = i.ValidStatus.Validation_Date,
                        Status = "Approved",
                        ValidatorID = i.ValidStatus.ValidatorID
                    })
                    .OrderByDescending(i => i.ValidationDate)
                    .Take(5)
                    .ToListAsync();
                var rejectedIncidents = await _context.RejectedIncidents
                    .Select(i => new
                    {
                        IncidentID = i.RejectedIncidentID, 
                        Title = i.Title,
                        ValidationDate = (DateTime?)i.RejectionDate,
                        Status = "Rejected",
                        ValidatorID = i.RejectorID
                    })
                    .OrderByDescending(i => i.ValidationDate)
                    .Take(5)
                    .ToListAsync();

                var recentActivity = approvedIncidents.Concat(rejectedIncidents)
                    .OrderByDescending(i => i.ValidationDate)
                    .Take(7)
                    .ToList();

                var enrichedActivity = new List<object>();
                foreach (var activity in recentActivity)
                {
                    string validatorName = "Unknown";
                    if (activity.ValidatorID.HasValue)
                    {
                        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid == activity.ValidatorID.Value);
                        if (admin != null)
                        {
                            validatorName = admin.Username;
                        }
                        else
                        {
                            var mod = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid == activity.ValidatorID.Value);
                            if (mod != null)
                            {
                                validatorName = mod.Username;
                            }
                        }
                    }

                    enrichedActivity.Add(new
                    {
                        activity.IncidentID,
                        activity.Title,
                        activity.ValidationDate,
                        activity.Status,
                        ValidatorName = validatorName
                    });
                }

                return Ok(enrichedActivity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching recent activity.", details = ex.Message });
            }
        }
        [HttpPost("change-role")]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleDto changeDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != "Admin")
                    return Unauthorized(new { error = "Only Admin can change user roles." });

                if (!await IsAdmin(requester.RequesterId))
                    return Unauthorized(new { error = "Invalid admin credentials." });

                
                if (changeDto.TargetRole == "Admin")
                {
                   var requesterAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == requester.RequesterId);
                   if (requesterAdmin == null || !requesterAdmin.IsSuperAdmin)
                       return Unauthorized(new { error = "Only Super Admin can promote users to Admin." });
                }
                else if (changeDto.TargetRole == "Moderator" && !await HasPermission(requester.RequesterId, "ManageModerators"))
                {
                    return StatusCode(403, new { error = "You do not have permission to create Moderators." });
                }
                else if (changeDto.TargetRole == "User" && !await HasPermission(requester.RequesterId, "ManageUsers"))
                {
                    return StatusCode(403, new { error = "You do not have permission to manage Users." });
                }

                if (!Guid.TryParse(changeDto.Id, out Guid idGuid))
                    return BadRequest(new { error = "Invalid ID format." });

                
                string username = "", email = "", contact = "", passwordHash = "", firstName = "", lastName = "", middleName = "";
                bool isActive = true;
                DateTime? suspensionEnd = null;

                if (changeDto.CurrentRole == "User")
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Userid == idGuid);
                    if (user == null) return NotFound(new { error = "User not found." });
                    username = user.Username; email = user.Email; contact = user.Contact; passwordHash = user.Userpassword;
                    firstName = user.FirstName; lastName = user.LastName; middleName = user.MiddleName;
                    isActive = user.IsActive; suspensionEnd = user.SuspensionEndTime;
                    _context.Users.Remove(user);
                }
                else if (changeDto.CurrentRole == "Moderator")
                {
                    var mod = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid == idGuid);
                    if (mod == null) return NotFound(new { error = "Moderator not found." });
                    username = mod.Username; email = mod.Email; contact = mod.Contact; passwordHash = mod.Modpassword;
                    
                    isActive = mod.IsActive; suspensionEnd = mod.SuspensionEndTime;
                    _context.Moderators.Remove(mod);
                }
                else if (changeDto.CurrentRole == "Admin")
                {
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid == idGuid);
                    if (admin == null) return NotFound(new { error = "Admin not found." });
                    if (admin.Adminid.ToString() == requester.RequesterId) return BadRequest(new { error = "You cannot change your own role." });
                    
                    username = admin.Username; email = admin.Email; contact = admin.Contact; passwordHash = admin.Adminpassword;
                    isActive = admin.IsActive; suspensionEnd = admin.SuspensionEndTime;
                    _context.Admins.Remove(admin);
                }
                else return BadRequest(new { error = "Invalid current role." });

                
                if (!string.IsNullOrEmpty(changeDto.Username)) username = changeDto.Username;
                if (!string.IsNullOrEmpty(changeDto.Email)) email = changeDto.Email;
                if (!string.IsNullOrEmpty(changeDto.Contact)) contact = changeDto.Contact;
                if (!string.IsNullOrEmpty(changeDto.FirstName)) firstName = changeDto.FirstName;
                if (!string.IsNullOrEmpty(changeDto.LastName)) lastName = changeDto.LastName;
                if (changeDto.MiddleName != null) middleName = changeDto.MiddleName;
                if (changeDto.IsActive.HasValue) isActive = changeDto.IsActive.Value;
                if (changeDto.SuspensionEndTime.HasValue || isActive) suspensionEnd = isActive ? null : changeDto.SuspensionEndTime;

                if (!string.IsNullOrEmpty(changeDto.Password))
                {
                    passwordHash = BCrypt.Net.BCrypt.HashPassword(changeDto.Password);
                }

                
                if (changeDto.TargetRole == "User")
                {
                    var newUser = new User
                    {
                        Userid = Guid.NewGuid(),
                        Username = username,
                        Email = email,
                        Contact = contact,
                        Userpassword = passwordHash,
                        FirstName = !string.IsNullOrEmpty(firstName) ? firstName : "Unknown",
                        LastName = !string.IsNullOrEmpty(lastName) ? lastName : "User",
                        MiddleName = middleName,
                        UserRole = "User",
                        IsActive = isActive,
                        SuspensionEndTime = suspensionEnd
                    };
                    _context.Users.Add(newUser);
                }
                else if (changeDto.TargetRole == "Moderator")
                {
                    var newMod = new Moderator
                    {
                        Modid = Guid.NewGuid(),
                        Username = username,
                        Email = email,
                        Contact = contact,
                        Modpassword = passwordHash,
                        FirstName = !string.IsNullOrEmpty(firstName) ? firstName : "Moderator",
                        LastName = !string.IsNullOrEmpty(lastName) ? lastName : "User",
                        MiddleName = middleName,
                        Area_Code = changeDto.Area_Code ?? "DEFAULT",
                        UserRole = "Moderator",
                        IsActive = isActive,
                        SuspensionEndTime = suspensionEnd
                    };
                     
                     var area = await _context.Area.FirstOrDefaultAsync(a => a.Area_Code == newMod.Area_Code);
                     if(area == null && newMod.Area_Code == "DEFAULT") {
                          _context.Area.Add(new Area { Area_Code = "DEFAULT", ALocation = "Default Area" });
                     }

                    _context.Moderators.Add(newMod);
                }
                else if (changeDto.TargetRole == "Admin")
                {
                    var newAdmin = new Admin
                    {
                        Adminid = Guid.NewGuid(),
                        Username = username,
                        Email = email,
                        Contact = contact,
                        Adminpassword = passwordHash,
                        FirstName = !string.IsNullOrEmpty(firstName) ? firstName : "Admin",
                        LastName = !string.IsNullOrEmpty(lastName) ? lastName : "User",
                        MiddleName = middleName,
                        Permissions = changeDto.Permissions,
                        UserRole = "Admin",
                        IsActive = isActive,
                        IsSuperAdmin = false,
                        SuspensionEndTime = suspensionEnd
                    };
                    _context.Admins.Add(newAdmin);
                }
                else return BadRequest(new { error = "Invalid target role." });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $"Role changed from {changeDto.CurrentRole} to {changeDto.TargetRole} successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Role change failed.", details = ex.Message });
            }
        }
        private async Task CheckAndAwardBadge(Guid userId, string badgeName)
        {
            var hasBadge = await _context.UserBadges.AnyAsync(b => b.UserId == userId && b.BadgeName == badgeName);
            if (!hasBadge)
            {
                var autoBadge = new UserBadge
                {
                    UserId = userId,
                    BadgeName = badgeName,
                    AwardedBy = "System",
                    AwardedAt = DateTime.UtcNow
                };
                _context.UserBadges.Add(autoBadge);
                await _context.SaveChangesAsync();
                

                await _hubContext.Clients.All.SendAsync("ReceiveBadgeNotification", userId.ToString(), badgeName);
            }
        }
    }
}