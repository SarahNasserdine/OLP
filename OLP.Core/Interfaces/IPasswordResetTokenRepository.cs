using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetByHashAsync(string tokenHash);
        Task AddAsync(PasswordResetToken token);
        Task SaveChangesAsync();
    }
}
