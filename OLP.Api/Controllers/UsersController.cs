using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.Enums;
using OLP.Core.Interfaces;

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

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] string role)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (!Enum.TryParse<UserRole>(role, out var newRole))
                return BadRequest("Invalid role.");

            user.Role = newRole;
            await _userRepo.SaveChangesAsync();
            return Ok(user);
        }
    }
}
//GET /api/users/me → current logged-in user (any role)

//GET / api / users /{ id} → Admin / SuperAdmin

//PUT / api / users /{ id}/ role → SuperAdmin only(change Student/Admin/Instructor)