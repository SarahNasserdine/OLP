using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.Id)
                .ToListAsync();

        public async Task<User?> GetByIdAsync(int id)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        public async Task AddAsync(User user)
            => await _context.Users.AddAsync(user);

        public async Task<bool> EmailExistsAsync(string email)
            => await _context.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
