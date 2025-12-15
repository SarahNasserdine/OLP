using OLP.Core.Entities;
using OLP.Core.Interfaces;

namespace OLP.Infrastructure.Services
{
    public class CertificateService
    {
        private readonly ICertificateRepository _certRepo;
        private readonly ICourseRepository _courseRepo;

        public CertificateService(ICertificateRepository certRepo, ICourseRepository courseRepo)
        {
            _certRepo = certRepo;
            _courseRepo = courseRepo;
        }

        public async Task<Certificate> GenerateCertificateAsync(int userId, int courseId)
        {
            var exists = await _certRepo.GetByUserAndCourseAsync(userId, courseId);
            if (exists != null)
                throw new Exception("Certificate already exists for this course.");

            var verificationCode = Guid.NewGuid().ToString("N").Substring(0, 10);

            var certificate = new Certificate
            {
                UserId = userId,
                CourseId = courseId,
                DownloadUrl = $"https://yourdomain.com/certificates/{verificationCode}.pdf",
                VerificationCode = verificationCode,
                Status = "Generated",
                GeneratedAt = DateTime.UtcNow
            };

            await _certRepo.AddAsync(certificate);
            await _certRepo.SaveChangesAsync();

            return certificate;
        }
    }
}
