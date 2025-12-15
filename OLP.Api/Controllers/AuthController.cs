using Microsoft.AspNetCore.Mvc;
using OLP.Core.DTOs;
using OLP.Infrastructure.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // always Student
            var user = await _authService.RegisterAsync(dto.FullName, dto.Email, dto.Password);
            return Ok(new { user.Id, user.FullName, user.Email, Role = user.Role.ToString() });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto.Email, dto.Password);
            return Ok(new { token });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto.Email, dto.NewPassword);
            return Ok(new { message = "Password reset successfully" });
        }
    }

}
