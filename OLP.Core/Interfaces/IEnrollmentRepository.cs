using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId);
        Task<IEnumerable<Enrollment>> GetByUserAsync(int userId);
        Task AddAsync(Enrollment enrollment);
        Task SaveChangesAsync();
    }
}
