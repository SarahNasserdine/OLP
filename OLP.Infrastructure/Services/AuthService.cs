using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using OLP.Core.Entities;
using OLP.Core.Enums;
using OLP.Core.Interfaces;

namespace OLP.Infrastructure.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        // === Register a new student ===
        public async Task<User> RegisterAsync(string fullName, string email, string password)
        {
            if (await _userRepo.EmailExistsAsync(email))
                throw new Exception("Email already exists");

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = hash,
                PasswordSalt = Convert.ToBase64String(salt),
                Role = UserRole.Student,
                IsEmailConfirmed = true // No Gmail confirmation required
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
            return user;
        }

        // === Login ===
        public async Task<string> LoginAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email)
                ?? throw new Exception("User not found");

            var salt = Convert.FromBase64String(user.PasswordSalt ?? "");
            var hash = HashPassword(password, salt);

            if (hash != user.PasswordHash)
                throw new Exception("Invalid password");

            return GenerateJwtToken(user);
        }

        // === Reset password ===
        public async Task ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _userRepo.GetByEmailAsync(email)
                ?? throw new Exception("User not found");

            var salt = GenerateSalt();
            var hash = HashPassword(newPassword, salt);

            user.PasswordSalt = Convert.ToBase64String(salt);
            user.PasswordHash = hash;

            await _userRepo.SaveChangesAsync();
        }

        // === Helpers ===
        private static byte[] GenerateSalt()
        {
            var salt = new byte[128 / 8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private static string HashPassword(string password, byte[] salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
