using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId);

        Task<IEnumerable<Enrollment>> GetByUserAsync(int userId);
        Task<IEnumerable<Enrollment>> GetByUserWithCourseAsync(int userId);

        // ? Admin needs this
        Task<IEnumerable<Enrollment>> GetByCourseAsync(int courseId);
        Task<int> CountAllAsync();
        Task<IEnumerable<Enrollment>> GetAllAsync();
        Task<double> GetAverageProgressAsync();

        Task AddAsync(Enrollment enrollment);
        Task SaveChangesAsync();
    }
}
