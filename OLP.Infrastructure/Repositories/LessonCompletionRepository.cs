using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class LessonCompletionRepository : ILessonCompletionRepository
    {
        private readonly AppDbContext _context;
        public LessonCompletionRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(LessonCompletion completion) =>
            await _context.LessonCompletions.AddAsync(completion);

        public async Task<bool> IsCompletedAsync(int userId, int lessonId) =>
            await _context.LessonCompletions.AnyAsync(lc => lc.UserId == userId && lc.LessonId == lessonId);

        public async Task<IEnumerable<LessonCompletion>> GetByUserAsync(int userId) =>
            await _context.LessonCompletions.Include(lc => lc.Lesson).Where(lc => lc.UserId == userId).ToListAsync();

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
