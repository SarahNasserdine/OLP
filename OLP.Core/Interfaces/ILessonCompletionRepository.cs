using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface ILessonCompletionRepository
    {
        Task AddAsync(LessonCompletion completion);
        Task<bool> IsCompletedAsync(int userId, int lessonId);
        Task<IEnumerable<LessonCompletion>> GetByUserAsync(int userId);
        Task SaveChangesAsync();
    }
}
