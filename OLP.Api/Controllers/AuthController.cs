using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IPasswordResetTokenRepository _tokenRepo;
        private readonly IUserRepository _userRepo;

        public AuthController(AuthService authService, IPasswordResetTokenRepository tokenRepo, IUserRepository userRepo)
        {
            _authService = authService;
            _tokenRepo = tokenRepo;
            _userRepo = userRepo;
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
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);
            if (user == null)
                return Ok(new { message = "If the email exists, a reset link was sent." });

            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashToken(rawToken);

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            await _tokenRepo.AddAsync(resetToken);
            await _tokenRepo.SaveChangesAsync();

            return Ok(new { message = "Reset token generated", token = rawToken });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var tokenHash = HashToken(dto.Token);
            var resetToken = await _tokenRepo.GetByHashAsync(tokenHash);

            if (resetToken == null || resetToken.UsedAt.HasValue || resetToken.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired reset token.");

            await _authService.ResetPasswordAsync(resetToken.User.Email, dto.NewPassword);

            resetToken.UsedAt = DateTime.UtcNow;
            await _tokenRepo.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("CurrentPassword and NewPassword are required.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized("Invalid token.");

            try
            {
                await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }

}
