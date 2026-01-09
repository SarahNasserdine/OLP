using OLP.Core.Entities;
using OLP.Core.Enums;

namespace OLP.Core.Interfaces
{
    public interface ICourseRepository
    {
        Task<IEnumerable<Course>> GetAllAsync();
        Task<Course?> GetByIdAsync(int id);
        Task<IEnumerable<Course>> SearchAsync(string? q, string? category, DifficultyLevel? difficulty, string? sort);
        Task<int> CountByCreatorAsync(int creatorId);
        Task AddAsync(Course course);
        Task UpdateAsync(Course course);
        Task DeleteAsync(Course course);
        Task SaveChangesAsync();
    }
}
