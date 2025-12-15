using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Enums;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedUsersAsync(AppDbContext context, IConfiguration config)
        {
            await context.Database.MigrateAsync();

            if (!await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            {
                var adminPassword = "Admin@123"; // change after seeding
                var admin = CreateUser("Admin User", "admin@olp.com", adminPassword, UserRole.Admin);
                await context.Users.AddAsync(admin);
            }

            if (!await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var superPassword = "SuperAdmin@123"; // change after seeding
                var superAdmin = CreateUser("Super Admin", "superadmin@olp.com", superPassword, UserRole.SuperAdmin);
                await context.Users.AddAsync(superAdmin);
            }

            await context.SaveChangesAsync();
        }

        private static User CreateUser(string name, string email, string password, UserRole role)
        {
            byte[] salt = new byte[128 / 8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return new User
            {
                FullName = name,
                Email = email,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = hash,
                Role = role
            };
        }
    }
}
