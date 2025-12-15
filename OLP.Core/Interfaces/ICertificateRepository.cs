using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface ICertificateRepository
    {
        Task<Certificate?> GetByUserAndCourseAsync(int userId, int courseId);
        Task AddAsync(Certificate certificate);
        Task<IEnumerable<Certificate>> GetByUserAsync(int userId);
        Task SaveChangesAsync();
    }
}
