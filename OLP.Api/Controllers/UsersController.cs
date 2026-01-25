using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using OLP.Core.Enums;
using OLP.Core.Interfaces;
using OLP.Core.DTOs;
using OLP.Core.Entities;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UsersController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // GET /api/users (Admin, SuperAdmin)
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userRepo.GetAllAsync();
            return Ok(users.Select(ToDto));
        }

        // GET /api/users/me (any logged in user)
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(ToDto(user));
        }

        // GET /api/users/{id}
        // Student: only self
        // Admin/SuperAdmin: any user
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";

            if (role == "Student" && id != currentUserId)
                return Forbid();

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(ToDto(user));
        }

        // PUT /api/users/{id}
        // Student: update only self (FullName only)
        // Admin/SuperAdmin: update anyone (FullName + IsActive)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (dto == null) return BadRequest("Body is required.");

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";

            if (role == "Student" && id != currentUserId)
                return Forbid();

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            // Profile update
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (role != "SuperAdmin")
                    return Forbid();

                var newEmail = dto.Email.Trim();
                var currentEmail = user.Email ?? "";
                if (!string.Equals(newEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _userRepo.EmailExistsAsync(newEmail))
                        return BadRequest("Email already exists");

                    var isOlpEmail = newEmail.EndsWith("@olp.com", StringComparison.OrdinalIgnoreCase);
                    if ((user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin) && !isOlpEmail)
                        return BadRequest("Admin and SuperAdmin emails must use the @olp.com domain.");
                    if (user.Role == UserRole.Student && isOlpEmail && role != "SuperAdmin")
                        return BadRequest("Student emails cannot use the @olp.com domain.");

                    user.Email = newEmail;
                }
            }

            // Moderation field (Admin/SuperAdmin only)
            if ((role == "Admin" || role == "SuperAdmin") && dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            await _userRepo.SaveChangesAsync();
            return Ok(ToDto(user));
        }

        // PUT /api/users/{id}/role (SuperAdmin only)
        [HttpPut("{id}/role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Role))
                return BadRequest("Role is required.");

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var newRole))
                return BadRequest("Invalid role.");

            var email = user.Email ?? "";
            var isOlpEmail = email.Trim().EndsWith("@olp.com", StringComparison.OrdinalIgnoreCase);
            if ((newRole == UserRole.Admin || newRole == UserRole.SuperAdmin) && !isOlpEmail)
                return BadRequest("Admin and SuperAdmin emails must use the @olp.com domain.");
            if (newRole == UserRole.Student && isOlpEmail)
                return BadRequest("Student emails cannot use the @olp.com domain.");

            user.Role = newRole;
            await _userRepo.SaveChangesAsync();

            return Ok(ToDto(user));
        }

        // DELETE /api/users/{id} (soft delete, SuperAdmin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;
            await _userRepo.SaveChangesAsync();

            return NoContent();
        }

        // Mapping: User -> UserDto
        private static UserDto ToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }
    }
}
