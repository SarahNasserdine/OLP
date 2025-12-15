using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface ILessonRepository
    {
        Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId);
        Task<Lesson?> GetByIdAsync(int id);
        Task AddAsync(Lesson lesson);
        Task UpdateAsync(Lesson lesson);
        Task DeleteAsync(Lesson lesson);
        Task SaveChangesAsync();
    }
}
