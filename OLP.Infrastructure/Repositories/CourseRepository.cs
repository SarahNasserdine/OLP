using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Enums;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;
        public CourseRepository(AppDbContext context) => _context = context;

        public async Task<IEnumerable<Course>> GetAllAsync() =>
            await _context.Courses
                .Include(c => c.Creator)
                .ToListAsync();

        public async Task<Course?> GetByIdAsync(int id) =>
            await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<IEnumerable<Course>> SearchAsync(string? q, string? category, DifficultyLevel? difficulty, string? sort)
        {
            var query = _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Enrollments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(c =>
                    c.Title.Contains(keyword) ||
                    c.ShortDescription.Contains(keyword) ||
                    c.LongDescription.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim();
                query = query.Where(c => c.Category == cat);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(c => c.Difficulty == difficulty.Value);
            }

            query = sort?.ToLower() switch
            {
                "popular" => query.OrderByDescending(c => c.Enrollments.Count),
                "newest" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            return await query.ToListAsync();
        }

        public async Task<int> CountByCreatorAsync(int creatorId) =>
            await _context.Courses.CountAsync(c => c.CreatedById == creatorId);

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
