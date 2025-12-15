using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class CertificateRepository : ICertificateRepository
    {
        private readonly AppDbContext _context;
        public CertificateRepository(AppDbContext context) => _context = context;

        public async Task<Certificate?> GetByUserAndCourseAsync(int userId, int courseId) =>
            await _context.Certificates.FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

        public async Task<IEnumerable<Certificate>> GetByUserAsync(int userId) =>
            await _context.Certificates.Include(c => c.Course).Where(c => c.UserId == userId).ToListAsync();

        public async Task AddAsync(Certificate certificate) =>
            await _context.Certificates.AddAsync(certificate);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
