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
using System.Linq;

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

        private async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email) ||
                   await _context.Admins.AnyAsync(a => a.Email == email) ||
                   await _context.Moderators.AnyAsync(m => m.Email == email);
        }

        private async Task<bool> EmailExistsForUpdateAsync(string email, Guid currentUserId)
        {
            return await _context.Users.AnyAsync(u => u.Email == email && u.Userid != currentUserId) ||
                   await _context.Admins.AnyAsync(a => a.Email == email && a.Adminid != currentUserId) ||
                   await _context.Moderators.AnyAsync(m => m.Email == email && m.Modid != currentUserId);
        }

        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto userDto)
        {
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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
                
                if (!string.IsNullOrEmpty(userDto.Email) && userDto.Email != user.Email)
                {
                    if (await EmailExistsForUpdateAsync(userDto.Email, userIdGuid))
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

                await _context.SaveChangesAsync();
                return Ok(new { message = "User updated successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while updating the user." });
            }
        }

        [HttpPut("update-moderator/{id}")]
        public async Task<IActionResult> UpdateModerator(string id, [FromBody] UpdateModeratorDto modDto)
        {
            try 
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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
                
                if (!string.IsNullOrEmpty(modDto.Email) && modDto.Email != moderator.Email)
                {
                    if (await EmailExistsForUpdateAsync(modDto.Email, modIdGuid))
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

                await _context.SaveChangesAsync();
                return Ok(new { message = "Moderator updated successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while updating the moderator." });
            }
        }

        [HttpPut("update-admin/{id}")]
        public async Task<IActionResult> UpdateAdmin(string id, [FromBody] UpdateAdminDto adminDto)
        {
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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
                
                if (!string.IsNullOrEmpty(adminDto.Email) && adminDto.Email != admin.Email)
                {
                    if (await EmailExistsForUpdateAsync(adminDto.Email, adminIdGuid))
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

                await _context.SaveChangesAsync();
                return Ok(new { message = "Admin updated successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while updating the admin." });
            }
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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
                await _context.SaveChangesAsync();
                return Ok(new { message = "User deleted and archived successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the user." });
            }
        }

        [HttpDelete("delete-moderator/{id}")]
        public async Task<IActionResult> DeleteModerator(string id)
        {
            try 
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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
                await _context.SaveChangesAsync();
                return Ok(new { message = "Moderator deleted successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the moderator." });
            }
        }

        [HttpDelete("delete-admin/{id}")]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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
                await _context.SaveChangesAsync();
                return Ok(new { message = "Admin deleted successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the admin." });
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
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
                {
                    return Unauthorized(new { error = "Only Admin can create Admin accounts." });
                }

                var requesterAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == requester.RequesterId);
                if (requesterAdmin == null || !requesterAdmin.IsSuperAdmin)
                {
                    return Unauthorized(new { error = "Only Super Admin can create other Admin accounts." });
                }

                if (await EmailExistsAsync(adminDto.Email))
                {
                    return Conflict(new { error = "Email is already in use." });
                }

                // Check username uniqueness
                if (await _context.Admins.AnyAsync(a => a.Username == adminDto.Username) ||
                    await _context.Moderators.AnyAsync(m => m.Username == adminDto.Username) ||
                    await _context.Users.AnyAsync(u => u.Username == adminDto.Username))
                {
                     return Conflict(new { error = "Username is already in use." });
                }


                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminDto.Password);

                var newAdmin = new Admin
                {
                    Adminid = Guid.NewGuid(),
                    Username = adminDto.Username,
                    Email = adminDto.Email,
                    Contact = adminDto.Contact,
                    Adminpassword = hashedPassword,
                    UserRole = UserRoles.Admin,
                    IsActive = true,
                    IsSuperAdmin = false,
                    Permissions = adminDto.Permissions
                };

                _context.Admins.Add(newAdmin);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Admin created successfully.", adminId = newAdmin.Adminid });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while creating admin." });
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
                if (requester.RequesterRole == UserRoles.Admin)
                {
                    isAuthorized = await IsAdmin(requester.RequesterId);
                    if (isAuthorized && !await HasPermission(requester.RequesterId, "ManageUsers"))
                    {
                        return StatusCode(403, new { error = "You do not have permission to create users." });
                    }
                }
                else if (requester.RequesterRole == UserRoles.Moderator)
                {
                    isAuthorized = await IsModerator(requester.RequesterId);
                }

                if (!isAuthorized)
                {
                    return Unauthorized(new { error = "Only Admin or Moderator can create users." });
                }

                userDto.UserRole = UserRoles.User;

                if (await EmailExistsAsync(userDto.Email))
                {
                     return Conflict(new { error = "Email is already in use." });
                }

                 // Check username uniqueness
                if (await _context.Admins.AnyAsync(a => a.Username == userDto.Username) ||
                    await _context.Moderators.AnyAsync(m => m.Username == userDto.Username) ||
                    await _context.Users.AnyAsync(u => u.Username == userDto.Username))
                {
                     return Conflict(new { error = "Username is already in use." });
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
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while creating user." });
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
                if (requester == null || requester.RequesterRole != UserRoles.Admin)
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

                modDto.UserRole = UserRoles.Moderator;
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
                                return StatusCode(500, new { error = "Failed to create or retrieve DEFAULT area." });
                            }
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = $"Area '{modDto.Area_Code}' not found. Use 'DEFAULT' or create the area first." });
                    }
                }

                if (await EmailExistsAsync(modDto.Email))
                {
                     return Conflict(new { error = "Email is already in use." });
                }

                 // Check username uniqueness
                if (await _context.Admins.AnyAsync(a => a.Username == modDto.Username) ||
                    await _context.Moderators.AnyAsync(m => m.Username == modDto.Username) ||
                    await _context.Users.AnyAsync(u => u.Username == modDto.Username))
                {
                     return Conflict(new { error = "Username is already in use." });
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
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while creating moderator." });
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
            if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator))
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
            if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator))
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
            if (role == UserRoles.Admin)
            {
                 var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Adminid.ToString() == id);
                 return admin?.Username ?? "Unknown Admin";
            }
            else if (role == UserRoles.Moderator)
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
                    m.Email,
                    m.Contact,
                    m.UserRole,
                    m.IsActive,
                    m.SuspensionEndTime,
                    m.Area_Code,
                    AreaLocation = m.Area != null ? m.Area.ALocation : "Unknown"
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
                .Select(a => new
                {
                    a.Adminid,
                    a.Username,
                    a.Email,
                    a.Contact,
                    a.UserRole,
                    a.IsActive,
                    a.SuspensionEndTime,
                    a.IsSuperAdmin,
                    a.Permissions
                })
                .ToListAsync();

            return Ok(admins);
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAllAreas()
        {
            var areas = await _context.Area.ToListAsync();
            return Ok(areas);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingIncidents()
        {
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator && requester.RequesterRole != UserRoles.User))
                {
                    return Unauthorized(new { error = "Unauthorized access." });
                }

                string requesterAreaCode = null;
                if (requester.RequesterRole == UserRoles.Moderator)
                {
                    var mod = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid.ToString() == requester.RequesterId);
                    if (mod != null)
                    {
                         requesterAreaCode = mod.Area_Code;
                    }
                }

                // PENDING LOGIC:
                // 1. ValidStatus is null (Legacy or Unvalidated)
                // 2. OR Validation_Date is null (Created but not acted upon, default Status is false)
                var query = _context.Incident
                    .Include(i => i.ValidStatus)
                    .Include(i => i.User) 
                    .ThenInclude(u => u.Badges)
                    .Include(i => i.Area)
                    .Where(i => i.ValidStatus == null || i.ValidStatus.Validation_Date == null);

                if (requester.RequesterRole == UserRoles.User)
                {
                    // Strict filter: Users can ONLY see their own pending incidents
                    query = query.Where(i => i.Userid.ToString() == requester.RequesterId);
                }
                else if (!string.IsNullOrEmpty(requesterAreaCode) && requesterAreaCode != "DEFAULT")
                {
                    query = query.Where(i => i.Area_Code == requesterAreaCode);
                }

                var pendingIncidents = await query
                    .OrderByDescending(i => i.IncidentDateTime)
                    .Select(i => new
                    {
                        i.IncidentID,
                        i.Userid, // Expose Userid for client-side filtering
                        i.Title,
                        i.Incident_Code,
                        i.OtherHazard,
                        i.Severity,
                        i.IncidentDateTime,
                        i.LocationAddress,
                        i.Img,
                        i.Descr,
                        i.Latitude,
                        i.Longitude,
                        User = i.User != null ? new { 
                            i.User.Username, 
                            i.User.Email,
                            i.User.TrustScore,
                            i.User.FirstName,
                            i.User.LastName,
                            Badges = i.User.Badges.Select(b => new { b.BadgeName, b.AwardedAt }).ToList() 
                        } : null,
                        Area = i.Area != null ? new { i.Area.ALocation, i.Area.Area_Code } : null,
                        ValidStatus = i.ValidStatus != null ? new { i.ValidStatus.Validation_Status, i.ValidStatus.Validation_Date } : null
                    })
                    .ToListAsync();

                return Ok(pendingIncidents);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while fetching pending incidents." });
            }
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedIncidents()
        {
            try
            {
                var requester = GetRequesterInfo();
                if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator && requester.RequesterRole != UserRoles.User))
                {
                    return Unauthorized(new { error = "Unauthorized access." });
                }

                string requesterAreaCode = null;
                if (requester.RequesterRole == UserRoles.Moderator)
                {
                     var mod = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid.ToString() == requester.RequesterId);
                     if (mod != null) requesterAreaCode = mod.Area_Code;
                }

                // REJECTED LOGIC:
                // 1. ValidStatus exists
                // 2. Validation_Status is False
                // 3. Validation_Date is NOT Null (Meaning it was explicitly rejected)
                var query = _context.Incident
                    .Include(i => i.ValidStatus)
                    .Include(i => i.User)
                    .Include(i => i.Area)
                    .Where(i => i.ValidStatus != null && i.ValidStatus.Validation_Status == false && i.ValidStatus.Validation_Date != null);

                if (requester.RequesterRole == UserRoles.User)
                {
                    // Strict filter: Users can ONLY see their own rejected incidents
                    query = query.Where(i => i.Userid.ToString() == requester.RequesterId);
                }
                else if (!string.IsNullOrEmpty(requesterAreaCode) && requesterAreaCode != "DEFAULT")
                {
                    query = query.Where(i => i.Area_Code == requesterAreaCode);
                }

                var deletedIncidents = await query
                    .OrderByDescending(i => i.IncidentDateTime)
                    .Select(i => new
                    {
                        i.IncidentID,
                        i.Userid, // Expose Userid for client-side filtering
                        i.Title,
                        i.Incident_Code,
                        i.OtherHazard,
                        i.Severity,
                        i.IncidentDateTime,
                        i.LocationAddress,
                        i.Img,
                        i.Descr,
                        i.Latitude,
                        i.Longitude,
                        rejectedIncidentID = i.IncidentID, 
                        User = i.User != null ? new { i.User.Username } : null,
                        Area = i.Area != null ? new { i.Area.ALocation } : null,
                        ValidStatus = new 
                        { 
                            Validation_Status = false, 
                            Validation_Date = i.ValidStatus.Validation_Date 
                        }
                    })
                    .ToListAsync();

                return Ok(deletedIncidents);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while fetching deleted incidents." });
            }
        }

        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            var requester = GetRequesterInfo();
            if (requester == null) return Unauthorized();

            string requesterAreaCode = null;
            if (requester.RequesterRole == UserRoles.Moderator)
            {
                 var mod = await _context.Moderators.FirstOrDefaultAsync(m => m.Modid.ToString() == requester.RequesterId);
                 if (mod != null) requesterAreaCode = mod.Area_Code;
            }

            var query = _context.Valid
                .Include(v => v.Incident)
                .Where(v => v.Validation_Date != null);

             if (!string.IsNullOrEmpty(requesterAreaCode) && requesterAreaCode != "DEFAULT")
            {
                query = query.Where(v => v.Incident.Area_Code == requesterAreaCode);
            }

            var activities = await query
                .OrderByDescending(v => v.Validation_Date)
                .Take(10)
                .Select(v => new
                {
                    Title = v.Incident.Title,
                    Status = v.Validation_Status == true ? "Approved" : "Rejected",
                    ValidationDate = v.Validation_Date,
                    ValidatorName = "Unknown" 
                })
                .ToListAsync();

            return Ok(activities);
        }

        [HttpPost("validate/{id}")]
        public async Task<IActionResult> ValidateIncident(int id, [FromQuery] bool isAccepted)
        {
            var requester = GetRequesterInfo();
            if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator))
            {
                return Unauthorized(new { error = "Only Admin/Moderator can validate incidents." });
            }

            var incident = await _context.Incident.Include(i => i.ValidStatus).FirstOrDefaultAsync(i => i.IncidentID == id);
            if (incident == null) return NotFound(new { error = "Incident not found." });

            var validation = await _context.Valid.FirstOrDefaultAsync(v => v.IncidentID == id);
            if (validation == null)
            {
                validation = new Valid { IncidentID = id };
                _context.Valid.Add(validation);
            }

            validation.Validation_Status = isAccepted;
            validation.Validation_Date = DateTime.UtcNow;
            
            if (Guid.TryParse(requester.RequesterId, out Guid validatorId))
            {
                validation.ValidatorID = validatorId;
            }

            if (isAccepted)
            {
                incident.IsResolved = false; // Ensure it doesn't jump to Resolved filter
                var user = await _context.Users.FindAsync(incident.Userid);
                if (user != null)
                {
                    user.TrustScore += 10;
                    
                    if (user.TrustScore >= 100 && !user.Badges.Any(b => b.BadgeName == "Reliable Source"))
                    {
                        _context.UserBadges.Add(new UserBadge { UserId = user.Userid, BadgeName = "Reliable Source", AwardedBy = "System" });
                         await _hubContext.Clients.All.SendAsync("ReceiveBadgeNotification", user.Userid.ToString(), "Reliable Source");
                    }
                }
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveIncidentNotification", 
                incident.Title, 
                incident.LocationAddress ?? "Unknown Location", 
                incident.Latitude ?? 0, 
                incident.Longitude ?? 0, 
                incident.IncidentID, 
                isAccepted ? "Validated" : "Rejected", 
                incident.Userid.ToString());

            return Ok(new { message = isAccepted ? "Incident validated successfully." : "Incident rejected successfully." });
        }

        [HttpPost("unvalidate/{id}")]
        public async Task<IActionResult> UnvalidateIncident(int id)
        {
            var requester = GetRequesterInfo();
            if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator))
            {
                return Unauthorized(new { error = "Only Admin/Moderator can unvalidate incidents." });
            }

            var validation = await _context.Valid.FirstOrDefaultAsync(v => v.IncidentID == id);
            if (validation == null) return NotFound(new { error = "Validation record not found." });

            _context.Valid.Remove(validation);
            
            var incident = await _context.Incident.FindAsync(id);
            if (incident != null)
            {
                 if (validation.Validation_Status == true)
                 {
                     var user = await _context.Users.FindAsync(incident.Userid);
                     if (user != null)
                     {
                         user.TrustScore = Math.Max(0, user.TrustScore - 10);
                     }
                 }
                 incident.IsResolved = false; 
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Incident unvalidated and moved to Pending." });
        }

        [HttpPost("recover/{id}")]
        public async Task<IActionResult> RecoverIncident(int id)
        {
            return await UnvalidateIncident(id);
        }

        [HttpDelete("delete-permanent/{id}")]
        public async Task<IActionResult> DeleteIncidentPermanently(int id)
        {
             var requester = GetRequesterInfo();
            if (requester == null || (requester.RequesterRole != UserRoles.Admin && requester.RequesterRole != UserRoles.Moderator))
            {
                return Unauthorized(new { error = "Only Admin/Moderator can delete incidents." });
            }

            var incident = await _context.Incident.FindAsync(id);
            if (incident == null) return NotFound(new { error = "Incident not found." });

            var archive = new IncidentArchive
            {
                OriginalIncidentID = incident.IncidentID,
                Userid = incident.Userid,
                Title = incident.Title,
                Incident_Code = incident.Incident_Code,
                Latitude = incident.Latitude ?? 0,
                Longitude = incident.Longitude ?? 0,
                IncidentDateTime = incident.IncidentDateTime,
                DeletionDate = DateTime.UtcNow,
                Area_Code = incident.Area_Code,
                Descr = incident.Descr,
                Severity = incident.Severity
            };
            _context.IncidentArchives.Add(archive);

            _context.Incident.Remove(incident);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Incident permanently deleted." });
        }


         [HttpPost("change-role")]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleDto changeDto)
        {
            var requester = GetRequesterInfo();
            if (requester == null || requester.RequesterRole != UserRoles.Admin)
            {
                return Unauthorized(new { error = "Only Admin can change user roles." });
            }
            if (!await IsAdmin(requester.RequesterId))
            {
                return Unauthorized(new { error = "Invalid admin credentials." });
            }
             if (!await HasPermission(requester.RequesterId, "ManageUsers") && !await HasPermission(requester.RequesterId, "ManageModerators") && !await HasPermission(requester.RequesterId, "ManageAdmins"))
            {
                return StatusCode(403, new { error = "You do not have permission to change roles." });
            }

            if (!Guid.TryParse(changeDto.Id, out Guid targetId))
            {
                return BadRequest(new { error = "Invalid ID format." });
            }

            try
            {
                if (changeDto.CurrentRole == UserRoles.User && changeDto.TargetRole == UserRoles.Moderator)
                {
                    var user = await _context.Users.FindAsync(targetId);
                    if (user == null) return NotFound("User not found.");

                    if (!string.IsNullOrEmpty(changeDto.Email) && changeDto.Email != user.Email)
                    {
                        if (await EmailExistsForUpdateAsync(changeDto.Email, targetId)) return Conflict(new { error = "Email already in use." });
                    }

                    var newMod = new Moderator
                    {
                        Modid = targetId, 
                        Username = changeDto.Username ?? user.Username,
                        Email = changeDto.Email ?? user.Email,
                        Contact = changeDto.Contact ?? user.Contact,
                        Modpassword = !string.IsNullOrEmpty(changeDto.Password) ? BCrypt.Net.BCrypt.HashPassword(changeDto.Password) : user.Userpassword,
                        Area_Code = changeDto.Area_Code ?? "DEFAULT",
                        UserRole = UserRoles.Moderator,
                        IsActive = changeDto.IsActive ?? user.IsActive,
                        SuspensionEndTime = changeDto.SuspensionEndTime
                    };

                    
                    _context.Users.Remove(user);
                     await _context.SaveChangesAsync(); 
                    
                     _context.Moderators.Add(newMod);
                    await _context.SaveChangesAsync();
                }
                else if (changeDto.CurrentRole == UserRoles.User && changeDto.TargetRole == UserRoles.Admin)
                {
                     var user = await _context.Users.FindAsync(targetId);
                    if (user == null) return NotFound("User not found.");

                     if (!string.IsNullOrEmpty(changeDto.Email) && changeDto.Email != user.Email)
                    {
                         if (await EmailExistsForUpdateAsync(changeDto.Email, targetId)) return Conflict(new { error = "Email already in use." });
                    }

                    var newAdmin = new Admin
                    {
                        Adminid = targetId,
                        Username = changeDto.Username ?? user.Username,
                        Email = changeDto.Email ?? user.Email,
                        Contact = changeDto.Contact ?? user.Contact,
                        Adminpassword = !string.IsNullOrEmpty(changeDto.Password) ? BCrypt.Net.BCrypt.HashPassword(changeDto.Password) : user.Userpassword,
                        UserRole = UserRoles.Admin,
                        IsActive = changeDto.IsActive ?? user.IsActive,
                        SuspensionEndTime = changeDto.SuspensionEndTime,
                        IsSuperAdmin = false,
                        Permissions = changeDto.Permissions
                    };
                    
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    
                    _context.Admins.Add(newAdmin);
                    await _context.SaveChangesAsync();
                }
                else if (changeDto.CurrentRole == UserRoles.Moderator && changeDto.TargetRole == UserRoles.User)
                {
                     var mod = await _context.Moderators.FindAsync(targetId);
                    if (mod == null) return NotFound("Moderator not found.");

                     if (!string.IsNullOrEmpty(changeDto.Email) && changeDto.Email != mod.Email)
                    {
                         if (await EmailExistsForUpdateAsync(changeDto.Email, targetId)) return Conflict(new { error = "Email already in use." });
                    }

                     var newUser = new User
                    {
                        Userid = targetId,
                        Username = changeDto.Username ?? mod.Username,
                        Email = changeDto.Email ?? mod.Email,
                        Contact = changeDto.Contact ?? mod.Contact,
                        FirstName = changeDto.FirstName ?? "Unknown", 
                        LastName = changeDto.LastName ?? "Unknown", 
                        Userpassword = !string.IsNullOrEmpty(changeDto.Password) ? BCrypt.Net.BCrypt.HashPassword(changeDto.Password) : mod.Modpassword,
                        UserRole = UserRoles.User,
                        IsActive = changeDto.IsActive ?? mod.IsActive,
                        SuspensionEndTime = changeDto.SuspensionEndTime
                    };

                    _context.Moderators.Remove(mod);
                    await _context.SaveChangesAsync();
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                }
                 else if (changeDto.CurrentRole == UserRoles.Moderator && changeDto.TargetRole == UserRoles.Admin)
                {
                     var mod = await _context.Moderators.FindAsync(targetId);
                    if (mod == null) return NotFound("Moderator not found.");

                     if (!string.IsNullOrEmpty(changeDto.Email) && changeDto.Email != mod.Email)
                    {
                        if (await EmailExistsForUpdateAsync(changeDto.Email, targetId)) return Conflict(new { error = "Email already in use." });
                    }

                    var newAdmin = new Admin
                    {
                        Adminid = targetId,
                        Username = changeDto.Username ?? mod.Username,
                        Email = changeDto.Email ?? mod.Email,
                        Contact = changeDto.Contact ?? mod.Contact,
                        Adminpassword = !string.IsNullOrEmpty(changeDto.Password) ? BCrypt.Net.BCrypt.HashPassword(changeDto.Password) : mod.Modpassword,
                        UserRole = UserRoles.Admin,
                        IsActive = changeDto.IsActive ?? mod.IsActive,
                        SuspensionEndTime = changeDto.SuspensionEndTime,
                         IsSuperAdmin = false,
                        Permissions = changeDto.Permissions
                    };
                    
                     _context.Moderators.Remove(mod);
                    await _context.SaveChangesAsync();
                    _context.Admins.Add(newAdmin);
                    await _context.SaveChangesAsync();
                }
                 else if (changeDto.CurrentRole == UserRoles.Admin && changeDto.TargetRole == UserRoles.User)
                {
                    var admin = await _context.Admins.FindAsync(targetId);
                    if (admin == null) return NotFound("Admin not found.");

                     if (!string.IsNullOrEmpty(changeDto.Email) && changeDto.Email != admin.Email)
                    {
                         if (await EmailExistsForUpdateAsync(changeDto.Email, targetId)) return Conflict(new { error = "Email already in use." });
                    }

                     var newUser = new User
                    {
                        Userid = targetId,
                        Username = changeDto.Username ?? admin.Username,
                        Email = changeDto.Email ?? admin.Email,
                        Contact = changeDto.Contact ?? admin.Contact,
                         FirstName = changeDto.FirstName ?? "Unknown", 
                        LastName = changeDto.LastName ?? "Unknown", 
                        Userpassword = !string.IsNullOrEmpty(changeDto.Password) ? BCrypt.Net.BCrypt.HashPassword(changeDto.Password) : admin.Adminpassword,
                        UserRole = UserRoles.User,
                        IsActive = changeDto.IsActive ?? admin.IsActive,
                        SuspensionEndTime = changeDto.SuspensionEndTime
                    };

                     _context.Admins.Remove(admin);
                     await _context.SaveChangesAsync();
                     _context.Users.Add(newUser);
                     await _context.SaveChangesAsync();
                }
                 else if (changeDto.CurrentRole == UserRoles.Admin && changeDto.TargetRole == UserRoles.Moderator)
                {
                     var admin = await _context.Admins.FindAsync(targetId);
                    if (admin == null) return NotFound("Admin not found.");

                     if (!string.IsNullOrEmpty(changeDto.Email) && changeDto.Email != admin.Email)
                    {
                         if (await EmailExistsForUpdateAsync(changeDto.Email, targetId)) return Conflict(new { error = "Email already in use." });
                    }

                    var newMod = new Moderator
                    {
                        Modid = targetId,
                        Username = changeDto.Username ?? admin.Username,
                        Email = changeDto.Email ?? admin.Email,
                        Contact = changeDto.Contact ?? admin.Contact,
                        Modpassword = !string.IsNullOrEmpty(changeDto.Password) ? BCrypt.Net.BCrypt.HashPassword(changeDto.Password) : admin.Adminpassword,
                        Area_Code = changeDto.Area_Code ?? "DEFAULT",
                        UserRole = UserRoles.Moderator,
                        IsActive = changeDto.IsActive ?? admin.IsActive,
                        SuspensionEndTime = changeDto.SuspensionEndTime
                    };

                     _context.Admins.Remove(admin);
                     await _context.SaveChangesAsync();
                     _context.Moderators.Add(newMod);
                     await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Role changed successfully." });
            }
            catch (Exception)
            {
               return StatusCode(500, new { error = "An error occurred while changing roles." });
            }
        }
    }
}