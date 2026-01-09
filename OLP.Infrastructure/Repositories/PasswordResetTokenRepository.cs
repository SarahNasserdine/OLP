using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AppDbContext _context;
        public PasswordResetTokenRepository(AppDbContext context) => _context = context;

        public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash) =>
            await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public async Task AddAsync(PasswordResetToken token) =>
            await _context.PasswordResetTokens.AddAsync(token);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
