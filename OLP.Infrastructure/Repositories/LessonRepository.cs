using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly AppDbContext _context;
        public LessonRepository(AppDbContext context) => _context = context;

        public async Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId) =>
            await _context.Lessons.Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

        public async Task<Lesson?> GetByIdAsync(int id) =>
            await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id);

        public async Task<int> CountByCourseAsync(int courseId) =>
            await _context.Lessons.CountAsync(l => l.CourseId == courseId);

        public async Task AddAsync(Lesson lesson) =>
            await _context.Lessons.AddAsync(lesson);

        public Task UpdateAsync(Lesson lesson)
        {
            _context.Lessons.Update(lesson);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Lesson lesson)
        {
            _context.Lessons.Remove(lesson);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
