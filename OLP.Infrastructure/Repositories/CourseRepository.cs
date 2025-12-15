using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;
        public CourseRepository(AppDbContext context) => _context = context;

        public async Task<IEnumerable<Course>> GetAllAsync() =>
      await _context.Courses.Include(c => c.Creator).ToListAsync();


        public async Task<Course?> GetByIdAsync(int id) =>
            await _context.Courses.Include(c => c.Lessons).FirstOrDefaultAsync(c => c.Id == id);

        public async Task AddAsync(Course course) =>
            await _context.Courses.AddAsync(course);

        public Task UpdateAsync(Course course)
        {
            _context.Courses.Update(course);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Course course)
        {
            _context.Courses.Remove(course);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
