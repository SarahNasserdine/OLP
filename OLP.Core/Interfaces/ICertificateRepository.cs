using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface ICertificateRepository
    {
        Task<Certificate?> GetByUserAndCourseAsync(int userId, int courseId);
        Task<Certificate?> GetByIdAsync(int id);
        Task<Certificate?> GetByCodeAsync(string code);
        Task AddAsync(Certificate certificate);
        Task<IEnumerable<Certificate>> GetByUserAsync(int userId);
        Task SaveChangesAsync();
    }
}
