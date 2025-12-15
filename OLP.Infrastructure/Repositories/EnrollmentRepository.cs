using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly AppDbContext _context;
        public EnrollmentRepository(AppDbContext context) => _context = context;

        public async Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId) =>
            await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

        public async Task<IEnumerable<Enrollment>> GetByUserAsync(int userId) =>
            await _context.Enrollments.Include(e => e.Course).Where(e => e.UserId == userId).ToListAsync();

        public async Task AddAsync(Enrollment enrollment) =>
            await _context.Enrollments.AddAsync(enrollment);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
