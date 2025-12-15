using System;

namespace OLP.Core.DTOs
{
    public class CertificateDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public string DownloadUrl { get; set; } = null!;
        public string? VerificationCode { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
