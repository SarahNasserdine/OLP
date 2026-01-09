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
            await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

        public async Task<IEnumerable<Enrollment>> GetByUserAsync(int userId) =>
            await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Include(e => e.Course) // ? OK (but avoid Course.Enrollments)
                .ToListAsync();

        public async Task<IEnumerable<Enrollment>> GetByUserWithCourseAsync(int userId) =>
            await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .ToListAsync();

        public async Task<IEnumerable<Enrollment>> GetByCourseAsync(int courseId) =>
            await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Include(e => e.User)   // ? optional but useful for admins
                .ToListAsync();

        public async Task<int> CountAllAsync() =>
            await _context.Enrollments.CountAsync();

        public async Task<IEnumerable<Enrollment>> GetAllAsync() =>
            await _context.Enrollments
                .AsNoTracking()
                .ToListAsync();

        public async Task<double> GetAverageProgressAsync() =>
            await _context.Enrollments.AnyAsync()
                ? await _context.Enrollments.AverageAsync(e => (double)e.Progress)
                : 0;

        public async Task AddAsync(Enrollment enrollment) =>
            await _context.Enrollments.AddAsync(enrollment);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
